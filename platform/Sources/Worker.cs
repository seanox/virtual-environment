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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace VirtualEnvironment.Platform
{
    internal partial class Worker : Form, Messages.ISubscriber
    {
        internal enum Task
        {
            Attach,
            Create,
            Compact,
            Detach,
            Shortcuts,
            Usage
        }

        internal Worker(Task task, string drive, string diskFile)
        {
            Messages.Subscribe(this);

            InitializeComponent();
            
            Output.Font = new Font(SystemFonts.DialogFont.FontFamily, 9.25f);
            Label.Font = new Font(SystemFonts.DialogFont.FontFamily, 8.25f);
            
            System.Threading.Tasks.Task.Run(() => WorkerAction(task, drive, diskFile));
        }

        private void WorkerAction(Task task, string drive, string diskFile)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var applicationPath = assembly.Location;
                var applicationFile = Path.GetFileName(applicationPath);
                var applicationVersion = assembly.GetName().Version;
                var applicationBuild = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(attribute => attribute.Key == "Build")?.Value;

                switch (task)
                {
                    case Task.Attach:
                        Service.Attach(drive, diskFile);
                        break;

                    case Task.Create:
                        Service.Create(drive, diskFile);
                        break;
                    
                    case Task.Compact:
                        Service.Compact(drive, diskFile);
                        break;
                    
                    case Task.Detach:
                        Service.Detach(drive, diskFile);
                        break;
                 
                    case Task.Shortcuts:
                        Service.Shortcuts(drive, diskFile);
                        break;
                    
                    default:
                        Messages.Push(Messages.Type.Error,
                            String.Format(Resources.WorkerVersion, applicationVersion, applicationBuild),
                            String.Format(Resources.WorkerUsage, applicationFile));
                        break;
                }
            }
            catch (Exception exception)
            {
                if (exception is DiskpartAbortException)
                    return;
                Messages.Push(Messages.Type.Error, exception);
                if (new[] {Task.Attach, Task.Create, Task.Compact}.Contains(task))
                    Diskpart.AbortDisk(drive, diskFile);
            }
        }
        
        private static readonly HashSet<Messages.Type> MESSAGE_TYPE_LIST = new HashSet<Messages.Type>()
        {
            Messages.Type.Error,
            Messages.Type.Trace,
            Messages.Type.Exit
        };

        void Messages.ISubscriber.Receive(Messages.Message message)
        {
            if (!MESSAGE_TYPE_LIST.Contains(message.Type))
                return;

            // Invoke is required because in Windows Forms all changes to UI
            // elements such as text, colors or sizes must be made in the main
            // UI thread. If the method is called from a background thread,
            // Invoke ensures that the execution is moved to the main thread to
            // avoid thread safety issues.
            Invoke((MethodInvoker)(() =>
            {
                var context = message.Context;
                var text = Convert.ToString(message.Data ?? String.Empty);
                
                if (Messages.Type.Trace == message.Type)
                {
                    if (!String.IsNullOrWhiteSpace(context)
                            && !Regex.IsMatch(text, @"[\r\n]"))
                        Output.Text = ($"{context}{System.Environment.NewLine}{text}").Trim();
                    Refresh();
                    return;
                }

                if (message.Type == Messages.Type.Error)
                {
                    text = text.Contains(Environment.NewLine) 
                        ? text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() 
                        : text;
                    
                    if (message.Data is ServiceException serviceException)
                        context = serviceException.Context;
                    else if (message.Data is DiskpartException diskpartException)
                        context = diskpartException.Context;
                    else
                        context = Resources.CommonUnexpectedErrorOccurred;

                    Output.Text = ($"{context}{System.Environment.NewLine}{text}").Trim();
                    Refresh();

                    BackColor = Color.FromArgb(250, 225, 150);
                    Progress.BackColor = Color.FromArgb(200, 150, 75);
                    Label.ForeColor = Progress.BackColor;
                }
                
                var originSize = Progress.Size;
                Progress.Visible = true;
                for (var width = 1; width < originSize.Width; width += 1)
                {
                    Progress.Size = new Size(Math.Min(width, originSize.Width), originSize.Height);
                    Refresh();
                    Thread.Sleep(message.Type == Messages.Type.Error ? 75 : 25);
                }

                Thread.Sleep(500);
                Close();
            }));
        }
    }
}