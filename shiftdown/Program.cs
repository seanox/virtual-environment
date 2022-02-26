// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Shiftdown
// Downgrades the priority of overactive processes.
// Copyright (C) 2022 Seanox Software Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
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
                            {FileName = "net.exe", Arguments = new[] {"stop", ApplicationMeta.Name}, Output = false});
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
                Console.Error.Write(standardErrorOutput + Environment.NewLine);
            
            var standardOutput = (process.StandardOutput.ReadToEnd()).Trim();
            if (standardOutput.Length > 0)
                Console.Out.Write(standardOutput + Environment.NewLine);
        }
    }
    
    internal class Service : ServiceBase
    {
        private const int MAXIMUM_CPU_LOAD_PERCENT = 25;
        private const int MEASURING_TIME_MILLISECONDS = 1000;

        private BackgroundWorker _backgroundWorker;
        
        private bool _interrupt;
        
        private struct ProcessMonitor
        {
            internal Process Process;
            internal ProcessPriorityClass PriorityClassInitial;
            internal PerformanceCounter PerformanceCounter;
        }

        private readonly List<ProcessMonitor> _processMonitorsDecreased;  
        
        private readonly Dictionary<int, ProcessMonitor> _processMonitors;

        private readonly EventLog _eventLog;

        public Service()
        {
            _eventLog = new EventLog();
            _eventLog.Source = Program.ApplicationMeta.Name; 
            
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = false;

            _processMonitors = new Dictionary<int, ProcessMonitor>();
            _processMonitorsDecreased = new List<ProcessMonitor>();

            // The own priority must be higher than normal, so that the service
            // itself gets enough CPU time at high load.

            // The service process is treated as a system process and uses an
            // empty ProcessMonitor, so it is excluded from prioritization.

            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.AboveNormal;
            _processMonitors.Add(process.Id, new ProcessMonitor());
        }

        private void ShiftDownPrioritySmart(List<ProcessMonitor> processMonitors)
        {
            // Processes where access to the priority is not allowed are
            // filtered and ignored (e.g. system processes)
            processMonitors = processMonitors.FindAll(processMonitor => processMonitor.Process != null);
            
            foreach (var processMonitor in processMonitors)
            {
                Thread.Sleep(25);
                
                try
                {
                    var process = processMonitor.Process;
                    
                    // Discards all information about the assigned process that
                    // was cached in the process component. Microsoft also
                    // likes to cache and it took me a while to understand why
                    // the priority is not always up to date :-)
                    process.Refresh();
                    if (ProcessPriorityClass.Idle.Equals(process.PriorityClass))
                        continue;

                    var cpuLoad = processMonitor.PerformanceCounter.NextValue() / Environment.ProcessorCount;
                    if (cpuLoad < MAXIMUM_CPU_LOAD_PERCENT)
                        continue;

                    process.PriorityClass = ProcessPriorityClass.Idle;
                    
                    if (!_processMonitorsDecreased.Contains(processMonitor))
                        _processMonitorsDecreased.Add(processMonitor);
                }
                catch (InvalidOperationException)
                {
                    _processMonitors.Remove(processMonitor.Process.Id);
                }
                catch (Exception)
                {
                }
            }
        }

        private void RestorePriority()
        {
            foreach (var processMonitor in _processMonitors.Values)
            {
                try
                {
                    processMonitor.Process.PriorityClass = processMonitor.PriorityClassInitial;
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
                
                var cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                
                var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuUsage.NextValue();

                while (!_backgroundWorker.CancellationPending)
                {
                    Thread.Sleep(MEASURING_TIME_MILLISECONDS);
                    if (_interrupt)
                        continue;
                    
                    // There are different views on the interpretation of the
                    // value. I refer to the statement that the value
                    // represents the average over all cores. Over 100% is also
                    // possible -- calculative or when the CPU uses Turbo Boost.
                    // Per WMI each core can be queried, but it probably
                    // doesn't matter with the question if the CPU is generally
                    // loaded.
                    
                    // The list of the ProcessMonitor always contains at least
                    // the process of the service. The use of Any function is
                    // not possible here.
                    
                    var cpuUsageCurrent = cpuUsage.NextValue();
                    if (cpuUsageCurrent < MAXIMUM_CPU_LOAD_PERCENT
                            && _processMonitors.Count() > 1)
                    {
                        // Increasing the priority is done with a delay, so
                        // that the priority is not switched up and down
                        // excessively -- because it can have effects on the
                        // process. 5 seconds is based on the assumption that
                        // a program will try to do things quickly and avoid
                        // delays of several seconds.  
                        
                        if (cpuUsageCurrent < MAXIMUM_CPU_LOAD_PERCENT / 4
                                && _processMonitorsDecreased.Any()
                                && DateTimeOffset.Now.ToUnixTimeMilliseconds() - cpuLoadTiming < 5000)
                        {
                            _processMonitorsDecreased.ForEach(processMonitor =>
                            {
                                try
                                {
                                    processMonitor.Process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                }
                                catch (Exception)
                                {
                                }
                            });
                            _processMonitorsDecreased.Clear();
                        } else cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        continue;
                    }
                    
                    // Control of the known loaders.
                    ShiftDownPrioritySmart(_processMonitorsDecreased);

                    // To determine only new processes Except is a nice
                    // function for deltas in lists used for this purpose.
                    var processList = Process.GetProcesses().ToList();
                    var processesListCurrent = (from process in processList select process.Id).ToList();
                    var processListDelta = processesListCurrent.Except(_processMonitors.Keys.ToList());
                    processList = processList.FindAll(process => processListDelta.Contains(process.Id));

                    foreach (var process in processList)
                    {
                        if (_processMonitors.ContainsKey(process.Id))
                            continue;
                        
                        // Access to the priority of system processes is not
                        // allowed. For these processes an empty ProcessMonitor
                        // is created so that these processes are not detected
                        // as new each time.

                        var processMonitor = new ProcessMonitor();
                        _processMonitors.Add(process.Id, processMonitor);
                        
                        try
                        {
                            processMonitor.Process = process;
                            processMonitor.PriorityClassInitial = process.PriorityClass;
                            processMonitor.PerformanceCounter = new PerformanceCounter("Process", "% Processor Time",
                                    process.ProcessName, true);
                            processMonitor.PerformanceCounter.NextValue();
                            
                            // Structures behave are strange :-|
                            // Each change of the values creates a new
                            // instance. That's why the existing reference in
                            // the list is an original of which with each
                            // change a copy is created as a new instance.
                            // Therefore the entry in the list does not get
                            // anything from the changes and so the changed
                            // instance must be packed into the list. 
                            _processMonitors[process.Id] = processMonitor;
                        }
                        catch (Exception)
                        {
                        }
                    }

                    ShiftDownPrioritySmart(_processMonitors.Values.ToList());
                    
                    cpuLoadTiming = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                RestorePriority();
            };

            return backgroundWorker;
        }

        internal void OnDebug(params string[] options)
        {
            OnStart(options);
            while (!_backgroundWorker.CancellationPending
                    && !_interrupt)
                Thread.Sleep(1000);
            OnStop();
        }

        protected override void OnStart(string[] options)
        { 
            if (_backgroundWorker != null
                    && _backgroundWorker.IsBusy)
                return;

            _eventLog.WriteEntry(Program.VERSION, EventLogEntryType.Information);

            _backgroundWorker = CreateBackgroundWorker();
            _backgroundWorker.RunWorkerAsync();

            _eventLog.WriteEntry("Service started.", EventLogEntryType.Information);
        }

        protected override void OnPause()
        {
            if (_interrupt)
                return;
            _interrupt = true;
            _eventLog.WriteEntry("Service paused.", EventLogEntryType.Information);
        }

        protected override void OnContinue()
        {
            if (!_interrupt)
                return;
            _interrupt = false;
            _eventLog.WriteEntry("Service continued.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            if (!_backgroundWorker.IsBusy)
                return;

            _backgroundWorker.CancelAsync();
            while (_backgroundWorker.IsBusy)
                Thread.Sleep(25);
            _backgroundWorker = null;
            _eventLog.WriteEntry("Service stopped.", EventLogEntryType.Information);
        }
    }
}