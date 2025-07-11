﻿// LICENSE TERMS - Seanox Software Solutions is an open source project,
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

            Func<string, string> truncateText = text =>
            {
                if (!isTextWidthSuitable(text))
                    return text; 
                while (text.Length > 0
                       && isTextWidthSuitable(text + "..."))
                    text = text.Substring(0, text.Length - 1);
                return text + "...";
            };            
            
            label.Text = string.Join(Environment.NewLine, 
                label.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
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
                if (exception is DiskpartException diskpartException)
                    Messages.Push(Messages.Type.Error, diskpartException.Context, diskpartException.Message, diskpartException.Details);
                else if (exception is ServiceException serviceException)
                    Messages.Push(Messages.Type.Error, serviceException.Context, serviceException.Message, serviceException.Details);
                else Messages.Push(Messages.Type.Error, exception);
                if (new[] {Task.Attach, Task.Create, Task.Compact}.Contains(task))
                    Diskpart.AbortDisk(drive, diskFile);
            }
        }

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

        private static readonly HashSet<Messages.Type> MESSAGE_TYPE_LIST = new HashSet<Messages.Type>()
        {
            Messages.Type.Error,
            Messages.Type.Trace,
            Messages.Type.Exit
        };

        private string _context;

        void Messages.ISubscriber.Receive(Messages.Message message)
        {
            if (!MESSAGE_TYPE_LIST.Contains(message.Type))
                return;
            
            // (Begin)Invoke is required because, in Windows Forms, all changes
            // to UI elements such as text, colors, or sizes must be executed in
            // the main UI thread. If the method is called from a background
            // thread, Invoke ensures that execution is transferred to the UI
            // thread to avoid thread safety issues.
            
            // BeginInvoke starts the update asynchronously, allowing the UI to
            // react faster because it does not have to wait for Invoke to
            // complete and does not block the application. This improves
            // performance and keeps the application responsive.
            
            BeginInvoke((MethodInvoker)(() =>
            {
                var context = message.Context?.Trim() ?? _context ?? string.Empty;

                var content = Convert.ToString(message.Data ?? String.Empty);
                if (message.Data is ServiceException serviceException)
                {
                    content = serviceException.Message;
                    context = serviceException.Context;
                }
                else if (message.Data is DiskpartException diskpartException)
                {
                    content = diskpartException.Message;
                    context = diskpartException.Context;
                }
                else if (message.Data is Exception exception)
                {
                    content = String.Format(
                        Resources.ApplicationUnexpectedErrorOccurred,
                        exception.GetType().Name);
                    content = $"{content}{Environment.NewLine}{exception.Message}";
                }
                    
                _context = context;                
                
                content = content.Trim()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .FirstOrDefault();
                content = ($"{context}{Environment.NewLine}{content}").Trim();

                if (Messages.Type.Trace == message.Type
                        || Messages.Type.Exit == message.Type)
                {
                    Output.Text = content;
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
                
                Output.Text = content;
                Refresh();
                
                ShowWaitingLoop(message.Type);
                Close();
            }));
        }
    }
}