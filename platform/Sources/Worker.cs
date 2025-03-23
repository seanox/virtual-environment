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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace VirtualEnvironment.Platform
{
    internal partial class Worker : Form, Notification.INotification
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
            Notification.Subscribe(this);

            InitializeComponent();
            
            Output.Font = new Font(SystemFonts.DialogFont.FontFamily, 9.25f);
            Label.Font = new Font(SystemFonts.DialogFont.FontFamily, 8.25f);
            
            System.Threading.Tasks.Task.Run(() => WorkerAction(task, drive, diskFile));
        }

        private void WorkerAction(Task task, string drive, string diskFile)
        {
            try
            {
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                var applicationFile = Path.GetFileName(applicationPath);

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
                        Notification.Push(Notification.Type.Error, Messages.WorkerVersion,
                                String.Format(Messages.WorkerUsage, applicationFile));
                        break;
                }
            }
            catch (Exception exception)
            {
                if (exception is WorkerException workerException)
                    Notification.Push(Notification.Type.Error, workerException.Messages);
                else if (exception is DiskpartAbortException diskpartAbortException)
                    Notification.Push(Notification.Type.Error, diskpartAbortException.Messages);
                else if (exception is DiskpartException diskpartException)
                    Notification.Push(Notification.Type.Error, diskpartException.Messages);
                else Notification.Push(Notification.Type.Error, Messages.WorkerUnexpectedErrorOccurred, exception);

                if (exception is DiskpartAbortException)
                    return;

                if (new []{Task.Attach, Task.Create, Task.Compact}.Contains(task))
                    Diskpart.AbortDisk(drive, diskFile);
            }
        }

        void Notification.INotification.Receive(Notification.Message message)
        {
            // Invoke is required because in Windows Forms all changes to UI
            // elements such as text, colors or sizes must be made in the main
            // UI thread. If the method is called from a background thread,
            // Invoke ensures that the execution is moved to the main thread to
            // avoid thread safety issues.
            Invoke((MethodInvoker)(() =>
            {
                Output.Text = message.Text;
                Refresh();

                if (message.Type != Notification.Type.Error
                        && message.Type != Notification.Type.Abort)
                    return;

                if (message.Type == Notification.Type.Error
                        || message.Type == Notification.Type.Warning)
                {
                    BackColor = Color.FromArgb(250, 225, 150);
                    Progress.BackColor = Color.FromArgb(200, 150, 75);
                    Label.ForeColor = Progress.BackColor;
                }
                
                if (message.Type == Notification.Type.Warning)
                    return;

                var originSize = Progress.Size;
                Progress.Visible = true;
                for (var width = 1; width < originSize.Width; width += 1)
                {
                    Progress.Size = new Size(Math.Min(width, originSize.Width), originSize.Height);
                    Refresh();
                    Thread.Sleep(message.Type == Notification.Type.Error ? 75 : 25);
                }

                Thread.Sleep(500);
                Close();
            }));
        }
    }

    internal abstract class WorkerException : Exception
    {
        internal string[] Messages { get; }

        internal WorkerException(params string[] messages)
        {
            Messages = messages;
        }
    }
}