// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Seanox.Platform
{
    internal static class Program {

        internal const string DISK_TYPE   = "expandable";
        internal const int    DISK_SIZE   = 128000;
        internal const string DISK_STYLE  = "GPT";
        internal const string DISK_FORMAT = "NTFS";

        [STAThread]
        private static void Main (params string[] arguments)
        {
            if (arguments == null
                    || arguments.Length < 2
                    || !new Regex("^[A-Z]:$", RegexOptions.IgnoreCase).IsMatch(arguments[0])
                    || Enum.GetValues(typeof(Worker.Task)).Cast<Worker.Task>()
                            .Where(task => !Worker.Task.Usage.Equals(task))
                            .All(task => string.Equals(task.ToString(), arguments[1], StringComparison.OrdinalIgnoreCase))) {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Worker(Worker.Task.Usage, null, null));
                return;
            }

            var drive = arguments[0].ToUpper(); 
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var diskFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                Path.GetFileNameWithoutExtension(applicationPath) + ".vhdx");
            var workerTask = Worker.Task.Usage;
            switch (arguments[1].ToLower())
            {
                case "create":
                    workerTask = Worker.Task.Create;
                    break;
                case "compact":
                    workerTask = Worker.Task.Compact;
                    break;
                case "attach":
                    workerTask = Worker.Task.Attach;
                    break;
                case "detach":
                    workerTask = Worker.Task.Detach;
                    break;
                case "shortcuts":
                    workerTask = Worker.Task.Shortcuts;
                    break;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Worker(workerTask, drive, diskFile));
        }
    }
}