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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.ShiftDown
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
                Console.WriteLine(VERSION);
                Console.WriteLine();

                var isAdministrator = false;
                using (var identity = WindowsIdentity.GetCurrent())
                    isAdministrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                
                var command = (isAdministrator
                        && ApplicationMeta.Name.Length > 0
                        && options.Length > 0
                    ? options[0] : "").Trim().ToLower();
                switch (command)
                {
                    case "install":
                        BatchExec("sc.exe", "create", ApplicationMeta.Name, $"binpath=\"{ApplicationMeta.Location}\"", "start=auto");
                        break;
                    case "uninstall":
                        BatchExec(new BatchExecMeta()
                            {FileName = "net.exe", Arguments = new[] {"stop", ApplicationMeta.Name}, Output = false});
                        BatchExec("sc.exe", "delete", ApplicationMeta.Name);
                        break;
                    case "start":
                    case "pause":
                    case "continue":
                    case "stop":
                        BatchExec("net.exe", command, ApplicationMeta.Name);
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
            BatchExec(new BatchExecMeta() {FileName = fileName, Arguments = arguments, Output = true});
        }

        private static void BatchExec(BatchExecMeta batchExecMeta)
        {
            if (batchExecMeta.Output)
            {
                Console.WriteLine(batchExecMeta.FileName + " " + String.Join(" ", batchExecMeta.Arguments));
                Console.WriteLine();
            }

            using (var process = new Process())
            {
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
    }
}