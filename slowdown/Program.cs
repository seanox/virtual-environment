// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Startup
// Downgrades the priority of overactive processes.
// Copyright (C) 2021 Seanox Software Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace slowdown
{
    internal class Program
    {
        private static void Main()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Seanox SwitchDown [Version 0.0.0 00000000]");
                Console.WriteLine("Copyright (C) 0000 Seanox Software Solutions");
                Console.WriteLine();
                Console.WriteLine("The program must be configured as a service.");
                Console.WriteLine();
                Console.WriteLine($"sc.exe create slowdown binpath=\"{Assembly.GetExecutingAssembly().Location}\" start=auto");
                return;
            }
            System.ServiceProcess.ServiceBase.Run(new Service());
        }
    }
    
    internal class Service : ServiceBase
    {
        private const int CPU_LOAD_THRESHOLD_PERCENT = 25;
        private const int MEASURING_TIME_MILLISECONDS = 1000;
        private const int INTERRUPT_MILLISECONDS = 25;

        private BackgroundWorker backgroundWorker;
        
        private BackgroundWorker backgroundCleaner;

        private bool interrupt;
        
        private readonly Dictionary<int, ProcessPriorityClass> processPriorities; 

        public Service()
        {
            this.ServiceName = "SlowDown";
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = false;

            this.processPriorities = new Dictionary<int, ProcessPriorityClass>();
        }

        private Dictionary<string, PerformanceCounter> CollectPerformanceCounter()
        {
            var performanceCounterDictionary = new Dictionary<string, PerformanceCounter>();
            Process.GetProcesses().ToList().ForEach(process =>
            {
                if (this.backgroundWorker.CancellationPending
                        || this.interrupt)
                {
                    performanceCounterDictionary.Clear();
                    return;
                }
                
                Thread.Sleep(INTERRUPT_MILLISECONDS);
                
                try
                {
                    using (process)
                    {
                        if (ProcessPriorityClass.Idle.Equals(process.PriorityClass)
                                || performanceCounterDictionary.ContainsKey(process.ProcessName))
                            return;
                        var performanceCounter = new PerformanceCounter("Process", "% Processor Time",
                            process.ProcessName, true);
                        performanceCounter.NextValue();
                        performanceCounterDictionary.Add(process.ProcessName, performanceCounter);
                    }
                }
                catch (Exception)
                {
                }
            });

            return performanceCounterDictionary;
        }

        private void SwitchDownPrioritySmart(Dictionary<string, PerformanceCounter> performanceCounterDictionary)
        {
            foreach (var keyValuePair in performanceCounterDictionary)
            {
                if (this.backgroundWorker.CancellationPending
                        || this.interrupt)
                    return;
                
                Thread.Sleep(INTERRUPT_MILLISECONDS);

                if (keyValuePair.Value.NextValue() < CPU_LOAD_THRESHOLD_PERCENT)
                    continue;

                foreach (var process in Process.GetProcessesByName(keyValuePair.Key))
                {
                    EventLog.WriteEntry(keyValuePair.Key , EventLogEntryType.Information);
                    try
                    {
                        if (!this.processPriorities.ContainsKey(process.Id))
                            this.processPriorities.Add(process.Id, process.PriorityClass);
                        process.PriorityClass = ProcessPriorityClass.Idle;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void RestorePriority()
        {
            foreach (var keyValuePair in this.processPriorities)
            {
                try
                {
                    var process = Process.GetProcessById(keyValuePair.Key);
                    process.PriorityClass = keyValuePair.Value;
                }
                catch (Exception)
                {
                }
            }
        }

        private BackgroundWorker CreateBackgroundCleaner()
        {
            var backgroundCleaner = new BackgroundWorker();
            backgroundCleaner.WorkerSupportsCancellation = true;
            backgroundCleaner.WorkerReportsProgress = false;
            backgroundCleaner.DoWork += (sender, eventArguments) =>
            {
                EventLog.WriteEntry("Cleaner running.", EventLogEntryType.Information);
                while (!this.backgroundCleaner.CancellationPending)
                {
                    Thread.Sleep(1000);
                    if (this.interrupt
                            || this.backgroundCleaner.CancellationPending)
                        break;

                    foreach (var keyValuePair in this.processPriorities)
                    {
                        Thread.Sleep(1000);
                        if (this.interrupt
                                || this.backgroundCleaner.CancellationPending)
                            break;

                        try
                        {
                            Process.GetProcessById(keyValuePair.Key);
                        }
                        catch (Exception)
                        {
                            this.processPriorities.Remove(keyValuePair.Key);
                        }
                    }
                }
            };

            return backgroundCleaner;
        }

        private BackgroundWorker CreateBackgroundWorker()
        {
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.DoWork += (sender, eventArguments) =>
            {
                EventLog.WriteEntry("Worker running.", EventLogEntryType.Information);
                while (!this.backgroundWorker.CancellationPending)
                {
                    var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuUsage.NextValue();
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.interrupt
                            || cpuUsage.NextValue() < CPU_LOAD_THRESHOLD_PERCENT)
                        continue;

                    var performanceCounterDictionary = this.CollectPerformanceCounter();
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.interrupt
                            || this.backgroundWorker.CancellationPending)
                        continue;
                    this.SwitchDownPrioritySmart(performanceCounterDictionary);
                }

                this.RestorePriority();

                EventLog.WriteEntry("Worker stopped.", EventLogEntryType.Information);
            };

            return backgroundWorker;
        }

        protected override void OnStart(string[] options)
        {
            if (this.backgroundWorker != null
                    && this.backgroundWorker.IsBusy)
                return;
            EventLog.WriteEntry("Service initialized.", EventLogEntryType.Information);

            this.backgroundCleaner = this.CreateBackgroundCleaner();
            this.backgroundCleaner.RunWorkerAsync();

            this.backgroundWorker = this.CreateBackgroundWorker();
            this.backgroundWorker.RunWorkerAsync();
            
            EventLog.WriteEntry("Service started.", EventLogEntryType.Information);
        }

        protected override void OnPause()
        {
            if (this.interrupt)
                return;
            this.interrupt = true;
            EventLog.WriteEntry("Service paused.", EventLogEntryType.Information);
        }

        protected override void OnContinue()
        {
            if (!this.interrupt)
                return;
            this.interrupt = false;
            EventLog.WriteEntry("Service continued.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            if (!this.backgroundWorker.IsBusy)
                return;

            this.backgroundCleaner.CancelAsync();
            this.backgroundWorker.CancelAsync();
            while (this.backgroundCleaner.IsBusy
                    || this.backgroundWorker.IsBusy)
                Thread.Sleep(INTERRUPT_MILLISECONDS);
            this.backgroundCleaner = null;
            this.backgroundWorker = null;
            EventLog.WriteEntry("Service stopped.", EventLogEntryType.Information);
        }
    }
}