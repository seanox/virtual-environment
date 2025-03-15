﻿// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
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
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VirtualEnvironment.Launcher
{
    internal static class Program
    {
        private const int ERROR_CANCELLED = 0x4C7;

        private static Settings Settings;
        
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Handle the Windows shutdown event
            SystemEvents.SessionEnding += OnSessionEnding;            

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
                    Program.Settings = settings;
                    using (control = new Control(settings, control == null))
                        Application.Run(control);
                    GC.Collect();
                }
                catch (Exception exception)
                {
                    // System.IO.IOException can occur due to asynchronous
                    // access and are ignored.
                    if (exception is System.IO.IOException
                            && control != null)
                        continue;
                    
                    var message = "An unexpected error has occurred."
                            + $"{Environment.NewLine}{Environment.NewLine}{exception}";
                    if (exception is Settings.SettingsException)
                        message = exception.Message;
                    
                    MessageBox.Show(message, "Virtual Environment Launcher",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    // If an error occurs during the initial start, the
                    // program is terminated.
                    if (control == null)
                        Environment.Exit(0);
                }
                Thread.Sleep(25);
            }
        }
        
        private static void OnSessionEnding(object sender, SessionEndingEventArgs eventArgs)
        {
            SystemEvents.SessionEnding -= OnSessionEnding;
            
            if (String.IsNullOrWhiteSpace(Settings?.Events?.Session?.Ending?.Destination))
                return;
            
            try
            {
                Settings.Events.Session.Ending.Start(false);
            }
            catch (Exception exception)
            {
                // Exception when canceling by the user (UAC) is ignored
                if (exception is Win32Exception
                        && ((Win32Exception)exception).NativeErrorCode == ERROR_CANCELLED)
                    return;

                MessageBox.Show(($"Error opening action: {Settings.Events.Session.Ending.Destination}"
                        + $"{Environment.NewLine}{exception.Message}"
                        + $"{Environment.NewLine}{exception.InnerException?.Message ?? ""}").Trim(),
                    "Virtual Environment Launcher", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}