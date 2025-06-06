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

            Paint += OnPaint; 
            
            System.Threading.Tasks.Task.Run(() => WorkerAction(task, drive, diskFile));
        }
        
        private void OnPaint(object sender, PaintEventArgs paintEvent)
        {
            TruncateLabelTextToFit(paintEvent.Graphics, Output);
        }

        private static void TruncateLabelTextToFit(Graphics graphics, Label label)
        {
            if (String.IsNullOrEmpty(label.Text))
                return;
            
            Func<string, bool> isTextWidthSuitable = text =>
                graphics.MeasureString(text, label.Font).Width > label.Width;

            Func<string, string> truncateText = line =>
            {
                var textTruncated = line;
                while (textTruncated.Length > 0
                        && isTextWidthSuitable(textTruncated + "..."))
                    textTruncated = textTruncated.Substring(0, textTruncated.Length - 1);
                return textTruncated + "...";
            };            
            
            label.Text = string.Join(System.Environment.NewLine, 
                label.Text.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None)
                    .Select(truncateText));
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
                            String.Format(Resources.ApplicationVersion, applicationVersion, applicationBuild),
                            String.Format(Resources.ApplicationUsage, applicationFile));
                        break;
                }
            }
            catch (Exception exception)
            {
                if (exception is DiskpartAbortException diskpartAbortException)
                    Messages.Push(Messages.Type.Error, diskpartAbortException.Context, diskpartAbortException.Message);
                else Messages.Push(Messages.Type.Error, exception);
                if (exception is DiskpartException diskpartException)
                    Messages.Push(Messages.Type.Verbose, diskpartException.Context, diskpartException.Details);
                if (exception is ServiceException serviceException)
                    Messages.Push(Messages.Type.Verbose, serviceException.Context, serviceException.Details);
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

        private void ShowWaitingLoop(Messages.Type type)
        {
            var originSize = Progress.Size;
            Progress.Visible = true;
            for (var width = 1; width < originSize.Width; width += 1)
            {
                Progress.Size = new Size(Math.Min(width, originSize.Width), originSize.Height);
                Refresh();
                Thread.Sleep(Messages.Type.Error == type ? 75 : 25);
            }
            Thread.Sleep(500);
            Progress.Visible = false;
        }

        void Messages.ISubscriber.Receive(Messages.Message message)
        {
            if (!MESSAGE_TYPE_LIST.Contains(message.Type))
                return;
            
            // (Begin)Invoke is required because, in Windows Forms, all changes
            // to UI elements such as text, colors, or sizes must be executed in
            // the main UI thread. If the method is called from a background
            // thread, Invoke ensures that execution is transferred to the UI
            // thread to avoid thread safety issues.
            //
            // BeginInvoke starts the update asynchronously, allowing the UI to
            // react faster because it does not have to wait for Invoke to
            // complete and does not block the application. This improves
            // performance and keeps the application responsive.
            
            BeginInvoke((MethodInvoker)(() =>
            {
                var context = message.Context?.Trim() ?? string.Empty;
                var content = Convert.ToString(message.Data ?? String.Empty).Trim()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                    .Select(line => line.Trim())
                    .FirstOrDefault();

                if (Messages.Type.Trace == message.Type
                        || Messages.Type.Exit == message.Type)
                {
                    if (!String.IsNullOrWhiteSpace(context))
                        Output.Text = ($"{context}{System.Environment.NewLine}{content}").Trim();
                    Refresh();
                    
                    if (Messages.Type.Exit != message.Type)
                        return;
                    
                    ShowWaitingLoop(message.Type);
                    Close();
                    
                    return;
                }

                Output.Text = String.Empty;
                Refresh();

                BackColor = Color.FromArgb(250, 225, 150);
                Progress.BackColor = Color.FromArgb(200, 150, 75);
                Label.ForeColor = Progress.BackColor;
                
                Refresh();
                
                if (message.Data is Exception exception)
                {
                    content = exception.Message;
                    context = String.IsNullOrWhiteSpace(context)
                        ? Resources.ApplicationUnexpectedErrorOccurred
                        : context;
                    content = $"{exception.GetType().Name}: {content}";
                }
                else if (String.IsNullOrWhiteSpace(context))
                    context = Resources.ApplicationUnexpectedErrorOccurred;
                
                Output.Text = ($"{context}{System.Environment.NewLine}{content}").Trim();
                Refresh();
                
                ShowWaitingLoop(message.Type);
                Close();
            }));
        }
    }
}