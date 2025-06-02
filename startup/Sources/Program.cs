// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Startup
// Starts a batch script with the same name minimized.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace VirtualEnvironment.Startup
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] arguments)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var applicationDirectory = Path.GetDirectoryName(applicationPath);
            var applicationName = Path.GetFileNameWithoutExtension(applicationPath);

            var scriptName = Path.GetFileNameWithoutExtension(applicationPath) + ".cmd";
            var scriptFile = Path.Combine(applicationDirectory, scriptName);
            if (!File.Exists(scriptFile))
            {
                MessageBox.Show($"The required {scriptName} file was not found",
                    applicationName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
                return;
            }

            if (new FileInfo(scriptFile).Length <= 0)
                return;

            var processStartInfo = new ProcessStartInfo()
            {
                UseShellExecute = true,
                CreateNoWindow  = true,

                WindowStyle = ProcessWindowStyle.Hidden,

                FileName = scriptFile,
                WorkingDirectory = applicationDirectory,

                RedirectStandardError  = false,
                RedirectStandardOutput = false,
            };
            
            if (arguments?.Length > 0)
                processStartInfo.Arguments = String.Join(" ",
                    arguments.Select(argument => argument.Replace("\"", "\"\"\"")));

            try
            {
                var process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"The program start has failed{System.Environment.NewLine}{exception.Message}",
                    applicationName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}