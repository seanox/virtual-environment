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
// http://www.apache.org/licenses/LICENSE-2.0
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
        private static Settings _settings;
        
        [STAThread]
        internal static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Settings are monitored by the main program. When changes are
            // detected, the form is closed and set up again with the changed
            // settings. If an error occurs when loading the settings, a
            // message is shown and existing settings continue to be used.
                
            while (true)
            {
                if (Settings.IsUpdateAvailable())
                {
                    try
                    {
                        var settings = Settings.Load();
                        var visible = Application.OpenForms.Count <= 0
                                || Application.OpenForms[0].Visible;
                        if (Application.OpenForms.Count > 0)
                            Application.Exit();
                        new Thread(delegate() {Application.Run(new Control(settings, visible));}).Start();
                    }
                    catch (Exception exception)
                    {
                        // System.IO.IOException can occur due to asynchronous
                        // access and are ignored. 
                        if (exception.InnerException == null
                                || !(exception is System.IO.IOException))
                            MessageBox.Show(exception.Message, "Virtual Environment Launcher",
                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        if (Application.OpenForms.Count <= 0)
                            break;
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}