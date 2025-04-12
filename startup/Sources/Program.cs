// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
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
using System.Text.RegularExpressions;
using System.Threading;

namespace VirtualEnvironment.Startup
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private const int SW_RESTORE = 9;

        private static Mutex Mutex;
        
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
                        Messages.Push(Messages.Type.Exit,
                            $"usage: {Path.GetFileName(applicationPath)} sync|scan(:depth)");
                    var command = match.Groups[match.Groups[1].Success ? 1 : 2].Value;
                    var depth = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : SCAN_DEPTH_DEFAULT;
                    if (String.Equals(command, "sync", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!File.Exists(Manifest.File))
                            Messages.Push(Messages.Type.Exit, $"Missing manifest file: {Manifest.File}");
                        Messages.Push(Messages.Type.Trace, "Read manifest file");
                        var manifest = Manifest.Load();
                        var datastore = new Datastore(manifest);
                        Messages.Push(Messages.Type.Trace, "Create mirror directory");
                        datastore.CreateMirrorDirectory();
                        Messages.Push(Messages.Type.Trace, "Mirror registry");
                        datastore.MirrorRegistry();
                        Messages.Push(Messages.Type.Trace, "Mirror file system");
                        datastore.MirrorFileSystem();
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
                    // Step 09
                    // - Mirror/backup existing registry to mirror/backup
                    //   directory with timestamp
                    datastore.MirrorRegistry();
                    // Step 10
                    // - Delete existing registry keys
//                  datastore.DeleteExistingRegistry();
                    // Step 11
                    // - Delete existing file system locations
//                  datastore.DeleteExistingSettings();

                    // The reference is intended to prevent the garbage
                    // collector from cleaning up the mutex instance too early
                    // and thus losing the lock.
                    Mutex.ReleaseMutex();
                    
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
                    CreateNoWindow  = true,

                    WindowStyle = ProcessWindowStyle.Minimized,

                    FileName = scriptFile,
                    WorkingDirectory = scriptDirectory,

                    RedirectStandardError  = false,
                    RedirectStandardOutput = false,
                };
                
                if (arguments?.Length > 0)
                    processStartInfo.Arguments = String.Join(" ", arguments
                            .Select(argument => $"\"{argument}\""));
                    
                var process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();

                // TODO: Session Ending
                // TODO: Windows Shutdown
            }
            catch (Exception exception)
            {
                Messages.Push(Messages.Type.Error, exception.ToString());
            }
        }

        private class Subscription : Messages.ISubscriber
        {
            private static readonly Messages.Type[] MESSAGES_TYPE_ACCEPTED = new[]
            {
                Messages.Type.Error,
                Messages.Type.Warn,
                Messages.Type.Trace,
                Messages.Type.Message,
                Messages.Type.Exit
            };

            public void Receive(Messages.Message message)
            {
                if (!MESSAGES_TYPE_ACCEPTED.Contains(message.Type))
                    return;
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                var logfilePath = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".log");

                var content = $"{timestamp} {message.Type.ToString().ToUpper()} {message.Content}";
                if (Messages.Type.Message != message.Type)
                    content = Regex.Replace(content, @"((?:\r\n)|(?:\n\r)|\r|\n)", "$1\t");
                content = content.Trim();
                if (!String.IsNullOrEmpty(content))
                {
                    Console.WriteLine(content);
                    File.AppendAllText(logfilePath, content);
                }
                
                if (Messages.Type.Exit == message.Type
                        || message.Exit)
                    Environment.Exit(0);
            }
        }
    }
}