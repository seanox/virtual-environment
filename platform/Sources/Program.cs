// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VirtualEnvironment.Platform
{
    internal static class Program
    {
        [STAThread]
        private static void Main (params string[] arguments)
        {
            if (arguments == null)
                arguments = new string[] {};
            var task = (arguments.ElementAtOrDefault(1) ?? "").ToLower();
            var drive = (arguments.ElementAtOrDefault(0) ?? "").ToUpper();
            if (!new Regex("^[A-Z]:$").IsMatch(drive))
                task = "";
            
            if (Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly()
                    && !new Regex("^(compact|attach|detach|shortcuts)$", RegexOptions.IgnoreCase).IsMatch(task))
                throw new InvalidOperationException("Requires a drive letter (A-Z) and a method: [compact|attach|detach|shortcuts].");
            
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            Messages.Subscribe(new Subscription());
            
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var diskFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".vhdx");
            var workerTask = Worker.Task.Usage;
            switch (task)
            {
                case "create":
                    workerTask = Worker.Task.Create;
                    break;
                case "compact":
                    workerTask = Worker.Task.Compact;
                    break;
                case "attach":
                    workerTask = Worker.Task.Attach;
                    break;
                case "detach":
                    workerTask = Worker.Task.Detach;
                    break;
                case "shortcuts":
                    workerTask = Worker.Task.Shortcuts;
                    break;
            }

            // In the case that the main method is called as a DLL. If a window
            // from the calling program already exists, no new one should or may
            // be established as an application, as this will otherwise cause an
            // InvalidOperationException. 
            
            if (!Application.MessageLoop
                    && Application.OpenForms.Count <= 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Worker(workerTask, drive, diskFile));
            }
            else new Worker(workerTask, drive, diskFile).Show();
        }
        
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs exceptionEvent)
        {
            Messages.Push(Messages.Type.Error, exceptionEvent.ExceptionObject as Exception);
        }
        
        private class Subscription : Messages.ISubscriber
        {
            private string _context;
            
            private bool _continue;

            private static readonly HashSet<Messages.Type> MESSAGE_TYPE_LIST = new HashSet<Messages.Type>()
            {
                Messages.Type.Error,
                Messages.Type.Warning,
                Messages.Type.Trace
            };

            public void Receive(Messages.Message message)
            {
                if (!MESSAGE_TYPE_LIST.Contains(message.Type)
                        || message.Data is null)
                    return;
                
                var content = message.ToString().Trim();
                if (String.IsNullOrWhiteSpace(content))
                    return;

                try
                {
                    var applicationPath = Assembly.GetExecutingAssembly().Location;
                    var logfilePath = Path.Combine(Path.GetDirectoryName(applicationPath),
                        Path.GetFileNameWithoutExtension(applicationPath) + ".log");

                    if (!_continue)
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var name = assembly.GetName().Name;
                        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
                        var version = assembly.GetName().Version;
                        var build = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(attribute => attribute.Key == "Build")?.Value;
                        var banner = new StringBuilder()
                            .AppendLine($"Seanox {name} [Version {version} {build}]")
                            .AppendLine($"{copyright.Replace("©", "(C)")}")
                            .ToString();
                        
                        if (!File.Exists(logfilePath)
                                || new FileInfo(logfilePath).Length <= 0)
                            File.WriteAllLines(logfilePath, new[] {banner});
                    }
                    else File.AppendAllText(logfilePath, Environment.NewLine);
                    
                    _continue = true;

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var lines = message.ToString()
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToArray();
                    
                    Action<string, bool> logfileWriteLine = (line, followup) =>
                    {
                        line = followup ? $" ... {line}" : line;
                        File.AppendAllLines(logfilePath, new[] { $"{timestamp} {line}" });
                    };

                    for (var index = 0; index < lines.Length; index++)
                    {
                        if (index == 0)
                        {
                            if (lines[index] != _context)
                                logfileWriteLine(lines[index], false);
                            _context = lines[index];
                        }
                        else logfileWriteLine(lines[index], true);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}