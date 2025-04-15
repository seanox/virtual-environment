// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Startup
// Program starter for the virtual environment.
// Copyright (C) 2024 Seanox Software Solutions
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace VirtualEnvironment.Startup
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private static Mutex _mutex;
        private static Runner _runner;

        private static readonly Regex COMMAND_PATTERN = new Regex(
            @"^(?:(sync)|(?:(scan)(?::(\d{1-8}))?))$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        
        // Standard depth is based on this path:
        // C:\Users\<Account>\AppData\Local\Packages\<Application>
        private const int SCAN_DEPTH_DEFAULT = 6;

        static Program()
        {
            // Set the default culture to "en-US" to ensure consistent
            // formatting and parsing of data types, such as numbers (decimal
            // point ".") and dates (MM/dd/yyyy format), regardless of system or
            // regional settings.

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        }

        private static bool CheckAndFocusExistingInstance(Application application)
        {
            var applicationName = Path.GetFileNameWithoutExtension(application.Destination);
            var process = Process.GetProcesses()
                .FirstOrDefault(entry => 
                    String.Equals(entry.ProcessName, applicationName, StringComparison.OrdinalIgnoreCase));
            if (process == null)
                return false;
            if (process.MainWindowHandle == IntPtr.Zero)
                return true;
            ShowWindow(process.MainWindowHandle, SW_RESTORE);
            SetForegroundWindow(process.MainWindowHandle);
            return true;
        }
        
        [STAThread]
        private static void Main(string[] arguments)
        {
            try
            {
                Messages.Subscribe(new Subscription());
                
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                var applicationDirectory = Path.GetDirectoryName(applicationPath);
                var applicationName = Path.GetFileNameWithoutExtension(applicationPath);

                if (arguments.Length > 0)
                {
                    var match = COMMAND_PATTERN.Match(arguments[0]);
                    if (!match.Success)
                        Messages.Push(Messages.Type.Message,
                            $"usage: {Path.GetFileName(applicationPath)} sync|scan(:depth)", true);
                    var command = match.Groups[match.Groups[1].Success ? 1 : 2].Value;
                    var depth = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : SCAN_DEPTH_DEFAULT;
                    if (String.Equals(command, "sync", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!File.Exists(Manifest.File))
                            Messages.Push(Messages.Type.Message, $"Missing manifest file: {Manifest.File}", true);
                        Messages.Push(Messages.Type.Trace, "Read manifest file");
                        var manifest = Manifest.Load();
                        var datastore = new Datastore(manifest);
                        Messages.Push(Messages.Type.Trace, "Create mirror directory");
                        datastore.CreateMirrorDirectory();
                        Messages.Push(Messages.Type.Trace, "Mirror registry");
                        datastore.MirrorRegistry();
                        Messages.Push(Messages.Type.Trace, "Mirror file system");
                        datastore.MirrorFileSystem();
                        Messages.Push(Messages.Type.Trace, "Mirror completed");
                    }
                    else
                    {
                        Scanner.Scan(depth);
                    }

                    return;
                }
                
                // The startup manifest (startup file) must be in the same
                // directory as the assembly (assembly location). Possible start
                // arguments and batch files are then ignored.
                
                if (File.Exists(Manifest.File))
                {
                    // Only one instance is supported for the target
                    // application(s). This is identified by the process via the
                    // execution name. The path is ignored. This is to ensure
                    // that the file system locations and registry are not
                    // unintentionally overwritten and shared. If a process with
                    // the same name is already running, an attempt is made to
                    // set the focus on it. Startup will end after.

                    var manifest = Manifest.Load();
                    var ambiguous = manifest.Applications
                            .Select(app => Path.GetFileName(app.Destination)?.ToLower())
                            .GroupBy(fileName => fileName)
                            .Any(group => group.Count() > 1);
                    if (ambiguous)
                        Messages.Push(Messages.Type.Error, "multiple application declarations found", true);
                    var mutexIdentifier = Regex.Replace(typeof(Program).Namespace, @"\W+", "_");
                    var mutexList = new List<Mutex>();
                    foreach (var application in manifest.Applications)
                    {
                        var destinationName = Path.GetFileNameWithoutExtension(application.Destination);
                        mutexList.Add(new Mutex(true, $"Global\\{mutexIdentifier}_{destinationName.ToUpper()}", out var created));
                        if (CheckAndFocusExistingInstance(application)
                                || !created)
                            return;
                    }

                    // Handle the Windows shutdown event
                    SystemEvents.SessionEnding += OnSessionEnding;
                    Shutdown.Lock($"{applicationName} must terminate");

                    var datastore = new Datastore(manifest);

                    // Step 01
                    // Create mirror directory
                    datastore.CreateMirrorDirectory();
                    // Step 02
                    // Migrate registry keys that are not yet mirrored
                    datastore.MirrorMissingRegistryKeys();
                    // Step 03
                    // Migrate file system locations that are not yet mirrored
                    datastore.MirrorMissingFileSystemLocations();
                    // Step 04
                    // - Delete existing registry keys
                    datastore.DeleteExistingRegistry();
                    // Step 05
                    // - Delete existing file system locations
                    datastore.DeleteExistingFileSystemLocations();
                    // Step 06
                    // - Insert mirrored/saved registry keys
                    datastore.RestoreRegistry();
                    // Step 07
                    // - Create mirrored/saved file system locations
                    datastore.RestoreFileSystem();
                    // Step 08
                    // - Start target application
                    // - Wait for the end of the target application, incl. kill

                    // The process will intentionally block here. The following events
                    // will end the processes and the program will continue:
                    // - Session Ending
                    // - Windows Shutdown
                    // ShutdownBlockReasonCreate attempts to maintain this process and
                    // execute the subsequent logic to the end.  

                    _runner = new Runner(manifest.Applications, manifest.Environment);
                    _runner.StartAndWaitForExit();

                    // Step 09
                    // - Mirror/backup existing registry to mirror/backup
                    //   directory with timestamp
                    datastore.MirrorRegistry();
                    // Step 10
                    // - Delete existing registry keys
                    datastore.DeleteExistingRegistry();
                    // Step 11
                    // - Delete existing file system locations
                    datastore.DeleteExistingFileSystemLocations();

                    // The reference is intended to prevent the garbage
                    // collector from cleaning up the mutex instance too early
                    // and thus losing the lock.
                    _mutex.ReleaseMutex();

                    return;
                }
                
                // The batch script (cmd file) is searched for in the current
                // working directory and alternatively in the directory of the
                // assembly (exe file).
                
                var scriptDirectory = applicationDirectory;
                var scriptName = applicationName + ".cmd";
                if (File.Exists(Path.Combine(".", Path.GetFileName(scriptName))))
                    scriptDirectory = ".";

                var scriptFile = Path.Combine(scriptDirectory, scriptName);
                if (!File.Exists(scriptFile))
                    throw new Exception($"The required {scriptName} file was not found");

                if (new FileInfo(scriptFile).Length <= 0)
                    return;

                var processStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,

                    WindowStyle = ProcessWindowStyle.Minimized,

                    FileName = scriptFile,
                    WorkingDirectory = scriptDirectory,

                    RedirectStandardError = false,
                    RedirectStandardOutput = false,
                };
                
                if (arguments?.Length > 0)
                    processStartInfo.Arguments = String.Join(" ", arguments
                            .Select(argument => $"\"{argument}\""));
                    
                var process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
            }
            catch (Exception exception)
            {
                Messages.Push(Messages.Type.Error, exception.ToString());
            }
            finally
            {
                Shutdown.Unlock();
            }
        }

        private static void OnSessionEnding(object sender, SessionEndingEventArgs eventArgs)
        {
            if (_runner != null)
                _runner.Terminate();
        }

        private class Subscription : Messages.ISubscriber
        {
            private bool _continue;
            
            private static readonly Messages.Type[] MESSAGES_TYPE_ACCEPTED = new[]
            {
                Messages.Type.Error,
                Messages.Type.Warn,
                Messages.Type.Trace,
                Messages.Type.Text,
                Messages.Type.Message,
                Messages.Type.Exit
            };

            public void Receive(Messages.Message message)
            {
                try
                {
                    if (!MESSAGES_TYPE_ACCEPTED.Contains(message.Type))
                        return;

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
                        
                        Console.WriteLine(banner);
                        if (!File.Exists(logfilePath)
                                || new FileInfo(logfilePath).Length <= 0)
                            File.WriteAllLines(logfilePath, new[] {banner});
                    }
                    _continue = true;

                    if (Messages.Type.Message != message.Type)
                    {
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var content = message.Content;
                        if (Messages.Type.Text != message.Type)
                            content = $"{message.Type.ToString().ToUpper()} {content}";
                        content = $"{timestamp} {content}";
                        content = Regex.Replace(content, @"((?:\r\n)|(?:\n\r)|\r|\n)", "$1\t").Trim();
                        if (!String.IsNullOrEmpty(content))
                        {
                            Console.WriteLine(content);
                            File.AppendAllText(logfilePath, content);
                        }
                    }
                    else
                    {
                        Console.WriteLine(message.Content);
                    }
                    
                    if (Messages.Type.Exit == message.Type
                            || message.Exit)
                        Environment.Exit(0);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}