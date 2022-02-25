// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Launcher
// Program starter for the virtual environment.
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
using System.Threading;
using System.Windows.Forms;
    
namespace Seanox.Platform.Launcher
{
    internal static class Program
    {
        [STAThread]
        internal static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Settings are monitored by the main program. When changes are
            // detected, the control is closed and set up again with the
            // changed settings. If an error occurs when loading the settings,
            // a message is shown and existing settings continue to be used.
            
            // The logic for the reload is a bit more complicated, because 
            // Application.Run blocks and the detection of changed settings
            // runs in the control as background timer. But since Windows
            // expects a STA (Single Thread Apartment) for some functions like
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
                    // System.IO.IOException can occur due to asynchronous
                    // access and are ignored.
                    if (!(exception is System.IO.IOException)
                            && control != null)
                        MessageBox.Show("An unexpected error has occurred."
                                    + $"{Environment.NewLine}{Environment.NewLine}{exception}",
                                "Virtual Environment Launcher", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    // If an error occurs during the initial start, the
                    // program is terminated.
                    if (control == null)
                        Environment.Exit(0);
                }
                Thread.Sleep(25);
            }
        }
    }
}