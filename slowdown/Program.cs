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
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace slowdown
{
    internal class Program
    {
        private struct ProcessesInfo
        {
            internal Process Process;
            internal float   Load;
            internal string  Path;
        }

        private enum State
        {
            Run,
            Interrupt,
            Stop
        }

        private static State state = Program.State.Run;

        private static Dictionary<int, ProcessPriorityClass> processPriorities; 

        public static void Main(params string[] options)
        {
            Console.WriteLine("Seanox SwitchDown [Version 0.0.0 00000000]");
            Console.WriteLine("Copyright (C) 0000 Seanox Software Solutions");
            Console.WriteLine("Running");
            Console.SetCursorPosition(0, Console.CursorTop - 1);

            Program.processPriorities = new Dictionary<int, ProcessPriorityClass>();
            
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs consoleEvent) =>
            {
                if (Program.State.Run.Equals(Program.state))
                    Program.state = Program.State.Interrupt;
                while (!Program.State.Stop.Equals(Program.state))
                    Thread.Sleep(25);
            };
            
            while (Program.State.Run.Equals(Program.state))
            {
                var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuUsage.NextValue();
                for (int counter = 0; Program.State.Run.Equals(Program.state) && counter < 50; counter++)
                    Thread.Sleep(25);
                if (!Program.State.Run.Equals(Program.state))
                    break;
                if (cpuUsage.NextValue() < 25)
                    continue;
                
                var performanceCounterDictionary = new Dictionary<string, PerformanceCounter>();
                Process.GetProcesses().ToList().ForEach(process =>
                {
                    if (!Program.State.Run.Equals(Program.state))
                        return;
                    Thread.Sleep(25);
                    try
                    {
                        using (process)
                        {
                            if (ProcessPriorityClass.Idle.Equals(process.PriorityClass)
                                    || performanceCounterDictionary.ContainsKey(process.ProcessName))
                                return;
                            var performanceCounter = new PerformanceCounter("Process", "% Processor Time",
                                process.ProcessName, true);
                            performanceCounter.NextValue();
                            performanceCounterDictionary.Add(process.ProcessName, performanceCounter);
                        }
                    }
                    catch (Exception)
                    {
                    }
                });
                
                for (int counter = 0; Program.State.Run.Equals(Program.state) && counter < 10; counter++)
                    Thread.Sleep(25);
                if (!Program.State.Run.Equals(Program.state))
                    break;
                
                foreach (var keyValuePair in performanceCounterDictionary)
                {
                    if (!Program.State.Run.Equals(Program.state))
                        break;
                    Thread.Sleep(25);

                    try
                    {
                        if ((keyValuePair.Value.NextValue() / Environment.ProcessorCount) < 25)
                            continue;
                        foreach (var process in Process.GetProcessesByName(keyValuePair.Key))
                        {
                            if (!Program.processPriorities.ContainsKey(process.Id))
                                Program.processPriorities.Add(process.Id, process .PriorityClass);
                            process .PriorityClass = ProcessPriorityClass.Idle;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            
            foreach (var KeyValuePair in Program.processPriorities)
            {
                try
                {
                    var process = Process.GetProcessById(KeyValuePair.Key);
                    process.PriorityClass = KeyValuePair.Value;
                }
                catch (Exception)
                {
                }
            }
            
            Program.state = Program.State.Stop;

            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, currentLineCursor);
            
            Console.Write("Terminated");
        }
    }
}
