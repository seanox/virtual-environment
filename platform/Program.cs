// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Platform
{
    internal static class Program {

        internal const string DISK_TYPE   = "expandable";
        internal const int    DISK_SIZE   = 128000;
        internal const string DISK_STYLE  = "GPT";
        internal const string DISK_FORMAT = "NTFS";

        [STAThread]
        static void Main (string[] arguments)
        {
            if (arguments == null
                    || arguments.Length < 2
                    || !new Regex("^[A-Z]:$", RegexOptions.IgnoreCase).IsMatch(arguments[0])
                    || !new Regex("^(create|compact|attach|detach)$", RegexOptions.IgnoreCase).IsMatch(arguments[1])) {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Worker(Worker.Task.Usage, null, null));
                return;
            }

            string drive = arguments[0].ToUpper(); 
            string applicationPath = Assembly.GetExecutingAssembly().Location;
            string diskFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                Path.GetFileNameWithoutExtension(applicationPath) + ".vhdx");
            Worker.Task workertask = Worker.Task.Usage;
            switch (arguments[1].ToLower())
            {
                case "create":
                    workertask = Worker.Task.Create;
                    break;
                case "compact":
                    workertask = Worker.Task.Compact;
                    break;
                case "attach":
                    workertask = Worker.Task.Attach;
                    break;
                case "detach":
                    workertask = Worker.Task.Detach;
                    break;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Worker(workertask, drive, diskFile));
        }
    }
}