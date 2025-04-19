// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Startup
// Program starter for the virtual environment.
// Copyright (C) 2025 Seanox Software Solutions
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace VirtualEnvironment.Startup
{
    internal class Runner
    {
        private readonly Application[] _applications;
        private readonly Variable[] _environment;
        private readonly List<Worker> _workers;
        
        internal Runner(Application[] applications, Variable[] environment)
        {
            _applications = applications;
            _environment = environment;
            _workers = new List<Worker>();
        }

        // The process is completed in three stages. First friendly (soft and
        // gentle), then hard and finally force. Taskkill is also called, even
        // if process.HasExited is true, as it should be ensured that the child
        // processes are also terminated. 

        private enum KillMode
        {
            SOFT,
            HARD,
            FORCE
        }
        
        private static void KillProcess(Process process, KillMode mode)
        {
            try
            {
                switch (mode)
                {
                    case KillMode.SOFT:
                        if (!process.HasExited)
                            process.CloseMainWindow();
                        break;

                    case KillMode.HARD:
                        var taskkill = new Process()
                        {
                            StartInfo = new ProcessStartInfo()
                            {
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                FileName = "taskkill.exe",
                                Arguments = $"/t /pid {process.Id}"
                            }
                        };
                        taskkill.Start();
                        taskkill.WaitForExit();
                        break;

                    case KillMode.FORCE:
                        if (process.HasExited)
                            break;
                        Process.GetProcessById(process.Id);
                        process.Kill();
                        break;
                }
            }
            catch (Exception exception)
            {
                if (!(exception is ArgumentException)
                    && !(exception is InvalidOperationException))
                    Messages.Push(Messages.Type.Error, exception.Message);
            }
            finally
            {
                if (process.HasExited)
                    process.Dispose();
            }
        }

        private static Process RunShellExecute(Application application, Variable[] environment)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(application.Destination, String.Join(" ", application.Arguments ?? ""))
                {
                    WorkingDirectory = application.WorkingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    RedirectStandardError = false,
                    RedirectStandardOutput = false
                }
            };
            foreach (var variable in environment ?? Array.Empty<Variable>())
                process.StartInfo.EnvironmentVariables[variable.Name] = variable.Value;
            process.Start();
            return process;
        }
        
        private static Process RunApplication(Application application, Variable[] environment)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(application.Destination, String.Join(" ", application.Arguments ?? ""))
                {
                    WorkingDirectory = application.WorkingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            foreach (var variable in environment ?? Array.Empty<Variable>())
                process.StartInfo.EnvironmentVariables[variable.Name] = variable.Value;
            Action<Messages.Type, Process, string> onProcessData = (type, module, data) =>
            {
                if (String.IsNullOrWhiteSpace(data))
                    return;
                var context = Messages.Type.Error == type ? "ERROR" : "OUTPUT";
                var output = $"{context} [{Path.GetFileNameWithoutExtension(module.ProcessName)}] {data.Trim()}";
                Messages.Push(Messages.Type.Text, output);
            };
            process.OutputDataReceived += (sender, eventArgs) =>
                onProcessData(Messages.Type.Text, sender as Process, eventArgs.Data);
            process.ErrorDataReceived += (sender, eventArgs) =>
                onProcessData(Messages.Type.Error, sender as Process, eventArgs.Data);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            return process;
        }

        internal void StartAndWaitForExit()
        {
            foreach (var application in _applications)
            {
                var useShellExecute = !Regex.IsMatch(application.Destination, @"\.(exe|com)$", RegexOptions.IgnoreCase);
                var process = useShellExecute
                    ? RunShellExecute(application, _environment)
                    : RunApplication(application, _environment);
                _workers.Add(new Worker(process, application.WaitForExit));
            }
            
            // The process will intentionally block here. The following events
            // will end the processes and the program will continue:
            // - Session Ending
            // - Windows Shutdown
            // ShutdownBlockReasonCreate attempts to maintain this process and
            // execute the subsequent logic to the end.  

            foreach (var worker in _workers)
                if (worker.WaitForExit)
                    worker.Process.WaitForExit();
            
            Terminate();
        }
        
        internal void Terminate()
        {
            var processes = _workers.Select(worker =>
                worker.Process).ToList();
            processes.ForEach(process =>
                KillProcess(process, KillMode.SOFT));
            if (processes.Any(process => !process.HasExited))
                Thread.Sleep(3000);
            processes.ForEach(process =>
                KillProcess(process, KillMode.HARD));
            if (processes.Any(process => !process.HasExited))
                Thread.Sleep(1000);
            
            // Killing the processes can block (e.g. system protection by the
            // virus scanner). Therefore, we try up to 3 times with a small
            // pause. After three attempts, the process is ignored.
            
            for (var index = 0; index < 3; index++)
            {
                processes.ForEach(process =>
                    KillProcess(process, KillMode.FORCE));
                Thread.Sleep(1000);
            }
        }
    
        private class Worker
        {
            internal Process Process { get; }
            internal bool WaitForExit { get; }

            internal Worker(Process process, bool waitForExit)
            {
                Process = process;
                WaitForExit = waitForExit;
            }
        }
    }
}