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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace shiftdown
{
    internal static class Program
    {
        internal static readonly string VERSION = 
            $"Seanox ShiftDown [Version 0.0.0 00000000]{Environment.NewLine}"
               + "Copyright (C) 0000 Seanox Software Solutions";

        internal struct Meta
        {
            internal string Location;
            internal string File;
            internal string Name;
        }

        internal static readonly Meta ApplicationMeta = new Meta()
        {
            Location = Assembly.GetExecutingAssembly().Location,
            File = Path.GetFileName(Assembly.GetExecutingAssembly().Location).Trim(),
            Name = Regex.Replace(Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location), @"\s+", "")
        };

        private static void Main(params string[] options)
        {
            #if DEBUG
                var service = new Service();
                service.OnDebug(options);
                return;
            #endif
            
            if (Environment.UserInteractive)
            {
                Console.WriteLine(Program.VERSION);
                Console.WriteLine();

                bool isAdministrator;
                using (var identity = WindowsIdentity.GetCurrent())
                    isAdministrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                
                var command = (isAdministrator
                        && ApplicationMeta.Name.Length > 0
                        && options.Length > 0
                    ? options[0] : "").Trim().ToLower();
                switch (command)
                {
                    case "install":
                        Program.BatchExec("sc.exe", "create", ApplicationMeta.Name, $"binpath=\"{Program.ApplicationMeta.Location}\"", "start=auto");
                        break;
                    case "uninstall":
                        Program.BatchExec(new BatchExecMeta()
                            {FileName = "net.exe", Arguments = new string[] {"stop", ApplicationMeta.Name}, Output = false});
                        Program.BatchExec("sc.exe", "delete", ApplicationMeta.Name);
                        break;
                    case "start":
                    case "pause":
                    case "continue":
                    case "stop":
                        Program.BatchExec("net.exe", command, ApplicationMeta.Name);
                        break;
                    default:
                        Console.WriteLine($"The program must be configured as a service ({ApplicationMeta.Name}).");
                        if (!isAdministrator)
                        {
                            Console.WriteLine();
                            Console.WriteLine("The use requires an administrator.");
                            Console.WriteLine("Please use a command line as administrator.");
                        }
                        Console.WriteLine();
                        Console.WriteLine($"usage: {ApplicationMeta.File} <command>");
                        Console.WriteLine();
                        Console.WriteLine("    install   creates the service");
                        Console.WriteLine("    uninstall deletes the service");
                        Console.WriteLine();
                        Console.WriteLine("The service supports start, pause, continue and stop.");
                        Console.WriteLine();
                        Console.WriteLine("    start     starts when not running");
                        Console.WriteLine("    pause     pauses when running");
                        Console.WriteLine("    continue  continues when paused");
                        Console.WriteLine("    stop      stops when running");
                        Console.WriteLine();
                        Console.WriteLine("When the program ends, the priority of the changed processes is restored.");
                        break;
                }
                return;
            }
            
            ServiceBase.Run(new Service());
        }

        private struct BatchExecMeta
        {
            internal string   FileName;
            internal string[] Arguments;
            internal bool     Output;
        }

        private static void BatchExec(string fileName, params string[] arguments)
        {
            Program.BatchExec(new BatchExecMeta() {FileName = fileName, Arguments = arguments, Output = true});
        }

        private static void BatchExec(BatchExecMeta batchExecMeta)
        {
            if (batchExecMeta.Output)
            {
                Console.WriteLine(batchExecMeta.FileName + " " + String.Join(" ", batchExecMeta.Arguments));
                Console.WriteLine();
            }

            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow  = true,

                WindowStyle = ProcessWindowStyle.Hidden,

                FileName  = batchExecMeta.FileName,
                Arguments = String.Join(" ", batchExecMeta.Arguments),

                RedirectStandardError  = true,
                RedirectStandardOutput = true
            };
            process.Start();
            process.WaitForExit();

            if (!batchExecMeta.Output)
                return;
            
            var standardErrorOutput = (process.StandardError.ReadToEnd()).Trim();
            if (standardErrorOutput.Length > 0)
            {
                Console.Out.Write(standardErrorOutput);
                Console.WriteLine();
            }
            var standardOutput = (process.StandardOutput.ReadToEnd()).Trim();
            if (standardOutput.Length > 0)
            {
                Console.Out.Write(standardOutput);
                Console.WriteLine();
            }
        }
    }
    
    internal class Service : ServiceBase
    {
        private const int MAXIMUM_CPU_LOAD_PERCENT = 25;
        private const int MEASURING_TIME_MILLISECONDS = 1000;

        private int processId;

        private BackgroundWorker backgroundWorker;
        
        private bool interrupt;
        
        private struct ProcessMonitor
        {
            internal int processId;
            internal ProcessPriorityClass processPriorityClassInitial;
            internal PerformanceCounter performanceCounter;
        }

        private readonly List<ProcessMonitor> processMonitorsDecreased;  
        
        private readonly Dictionary<int, ProcessMonitor> processMonitors;

        private readonly EventLog eventLog;

        public Service()
        {
            this.eventLog = new EventLog();
            this.eventLog.Source = Program.ApplicationMeta.Name; 
            
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = false;

            this.processMonitors = new Dictionary<int, ProcessMonitor>();
            this.processMonitorsDecreased = new List<ProcessMonitor>();

            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.AboveNormal;
            this.processId = process.Id;
        }

        private void ShiftDownPrioritySmart(List<ProcessMonitor> processMonitors)
        {
            foreach (var processMonitor in processMonitors)
            {
                Thread.Sleep(5);
                try
                {
                    using (var process = Process.GetProcessById(processMonitor.processId))
                    {
                        if (!this.processMonitors.ContainsKey(processMonitor.processId))
                            this.processMonitors.Add(processMonitor.processId, processMonitor);

                        if (ProcessPriorityClass.Idle == process.PriorityClass)
                            continue;

                        var cpuLoad = processMonitor.performanceCounter.NextValue() / Environment.ProcessorCount;
                        if (cpuLoad < MAXIMUM_CPU_LOAD_PERCENT)
                            continue;

                        process.PriorityClass = ProcessPriorityClass.Idle;
                        this.processMonitorsDecreased.Add(processMonitor);
                    }
                }
                catch (ArgumentException)
                {
                    this.processMonitors.Remove(processMonitor.processId);
                }
                catch (Exception)
                {
                }
            }
        }

        private void RestorePriority()
        {
            foreach (var processMonitor in this.processMonitors.Values)
            {
                try
                {
                    Process.GetProcessById(processMonitor.processId).PriorityClass = processMonitor.processPriorityClassInitial;
                }
                catch (Exception)
                {
                }
            }
        }

        private BackgroundWorker CreateBackgroundWorker()
        {
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.DoWork += (sender, eventArguments) =>
            {
                // How it works:
                // - Periodic measurement of the total CPU load
                //   From a load  of 25 percent (percentage on all cores) the
                //   priorities of the processes start to be checked
                // - Scan all processes and determine the new processes for
                //   which no process monitor with PerformanceCounter exists
                //   and create that for the process.
                // - Analyze CPU consumption based on process monitors and try
                //   to optimize. In doing so, clean up all process monitors
                //   for processes that no longer exist. 
                
                // Analysis:
                // - Check if the process still exists, if not clean it up
                // - If the priority is less than NORMAL, then ignore the process
                // - Measure the load of the "main process" (process name
                //   -- unfortunately I didn't find another easy way)
                // - If the load of the main process is greater than or equal
                //   to 25 percent, try to reduce the priority of the process
                
                // Attempted optimization of processes:
                // - In case of high load, the priority of the processes with
                //   high load is temporarily set to Idle
                // - If the general CPU load decreases to a quarter of the
                //   threshold value, the processes are set to BelowNormal
                // - Original priority normal or higher is restored only at the
                //   end of the service.
                
                long cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                
                while (!this.backgroundWorker.CancellationPending)
                {
                    var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuUsage.NextValue();
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.interrupt)
                        continue;
                    var cpuUsageCurrent = cpuUsage.NextValue() / Environment.ProcessorCount;
                    if (cpuUsageCurrent < MAXIMUM_CPU_LOAD_PERCENT)
                    {
                        // Increasing the priority is done with a delay, so
                        // that the priority is not switched up and down
                        // excessively -- because it can have effects on the
                        // process. 5 seconds is based on the assumption that
                        // a program will try to do things quickly and avoid
                        // delays of several seconds.  
                        
                        if (cpuUsageCurrent < MAXIMUM_CPU_LOAD_PERCENT / 4
                                && this.processMonitorsDecreased.Count > 0
                                && DateTimeOffset.Now.ToUnixTimeMilliseconds() - cpuLoadTiming < 5000)
                        {
                            this.processMonitorsDecreased.ForEach(processMonitor =>
                            {
                                try
                                {
                                    using (var process = Process.GetProcessById(processMonitor.processId))
                                        process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                }
                                catch (Exception)
                                {
                                }
                            });
                            this.processMonitorsDecreased.Clear();
                        } else cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        continue;
                    }
                    
                    Process.GetProcesses().ToList().ForEach(process =>
                    {
                        try
                        {
                            using (process)
                            {
                                if (this.processId == process.Id
                                        || ProcessPriorityClass.Idle == process.PriorityClass
                                        || this.processMonitors.ContainsKey(process.Id))
                                    return;

                                var processMonitor = new ProcessMonitor()
                                {
                                    processPriorityClassInitial = process.PriorityClass,
                                    processId = process.Id,
                                    performanceCounter = new PerformanceCounter("Process", "% Processor Time",
                                        process.ProcessName, true)
                                };
                                processMonitor.performanceCounter.NextValue();
                                
                                this.processMonitors.Add(processMonitor.processId, processMonitor);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    });

                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.backgroundWorker.CancellationPending)
                        return;

                    this.ShiftDownPrioritySmart(this.processMonitors.Values.ToList());
                    
                    cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                this.RestorePriority();
            };

            return backgroundWorker;
        }

        internal void OnDebug(params string[] options)
        {
            this.OnStart(options);
            while (!this.backgroundWorker.CancellationPending
                    && !this.interrupt)
                Thread.Sleep(1000);
            this.OnStop();
        }

        protected override void OnStart(string[] options)
        { 
            if (this.backgroundWorker != null
                    && this.backgroundWorker.IsBusy)
                return;

            this.eventLog.WriteEntry(Program.VERSION, EventLogEntryType.Information);

            this.backgroundWorker = this.CreateBackgroundWorker();
            this.backgroundWorker.RunWorkerAsync();

            this.eventLog.WriteEntry("Service started.", EventLogEntryType.Information);
        }

        protected override void OnPause()
        {
            if (this.interrupt)
                return;
            this.interrupt = true;
            this.eventLog.WriteEntry("Service paused.", EventLogEntryType.Information);
        }

        protected override void OnContinue()
        {
            if (!this.interrupt)
                return;
            this.interrupt = false;
            this.eventLog.WriteEntry("Service continued.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            if (!this.backgroundWorker.IsBusy)
                return;

            this.backgroundWorker.CancelAsync();
            while (this.backgroundWorker.IsBusy)
                Thread.Sleep(25);
            this.backgroundWorker = null;
            this.eventLog.WriteEntry("Service stopped.", EventLogEntryType.Information);
        }
    }
}