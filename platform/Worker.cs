// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment
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
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Management;
using System.Collections;
using System.Linq;

namespace Platform {

    public partial class Worker:Form, Notification.INotification
    {
        internal delegate void BackgroundWorkerCall(Task task);

        internal enum Task
        {
            Attach,
            Create,
            Compact,
            Detach,
            Usage
        }

        private struct WorkerTask
        {
            internal Task   Task;
            internal string Drive;
            internal string DiskFile;
        }

        private System.Threading.Timer timer;

        internal Worker(Task task, string drive, string diskFile)
        {
            Notification.Subscribe(this);
            InitializeComponent();
            this.timer = new System.Threading.Timer(Service, new WorkerTask() {Task = task, Drive = drive, DiskFile = diskFile}, 25, -1);
        }

        private struct ProcessesInfo
        {
            internal Process Process;
            internal string  Path;
            internal string  CommandLine;
        }

        private static List<ProcessesInfo> GetProcesses()
        {
            string wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
            using (ManagementObjectCollection collection = searcher.Get())
            {
                IEnumerable query = from process in Process.GetProcesses()
                                    join managementObject in collection.Cast<ManagementObject>()
                                    on process.Id equals (int)(uint)managementObject["ProcessId"]
                                    select new ProcessesInfo()
                                    {
                                        Process = process,
                                        Path = (string)managementObject["ExecutablePath"],
                                        CommandLine = (string)managementObject["CommandLine"]
                                    };
                List<ProcessesInfo> resultList = new List<ProcessesInfo>();
                foreach (ProcessesInfo process in query)
                    resultList.Add(process);
                return resultList;
            }
        }

        private struct BatchResult
        {
            internal string Output;
            internal bool   Failed;
        }

        private static BatchResult BatchExec(string fileName, params string[] arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow  = true,

                WindowStyle = ProcessWindowStyle.Minimized,

                FileName  = fileName,
                Arguments = String.Join(" ", arguments),
                WorkingDirectory = Path.GetDirectoryName(fileName),

                RedirectStandardError  = true,
                RedirectStandardOutput = true,
            };

            string applicationPath = Assembly.GetExecutingAssembly().Location;
            string applicationFile = Path.GetFileName(applicationPath);
            string applicationDirectory = Path.GetDirectoryName(applicationPath);
            string applicationName = Path.GetFileNameWithoutExtension(applicationFile);
            string diskFile = Path.Combine(applicationDirectory, applicationName + ".vhdx");
            processStartInfo.EnvironmentVariables.Add("VT_PLATFORM_NAME", applicationName);
            processStartInfo.EnvironmentVariables.Add("VT_PLATFORM_HOME", applicationDirectory);
            processStartInfo.EnvironmentVariables.Add("VT_PLATFORM_DISK", diskFile);
            processStartInfo.EnvironmentVariables.Add("VT_PLATFORM_APP", applicationFile);
            string rootPath = Path.GetPathRoot(fileName);
            processStartInfo.EnvironmentVariables.Add("VT_HOMEDRIVE", rootPath.Substring(0, 2));

            try
            {
                Process process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
                process.WaitForExit();

                string output = (process.StandardError.ReadToEnd() ?? "").Trim();
                if (output.Length <= 0)
                    output = (process.StandardOutput.ReadToEnd() ?? "").Trim();
                return new BatchResult()
                {
                    Output = output,
                    Failed = process.ExitCode != 0
                };
            }
            catch (Exception exception)
            {
                return new BatchResult()
                {
                    Output = exception.Message,
                    Failed = true
                };
            }
        }

        private void Service(object payload)
        {
            if (!this.IsHandleCreated
                    || this.IsDisposed)
                return;

            WorkerTask workerTask = (WorkerTask)payload;

            this.Invoke((MethodInvoker)delegate
            {
                /// The timer should only trigger the asynchronous processing,
                /// after that it is no longer needed and will be terminated.
                this.timer.Dispose();

                try
                {
                    BatchResult batchResult;

                    switch (workerTask.Task)
                    {
                        case Task.Attach:
                            Notification.Push(Notification.Type.Trace, Messages.WorkerAttachText);
                            Thread.Sleep(3000);

                            Diskpart.AttachDisk(workerTask.Drive, workerTask.DiskFile);
                            
                            batchResult = Worker.BatchExec(workerTask.Drive + @"\Startup.cmd");
                            if (batchResult.Failed)
                                throw new DiskpartException(Messages.WorkerAttachFailed, Messages.WorkerAttachBatchFailed, "@" + batchResult.Output);
                            
                            Notification.Push(Notification.Type.Abort, Messages.WorkerAttach, Messages.WorkerSuccessfullyCompleted);
                            break;

                        case Task.Create:
                            Diskpart.CreateDisk(workerTask.Drive, workerTask.DiskFile);
                            Notification.Push(Notification.Type.Abort, Messages.DiskpartCreate, Messages.WorkerSuccessfullyCompleted);
                            break;
                        
                        case Task.Compact:
                            Diskpart.CompactDisk(workerTask.Drive, workerTask.DiskFile);
                            Notification.Push(Notification.Type.Abort, Messages.DiskpartCompact, Messages.WorkerSuccessfullyCompleted);
                            break;
                        
                        case Task.Detach:
                            Notification.Push(Notification.Type.Trace, Messages.WorkerDetachText);
                            Thread.Sleep(3000);

                            batchResult = Worker.BatchExec(workerTask.Drive + @"\Startup.cmd", "exit");
                            if (batchResult.Failed)
                                throw new DiskpartException(Messages.WorkerDetachFailed, Messages.WorkerDetachBatchFailed, "@" + batchResult.Output);

                            Worker.GetProcesses()
                                .FindAll(processInfo => processInfo.Path != null)
                                .FindAll(processInfo => processInfo.Path.StartsWith(workerTask.Drive))
                                .ForEach(processInfo => processInfo.Process.CloseMainWindow());
                            Thread.Sleep(3000);
                            Worker.GetProcesses()
                                .FindAll(processInfo => processInfo.Path != null)
                                .FindAll(processInfo => processInfo.Path.StartsWith(workerTask.Drive))
                                .ForEach(processInfo => processInfo.Process.Kill());

                            Diskpart.DetachDisk(workerTask.Drive, workerTask.DiskFile);

                            Notification.Push(Notification.Type.Abort, Messages.WorkerDetach, Messages.WorkerSuccessfullyCompleted);
                            break;
                        
                        default:
                            Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                            string applicationVersion = String.Format("{0}.{1}.{2} {3}",
                                assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build, assemblyVersion.Revision);
                            string applicationPath = Assembly.GetExecutingAssembly().Location;
                            string applicationFile = Path.GetFileName(applicationPath);
                            Notification.Push(Notification.Type.Abort,
                                    String.Format(Messages.WorkerUsage, applicationVersion, applicationFile));
                            break;
                    }
                }
                catch (Exception exception)
                {
                    if (exception is WorkerException)
                        Notification.Push(Notification.Type.Error, ((WorkerException)exception).Messages);
                    else if (exception is DiskpartException)
                        Notification.Push(Notification.Type.Error, ((DiskpartException)exception).Messages);
                    else
                        Notification.Push(Notification.Type.Error, Messages.WorkerUnexpectedErrorOccurred, exception.Message);
                }
            });
        }

        void Notification.INotification.Receive(Notification.Message message)
        {
            this.Output.Text = message.Text;
            this.Refresh();

            if (message.Type != Notification.Type.Error
                    && message.Type != Notification.Type.Abort)
                return;

            if (message.Type == Notification.Type.Error)
            {
                this.BackColor = Color.FromArgb(250, 225, 150);
                this.Progress.BackColor = Color.FromArgb(225, 175, 100);
            }

            Size originSize = this.Progress.Size;
            this.Progress.Visible = true;
            for (int width = 1; width < originSize.Width; width++)
            {
                this.Progress.Size = new Size(width, originSize.Height);
                this.Refresh();
                Thread.Sleep(10);
            }

            Thread.Sleep(500);
            this.Close();
        }
    }

    internal class WorkerException : Exception
    {
        internal string[] Messages { get; }

        internal WorkerException(params string[] messages)
        {
            this.Messages = messages;
        }
    }
}