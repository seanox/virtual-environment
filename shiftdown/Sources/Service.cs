// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment ShiftDown
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
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace VirtualEnvironment.ShiftDown
{
    internal class Service : ServiceBase
    {
        private Settings           _settings;
        private BackgroundWorker   _backgroundWorker;
        private BackgroundWorker[] _backgroundMonitoringWorkers;
        private BackgroundWorker   _backgroundCleanUpWorker;

        private volatile ProcessPriorityClass _processPriorityClass;
        private volatile Process[]            _processes;
        
        private volatile Dictionary<int, ProcessMonitor> _processMonitors;

        private volatile List<string> _processPrioritySuspensions;
        private volatile List<int>    _processPrioritySuspensionsProcesses;
        private volatile List<string> _processPriorityDecreases;

        private volatile bool _paused;
        
        private readonly EventLog _eventLog;

        internal class ProcessMonitor
        {
            internal readonly Process Process;
            internal readonly string ProcessName;
            internal readonly ProcessPriorityClass PriorityClassInitial;

            private long _processTimeTimestamp;
            private long _processTime;
            
            private ProcessMonitor(Process process)
            {
                Process = process;
                ProcessName = process.ProcessName;
                PriorityClassInitial = process.PriorityClass;

                _processTimeTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                _processTime = (long)process.TotalProcessorTime.TotalMilliseconds;
            }

            internal static ProcessMonitor Create(Process process)
            {
                return new ProcessMonitor(process);
            }

            internal double ProcessorLoad
            {
                get
                {
                    Process.Refresh();
                    var timeTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    var measuringTime = (timeTimestamp - _processTimeTimestamp) * Environment.ProcessorCount;
                    var measuringProcessTime = Process.TotalProcessorTime.TotalMilliseconds - _processTime;
                    _processTimeTimestamp = timeTimestamp;
                    _processTime = (long)Process.TotalProcessorTime.TotalMilliseconds;
                    return measuringProcessTime * 100 / measuringTime;
                }
            }
        }

        internal Service()
        {
            _eventLog = new EventLog();
            _eventLog.Source = Program.ApplicationMeta.Name;
            
            _eventLog.WriteEntry(Program.VERSION, EventLogEntryType.Information);
            _eventLog.WriteEntry("Service initialized.", EventLogEntryType.Information);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                _eventLog.WriteEntry(eventArgs.ExceptionObject?.ToString(), EventLogEntryType.Error);
        }

        private bool IsInterrupted
        {
            get
            {
                lock (_processPrioritySuspensionsProcesses)
                    return _paused || _processPrioritySuspensionsProcesses.Count > 0;
            }
        }

        private ProcessMonitor AssembleProcessMonitor(Process process)
        {
            // Discards all information about the process that was cached.
            // Microsoft also likes to cache and It took a while to understand
            // why the priority is not always up to date :-)
            process.Refresh();

            lock (_processMonitors)
                if (_processMonitors.ContainsKey(process.Id))
                    return _processMonitors[process.Id];
            
            // Access to the priority of system processes is not allowed.
            // For these processes an empty ProcessMonitor is created so that
            // these processes are not detected as new each time.

            var processMonitor = ProcessMonitor.Create(process);
            lock (_processMonitors)
                _processMonitors.Add(process.Id, processMonitor);
                
            return processMonitor;
        }

        private BackgroundWorker CreateBackgroundMonitoringWorker(int segment)
        {
            var backgroundMonitoringWorker = new BackgroundWorker();
            backgroundMonitoringWorker.WorkerSupportsCancellation = true;
            backgroundMonitoringWorker.WorkerReportsProgress = false;
            backgroundMonitoringWorker.DoWork += (sender, eventArgs) =>
            {
                while (!backgroundMonitoringWorker.CancellationPending)
                {
                    Thread.Sleep(25);
                    if (IsInterrupted)
                        continue;

                    // It is better to work with a copy in case the list is
                    // modified in the meantime.
                    var processList = _processes?.ToList();
                    if (processList == null
                            || processList.Count <= 0)
                        continue;

                    var processPriorityClass = _processPriorityClass;
                    
                    var rangeSize = Math.Ceiling(processList.Count / (decimal)_settings.Workers);
                    var rangeStart = Math.Max(segment -1, Math.Min(((segment -1) *rangeSize), processList.Count));
                    var rangeEnd = Math.Max(rangeStart +1, Math.Min(rangeStart +rangeSize -1, processList.Count));
                    if (rangeStart >= processList.Count)
                        continue;
                    
                    var rangeCount = Math.Max(0, Math.Min(processList.Count -1, rangeEnd) -rangeStart +1);
                    processList.GetRange((int)rangeStart, (int)rangeCount).ForEach(process =>
                    {
                        if (backgroundMonitoringWorker.CancellationPending)
                            return;

                        Thread.Sleep(25);
                        if (IsInterrupted)
                            return;

                        try
                        {
                            // Protected processes are used as null and ignored.
                            var processMonitor = AssembleProcessMonitor(process);
                            if (processMonitor == null)
                                return;

                            if (_processPrioritySuspensions.Contains(processMonitor.ProcessName.ToLower()))
                                lock (_processPrioritySuspensionsProcesses)
                                    if (!_processPrioritySuspensionsProcesses.Contains(process.Id))
                                        _processPrioritySuspensionsProcesses.Add(process.Id);

                            if (IsInterrupted)
                                return;
                            
                            var processorLoad = (int)processMonitor.ProcessorLoad;

                            // ProcessPriorityDecreases: If strong activity has
                            // been detected for a process, then processes with
                            // the same name are also prioritized down.
                            // For this, the process names are collected here.
                            
                            if (processorLoad >= _settings.ProcessorLoadMax)
                                lock (_processPriorityDecreases)
                                    if (!_processPriorityDecreases.Contains(processMonitor.ProcessName))
                                        _processPriorityDecreases.Add(processMonitor.ProcessName);
                            if (processorLoad >= _settings.ProcessorLoadMax)
                                process.PriorityClass = ProcessPriorityClass.Idle;
                            
                            lock (_processPriorityDecreases)
                                if (ProcessPriorityClass.Idle.Equals(processPriorityClass)
                                        && !ProcessPriorityClass.Idle.Equals(process.PriorityClass)
                                        && _processPriorityDecreases.Contains(processMonitor.ProcessName))
                                    process.PriorityClass = processPriorityClass;
                            
                            if (processorLoad < _settings.ProcessorLoadMax
                                    && ProcessPriorityClass.BelowNormal.Equals(processPriorityClass)
                                    && ProcessPriorityClass.Idle.Equals(process.PriorityClass)
                                    && !processMonitor.PriorityClassInitial.Equals(process.PriorityClass))
                                process.PriorityClass = processPriorityClass;
                            
                            lock (_processPriorityDecreases)
                                if (ProcessPriorityClass.BelowNormal.Equals(processPriorityClass)
                                        && !ProcessPriorityClass.BelowNormal.Equals(process.PriorityClass)
                                        && !ProcessPriorityClass.Idle.Equals(processMonitor.PriorityClassInitial)
                                        && _processPriorityDecreases.Contains(processMonitor.ProcessName))
                                    process.PriorityClass = processPriorityClass;
                        }
                        catch (Exception)
                        {
                            lock (_processMonitors)
                                _processMonitors[process.Id] = null;
                        }
                    });
                }
            };
            return backgroundMonitoringWorker;
        }

        internal void OnDebug(params string[] options)
        {
            OnStart(options);
            while (!_backgroundWorker.CancellationPending)
                Thread.Sleep(1000);
            OnStop();
        }
        
        private static bool ProcessExists(int processId)
        {
            try
            {
                Process.GetProcessById(processId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void CleanUpProcessMonitors()
        {
            // Not all processes need to be reviewed. Only the processes where
            // an error occurred during access. Then access to the processes is
            // denied or the processes have expired. In these cases, a null
            // value is set for the ProcessMonitor for the process ID.

            Dictionary<int, ProcessMonitor> processMonitors;
            lock (_processMonitors)
                processMonitors = new Dictionary<int, ProcessMonitor>(_processMonitors);
            
            foreach (KeyValuePair<int, ProcessMonitor> entry in processMonitors)
            {
                Thread.Sleep(100);
                if (IsInterrupted)
                    return;
                if (entry.Value == null
                        && !ProcessExists(entry.Key))
                    lock (_processMonitors)
                        _processMonitors.Remove(entry.Key);
            }
        }
        
        private void CleanUpProcessSuspensions()
        {
            int[] processIds;
            lock (_processPrioritySuspensionsProcesses)
                processIds = _processPrioritySuspensionsProcesses.ToArray();
            foreach (var processId in processIds)
            {
                Thread.Sleep(100);
                if (_paused)
                    return;
                if (ProcessExists(processId))
                    continue;
                lock (_processPrioritySuspensionsProcesses)
                    _processPrioritySuspensionsProcesses.Remove(processId);
            }
        }

        protected override void OnStart(string[] options)
        { 
            if (_backgroundWorker != null
                    && _backgroundWorker.IsBusy)
                return;

            _eventLog.WriteEntry("Service start initiated.", EventLogEntryType.Information);
            
            _settings = Settings.Load();
            _processPriorityClass = ProcessPriorityClass.BelowNormal;
            _processMonitors = new Dictionary<int, ProcessMonitor>();
            _processPrioritySuspensions = new List<string>(_settings.Suspensions);
            _processPrioritySuspensionsProcesses = new List<int>();
            _processPriorityDecreases = new List<string>(_settings.Decreases);

            // The own priority must be higher than normal, so that the service
            // itself gets enough CPU time at high load. 

            // The service process is treated as a system process and uses an
            // empty ProcessMonitor, so it is excluded from prioritization.

            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.AboveNormal;
            _processMonitors.Add(process.Id, null);

            _backgroundMonitoringWorkers = new BackgroundWorker[_settings.Workers];
            for (var index = 1; index <= _settings.Workers; index++)
                _backgroundMonitoringWorkers[index -1] = CreateBackgroundMonitoringWorker(index);
            foreach (var worker in _backgroundMonitoringWorkers)
                worker.RunWorkerAsync();
            
            // The background worker continuously measures the total CPU load
            // and determines the priority that the workers have to use.
            // Primarily it is decided here between Idle and BelowNormal.  
            // Further, the process list -- but without its own process -- is
            // read and cached so that workers can access the same data later.
            // The own process should not be changed from the priority.
            
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.WorkerReportsProgress = false;
            _backgroundWorker.DoWork += (sender, eventArgs) =>
            {
                using (var cpuLoad = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    var timing = DateTimeOffset.Now.ToUnixTimeSeconds();
                    while (!_backgroundWorker.CancellationPending)
                    {
                        Thread.Sleep(500);
                        if (IsInterrupted)
                            continue;

                        _processes = Process.GetProcesses();

                        var cpuLoadCurrent = cpuLoad.NextValue() / Environment.ProcessorCount; 
                        if (cpuLoadCurrent >= _settings.ProcessorLoadMax)
                            _processPriorityClass = ProcessPriorityClass.Idle;
                        if (DateTimeOffset.Now.ToUnixTimeSeconds() -timing >= _settings.NormalizationTime
                                && ProcessPriorityClass.Idle.Equals(_processPriorityClass))
                            _processPriorityClass = ProcessPriorityClass.BelowNormal;
                        if (cpuLoadCurrent >= _settings.ProcessorLoadMax)
                            timing = DateTimeOffset.Now.ToUnixTimeSeconds();
                    }
                }
            };
            _backgroundWorker.RunWorkerAsync();
            
            _backgroundCleanUpWorker = new BackgroundWorker();
            _backgroundCleanUpWorker.WorkerSupportsCancellation = true;
            _backgroundCleanUpWorker.WorkerReportsProgress = false;
            _backgroundCleanUpWorker.DoWork += (sender, eventArgs) =>
            {
                while (!_backgroundCleanUpWorker.CancellationPending)
                {
                    Thread.Sleep(1000);
                    if (_paused)
                        continue;
                    CleanUpProcessSuspensions();
                    if (IsInterrupted)
                        continue;
                    CleanUpProcessMonitors();
                }
            };
            _backgroundCleanUpWorker.RunWorkerAsync();

            _eventLog.WriteEntry("Service started.", EventLogEntryType.Information);
        }

        protected override void OnPause()
        {
            if (_paused)
                return;
            _paused = true;
            _eventLog.WriteEntry("Service paused.", EventLogEntryType.Information);
        }

        protected override void OnContinue()
        {
            if (!_paused)
                return;
            _paused = false;
            _eventLog.WriteEntry("Service continued.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _eventLog.WriteEntry("Service stop initiated.", EventLogEntryType.Information);

            foreach (var worker in _backgroundMonitoringWorkers)
                worker.CancelAsync();
            
            while (_backgroundMonitoringWorkers.Any(worker => worker.IsBusy))
                Thread.Sleep(25);
            _backgroundMonitoringWorkers = null;

            _backgroundCleanUpWorker.CancelAsync();
            while (_backgroundCleanUpWorker.IsBusy)
                Thread.Sleep(25);
            _backgroundCleanUpWorker = null;
            
            _backgroundWorker.CancelAsync();
            while (_backgroundWorker.IsBusy)
                Thread.Sleep(25);
            _backgroundWorker = null;
            
            _eventLog.WriteEntry("Service stopped.", EventLogEntryType.Information);
        }
    }
}