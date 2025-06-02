// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Launcher
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VirtualEnvironment.Launcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Messages.Subscribe(new Subscription());
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Handle the Windows shutdown event, but only if the launcher is
            // running in the context of the virtual environment.
            var applicationDrive = Path.GetPathRoot(Assembly.GetExecutingAssembly().Location).Substring(0, 2);
            var platformDrive = Environment.GetEnvironmentVariable("PLATFORM_HOMEDRIVE");
            if (string.Equals(applicationDrive, platformDrive, StringComparison.OrdinalIgnoreCase))
                SystemEvents.SessionEnding += OnSessionEnding;          

            // Settings are monitored by the main program. When changes are
            // detected, the control is closed and set up again with the changed
            // settings. If an error occurs when loading the settings, a message
            // is shown and existing settings continue to be used.
            
            // The logic for the reload is a bit more complicated, because 
            // Application.Run blocks and the detection of changed settings runs
            // in the control as background timer. But since Windows expects a
            // STA (Single Thread Apartment) for some functions like
            // Icon.ExtractAssociatedIcon, the control must run in the main
            // thread, otherwise it is an MTA (Multi Thread Apartment) and
            // causes problems.

            Settings settings = null;
            Control  control  = null;
                
            while (true)
            {
                try
                {
                    if (Settings.IsUpdateAvailable()
                            || settings == null)
                        settings = Settings.Load();
                    using (control = new Control(settings, control == null))
                        Application.Run(control);
                    GC.Collect();
                }
                catch (Exception exception)
                {
                    // IOException due to asynchronous accesses are ignored
                    if (exception is IOException
                            && control != null)
                        continue;
                    
                    var message = "An unexpected error has occurred."
                            + $"{Environment.NewLine}{Environment.NewLine}{exception}";
                    if (exception is Settings.SettingsException)
                        message = exception.Message;
                    
                    Messages.Push(Messages.Type.Error, message);
                    if (control == null)
                        Messages.Push(Messages.Type.Exit);
                }
                Thread.Sleep(25);
            }
        }
        
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs exceptionEvent)
        {
            Messages.Push(Messages.Type.Error,
                    "Unexpected error occurred.",
                    (Exception)exceptionEvent.ExceptionObject);
        }
        
        private static void OnSessionEnding(object sender, SessionEndingEventArgs eventArgs)
        {
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var applicationDrive = Path.GetPathRoot(applicationPath).Substring(0, 2);
            var platformDrive = Environment.GetEnvironmentVariable("PLATFORM_HOMEDRIVE");
            var platformDisk = Environment.GetEnvironmentVariable("PLATFORM_PLATFORM_DISK");

            if (!string.Equals(applicationDrive, platformDrive, StringComparison.OrdinalIgnoreCase))
                return;
            
            var workdir = Path.GetDirectoryName(applicationPath);
            var library = Path.Combine(workdir, "platform.dll");
            if (!File.Exists(library))
                return;
            
            try
            {
                var assembly = Assembly.LoadFrom(library);
                var type = assembly.GetType("VirtualEnvironment.Platform.Service");
                var method = type.GetMethod("Detach", BindingFlags.NonPublic | BindingFlags.Static);
                method.Invoke(null, new object[] {platformDrive, platformDisk});
            }
            catch (Exception exception)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logfilePath = Path.ChangeExtension(applicationPath, ".log");
                var message = exception is TargetInvocationException
                        ? $"{timestamp} {exception.InnerException.Message}\r\n{exception.InnerException.StackTrace}\r\n"
                        : $"{timestamp} {exception.Message}\r\n{exception.StackTrace}\r\n";
                File.AppendAllText(logfilePath, message);
            }
        }
        
        private class Subscription : Messages.ISubscriber
        {
            private string _context;
            
            private bool _continue;

            private static readonly HashSet<Messages.Type> MESSAGE_TYPE_LIST = new HashSet<Messages.Type>()
            {
                Messages.Type.Error,
                Messages.Type.Warning,
                Messages.Type.Trace,
                Messages.Type.Verbose,
                Messages.Type.Exit
            };

            public void Receive(Messages.Message message)
            {
                if (!MESSAGE_TYPE_LIST.Contains(message.Type)
                        || message.Data is null)
                    return;
                
                // VERBOSE is extended information that lies between TRACE and
                // DEBUG. The message have its own context, as otherwise the
                // information cannot be placed in any context.
                if (Messages.Type.Verbose == message.Type)
                    if (String.IsNullOrWhiteSpace(_context)
                            || String.IsNullOrWhiteSpace(message.Context)
                            || message.Context != _context)
                        return;
                
                // VERBOSE is logged as an extension of TRACE and is therefore
                // converted to TRACE so that logging can pick up the previous
                // context of TRACE and continue the logging block. 
                if (Messages.Type.Verbose == message.Type)
                    message = message.ConvertTo(Messages.Type.Trace);
                
                var content = message.ToString().Trim();
                if (String.IsNullOrWhiteSpace(content))
                    return;

                try
                {
                    if (!_continue)
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
                        var version = assembly.GetName().Version;
                        var build = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(attribute => attribute.Key == "Build")?.Value;
                        var banner = new StringBuilder()
                            .AppendLine($"Seanox Launcher [{version} {build}]")
                            .AppendLine($"{copyright.Replace("©", "(C)")}")
                            .ToString();
                        Console.WriteLine(banner);

                        if (Messages.Type.Exit == message.Type)
                        {
                            Console.WriteLine(Convert.ToString(message.Data));
                            Environment.Exit(0);
                        }
                    }
                    
                    _continue = true;

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var lines = message.ToString()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !String.IsNullOrWhiteSpace(line))
                        .ToArray();
                    
                    Action<string, bool> consoleWriteLine = (line, followup) =>
                    {
                        line = followup ? $" ...  {line}" : line;
                        Console.WriteLine($"{timestamp} {line}");
                    };

                    if (lines.Length > 0)
                    {
                        if (lines[0] != _context)
                            consoleWriteLine(lines[0], false);
                        _context = lines[0];
                        for (var index = 1; index < lines.Length; index++)    
                            consoleWriteLine(lines[index], true);
                    }

                    if (Messages.Type.Error == message.Type)
                        MessageBox.Show(
                                String.Join(System.Environment.NewLine, lines),
                                "Virtual Environment Launcher",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    if (Messages.Type.Exit == message.Type)
                        Environment.Exit(0);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}