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
    internal class Program
    {
        internal static readonly string VERSION = 
            $"Seanox ShiftDown [Version 0.0.0 00000000]{Environment.NewLine}"
               + "Copyright (C) 0000 Seanox Software Solutions";

        private static void Main(params string[] options)
        {
            #if DEBUG
                var service = new Service();
                service.OnDebug();
                return;
            #endif
            
            if (Environment.UserInteractive)
            {
                Console.WriteLine(Program.VERSION);
                Console.WriteLine();

                var applicationLocation = Assembly.GetExecutingAssembly().Location;
                var applicationFile = Path.GetFileName(applicationLocation).Trim();
                var applicationName = Path.GetFileNameWithoutExtension(applicationLocation);
                applicationName = Regex.Replace(applicationName, @"\s+", "");
                if (applicationName.Length <= 0)
                    applicationName = "ShiftDown";

                bool isAdministrator;
                using (var identity = WindowsIdentity.GetCurrent())
                    isAdministrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                
                var command = (isAdministrator && options.Length > 0 ? options[0] : "").Trim().ToLower();
                switch (command)
                {
                    case "install":
                        Program.BatchExec("sc.exe", "create", applicationName, $"binpath=\"{applicationLocation}\"", "start=auto");
                        break;
                    case "uninstall":
                        Program.BatchExec(new BatchExecMeta()
                            {fileName = "sc.exe", arguments = new string[] {"stop", applicationName}, output = false});
                        Program.BatchExec("sc.exe", "delete", applicationName);
                        break;
                    case "start":
                    case "pause":
                    case "continue":
                    case "stop":
                        Program.BatchExec("sc.exe", command, applicationName);
                        break;
                    default:
                        Console.WriteLine($"The program must be configured as a service ({applicationName}).");
                        if (!isAdministrator)
                        {
                            Console.WriteLine();
                            Console.WriteLine("The use requires an administrator.");
                            Console.WriteLine("Please use a command line as administrator.");
                        }
                        Console.WriteLine();
                        Console.WriteLine($"usage: {applicationFile} <command>");
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
            internal string   fileName;
            internal string[] arguments;
            internal bool     output;
        }

        private static void BatchExec(string fileName, params string[] arguments)
        {
            Program.BatchExec(new BatchExecMeta() {fileName = fileName, arguments = arguments, output = true});
        }

        private static void BatchExec(BatchExecMeta batchExecMeta)
        {
            if (batchExecMeta.output)
            {
                Console.WriteLine(batchExecMeta.fileName + " " + String.Join(" ", batchExecMeta.arguments));
                Console.WriteLine();
            }

            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow  = true,

                WindowStyle = ProcessWindowStyle.Hidden,

                FileName  = batchExecMeta.fileName,
                Arguments = String.Join(" ", batchExecMeta.arguments),

                RedirectStandardError  = true,
                RedirectStandardOutput = true
            };
            process.Start();
            process.WaitForExit();
            if (batchExecMeta.output)
            {
                var standardErrorOutput = (process.StandardError.ReadToEnd() ?? "").Trim();
                if (standardErrorOutput.Length > 0)
                {
                    Console.Out.Write(standardErrorOutput);
                    Console.WriteLine();
                }
                var standardOutput = (process.StandardOutput.ReadToEnd() ?? "").Trim();
                if (standardOutput.Length > 0)
                {
                    Console.Out.Write(standardOutput);
                    Console.WriteLine();
                }
            }
        }
    }
    
    internal class Service : ServiceBase
    {
        private const int MAXIMUM_CPU_LOAD_PERCENT = 25;
        private const int MEASURING_TIME_MILLISECONDS = 1000;
        private const int INTERRUPT_MILLISECONDS = 5;

        private int processId;

        private BackgroundWorker backgroundWorker;
        
        private bool interrupt;
        
        private struct ProcessMonitor
        {
            internal int processId;
            internal ProcessPriorityClass processPriorityClassInitial;
            internal PerformanceCounter performanceCounter;
        }
        
        private readonly Dictionary<int, ProcessMonitor> processMonitors;

        public Service()
        {
            this.ServiceName = "ShiftDown";
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = false;

            this.processMonitors = new Dictionary<int, ProcessMonitor>();

            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.AboveNormal;
            this.processId = process.Id;
        }

        private void ShiftDownPrioritySmart(List<ProcessMonitor> processMonitors)
        {
            foreach (var processMonitor in processMonitors)
            {
                Thread.Sleep(INTERRUPT_MILLISECONDS);
                if (this.backgroundWorker.CancellationPending
                        || this.interrupt)
                    return;

                try
                {
                    using (var process = Process.GetProcessById(processMonitor.processId))
                    {
                        if (!this.processMonitors.ContainsKey(processMonitor.processId))
                            this.processMonitors.Add(processMonitor.processId, processMonitor);

                        if (ProcessPriorityClass.Idle == process.PriorityClass
                                || ProcessPriorityClass.BelowNormal == process.PriorityClass)
                            continue;

                        var cpuLoad = processMonitor.performanceCounter.NextValue() / Environment.ProcessorCount;
                        if (cpuLoad < MAXIMUM_CPU_LOAD_PERCENT)
                            continue;

                        process.PriorityClass = ProcessPriorityClass.BelowNormal;    
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
                // - Fast way: query all established ProcessMonitors with
                //      PerformanceCounter 
                // - Analyze the load by the process and try to reduce the
                //   priority if it has a load of more than 25 percent.
                // - Slower way: Query all active processes, ignore those wher
                //    a ProcessMonitor with PerformanceCounter already exists.
                
                // Analysis:
                // - Check if the process still exists, if not clean it up
                // - If the priority is less than NORMAL, then ignore the process
                // - Measure the load of the "main process" (process name
                //   -- unfortunately I didn't find another easy way)
                // - If the load of the main process is greater than or equal
                //   to 25 percent, try to reduce the priority of the process
                
                while (!this.backgroundWorker.CancellationPending)
                {
                    var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuUsage.NextValue();
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.interrupt
                            || cpuUsage.NextValue() / Environment.ProcessorCount < MAXIMUM_CPU_LOAD_PERCENT)
                        continue;
                    
                    this.ShiftDownPrioritySmart(this.processMonitors.Values.ToList());
                    var processMonitorsTemp = new List<ProcessMonitor>();
                    
                    Process.GetProcesses().ToList().ForEach(process =>
                    {
                        try
                        {
                            using (process)
                            {
                                if (this.processId == process.Id
                                        || ProcessPriorityClass.Idle == process.PriorityClass
                                        || ProcessPriorityClass.BelowNormal == process.PriorityClass
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
                                processMonitorsTemp.Add(processMonitor);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    });
                    
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (this.backgroundWorker.CancellationPending
                            || this.interrupt)
                        return;
                    
                    this.ShiftDownPrioritySmart(processMonitorsTemp);
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

        protected override void OnStart(params string[] options)
        { 
            if (this.backgroundWorker != null
                    && this.backgroundWorker.IsBusy)
                return;

            EventLog.WriteEntry(Program.VERSION, EventLogEntryType.Information);

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

            this.backgroundWorker.CancelAsync();
            while (this.backgroundWorker.IsBusy)
                Thread.Sleep(25);
            this.backgroundWorker = null;
            EventLog.WriteEntry("Service stopped.", EventLogEntryType.Information);
        }
    }
}