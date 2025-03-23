// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VirtualEnvironment.Platform
{
    internal static class Program
    {
        [STAThread]
        private static void Main (params string[] arguments)
        {
            if (arguments == null)
                arguments = new string[] {};
            var task = (arguments.ElementAtOrDefault(1) ?? "").ToLower();
            var drive = (arguments.ElementAtOrDefault(0) ?? "").ToUpper();
            if (!new Regex("^[A-Z]:$").IsMatch(drive))
                task = "";
            
            if (Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly()
                    && !new Regex("^(compact|attach|detach|shortcuts)$", RegexOptions.IgnoreCase).IsMatch(task))
                throw new InvalidOperationException("Requires a drive letter (A-Z) and a method: [compact|attach|detach|shortcuts].");
            
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var diskFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".vhdx");
            var workerTask = Worker.Task.Usage;
            switch (task)
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

            // In the case that the main method is called as a DLL. If a window
            // from the calling program already exists, no new one should or may
            // be established as an application, as this will otherwise cause an
            // InvalidOperationException. 
            
            if (!Application.MessageLoop
                    && Application.OpenForms.Count <= 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Worker(workerTask, drive, diskFile));
            }
            else new Worker(workerTask, drive, diskFile).Show();
        }
    }
}