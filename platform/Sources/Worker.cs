// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
// Copyright (C) 2022 Seanox Software Solutions
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
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Management;
using System.Linq;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    public partial class Worker : Form, Notification.INotification
    {
        private const int BATCH_PROCESS_IDLE_TIMEOUT_SECONDS = 30;

        internal delegate void BackgroundWorkerCall(Task task);

        internal enum Task
        {
            Attach,
            Create,
            Compact,
            Detach,
            Shortcuts,
            Usage
        }

        private struct WorkerTask
        {
            internal Task   Task;
            internal string Drive;
            internal string DiskFile;
        }

        private readonly System.Threading.Timer _timer;

        internal Worker(Task task, string drive, string diskFile)
        {
            Notification.Subscribe(this);

            InitializeComponent();
            
            Output.Font = new Font(SystemFonts.DialogFont.FontFamily, 9.25f);
            Label.Font = new Font(SystemFonts.DialogFont.FontFamily, 8.25f);

            _timer = new System.Threading.Timer(Service, new WorkerTask() {Task = task, Drive = drive, DiskFile = diskFile}, 25, -1);
        }

        private struct ProcessesInfo
        {
            internal Process Process;
            internal string  Path;
        }

        private static List<ProcessesInfo> GetProcesses()
        {
            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                using (var collection = searcher.Get())
                {
                    var query = from process in Process.GetProcesses()
                            join managementObject in collection.Cast<ManagementObject>()
                            on process.Id equals (int)(uint)managementObject["ProcessId"]
                            select new ProcessesInfo()
                            {
                                Process = process,
                                Path = (string)managementObject["ExecutablePath"],
                            };
                    var resultList = new List<ProcessesInfo>();
                    foreach (var process in query)
                        resultList.Add(process);
                    return resultList;
                }
        }

        private struct BatchResult
        {
            internal string Message;
            internal string Output;
            internal bool   Failed;
        }

        private static BatchResult BatchExec(Task task, string fileName, params string[] arguments)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                FileName  = "cmd.exe",
                Arguments = $"/C {fileName} {String.Join(" ", arguments)}",
                WorkingDirectory = Path.GetDirectoryName(fileName),
                RedirectStandardError  = true,
                RedirectStandardOutput = true
            };
                        
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var applicationFile = Path.GetFileName(applicationPath);
            var applicationDirectory = Path.GetDirectoryName(applicationPath);
            var applicationName = Path.GetFileNameWithoutExtension(applicationFile);
            var diskFile = Path.Combine(applicationDirectory, applicationName + ".vhdx");

            // The use of environment variables only makes sense during detach,
            // in all other cases it becomes a problem when the environment is
            // launched from an already existing virtual environment, as is the
            // case during platform development.

            var SetEnvironmentVariableIfNecessary = new Action<string, string>(delegate(string name, string value)
            {
                if (processStartInfo.EnvironmentVariables.ContainsKey(name)
                        && Task.Detach.Equals(task))
                    return;
                processStartInfo.EnvironmentVariables[name] = value;
            });

            foreach(KeyValuePair<string, string> value in Settings.Values)
                SetEnvironmentVariableIfNecessary(value.Key, value.Value);
            
            SetEnvironmentVariableIfNecessary("VT_PLATFORM_NAME", applicationName);
            SetEnvironmentVariableIfNecessary("VT_PLATFORM_HOME", applicationDirectory);
            SetEnvironmentVariableIfNecessary("VT_PLATFORM_DISK", diskFile);
            SetEnvironmentVariableIfNecessary("VT_PLATFORM_APP", applicationPath);
            var rootPath = Path.GetPathRoot(fileName);
            SetEnvironmentVariableIfNecessary("VT_HOMEDRIVE", rootPath.Substring(0, 2));

            var batchResult = new BatchResult() {Output = ""};
            
            try
            {
                var process = Process.Start(processStartInfo);
                process.OutputDataReceived += (object sender, DataReceivedEventArgs eventArgs) =>
                    batchResult.Output += $"{Environment.NewLine}{eventArgs.Data}";
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs eventArgs) =>
                    batchResult.Output += $"{Environment.NewLine}{eventArgs.Data}";
                process.BeginErrorReadLine();
                
                var idleTimoutSeconds = DateTimeOffset.Now.AddSeconds(BATCH_PROCESS_IDLE_TIMEOUT_SECONDS);
                var idleTotalProcessorTime = process.TotalProcessorTime;
                while (Process.GetProcesses().Any(entry => entry.Id == process.Id))
                {
                    Thread.Sleep(25);
                    var totalProcessorTime = process.TotalProcessorTime;
                    if (totalProcessorTime <= idleTotalProcessorTime
                            && DateTimeOffset.Now > idleTimoutSeconds)
                    {
                        try
                        {
                            process.Kill();
                            process.Dispose();
                        }
                        catch
                        {
                        }
                        throw new TimeoutException(Messages.WorkerBatchFreezeDetection);
                    }
                    if (totalProcessorTime == idleTotalProcessorTime)
                        continue;
                    idleTotalProcessorTime = totalProcessorTime;
                    idleTimoutSeconds = DateTimeOffset.Now.AddSeconds(BATCH_PROCESS_IDLE_TIMEOUT_SECONDS);
                }

                batchResult.Failed = process.ExitCode != 0;
                return batchResult;
            }
            catch (Exception exception)
            {
                batchResult.Message = exception.Message;
                batchResult.Failed = true;
                return batchResult;
            }
        }

        private static void KillProcess(ProcessesInfo processesInfo)
        {
            // The process is completed in three stages. First friendly, then
            // gentle, then hard. Here, gentle and hard are implemented.
            // Friendly had no effect before. Gentle tries to end the process
            // with the process structure. Occurring errors are ignored.
            
            try
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    CreateNoWindow  = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "taskkill.exe ",
                    Arguments = "/t /pid " + processesInfo.Process.Id
                };
                process.Start();
                process.WaitForExit();
            }
            catch
            {
            }
            finally
            {
                // Killing the processes can block (e.g. system protection by
                // the virus scanner). Therefore, we try up to 3 times with a
                // small pause. After three attempts, the process is ignored
                // and the drive is detached.

                for (var index = 0; index < 3; index++)
                {
                    Thread.Sleep(3000);
                    if (Process.GetProcesses().All(process => process.Id != processesInfo.Process.Id))
                        break;
                    try
                    {
                        processesInfo.Process.Kill();
                        break;
                    }
                    catch (Exception exception)
                    {
                        Notification.Push(Notification.Type.Warning,
                                String.Format(Messages.WorkerDetachBlocked,
                                        processesInfo.Process.ProcessName, exception.Message));
                    }
                }
            }
        }

        private static void CreateShortcut(string drive, string diskFile, Task task)
        {
            var applicationPath = Path.GetDirectoryName(diskFile);
            var applicationName = Path.GetFileNameWithoutExtension(diskFile);
            var wshShell = new IWshRuntimeLibrary.WshShell();
            var shortcutFile = Path.Combine(applicationPath, applicationName + "." + task.ToString().ToLower() + ".lnk");
            if (File.Exists(shortcutFile))
                File.Delete(shortcutFile);
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(shortcutFile);
            shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
            shortcut.Arguments = drive + " " + task.ToString().ToLower();
            shortcut.IconLocation = shortcut.TargetPath; 
            shortcut.Save();
        }
        
        private void SetupEnvironment(string drive)
        {
            Notification.Push(Notification.Type.Trace, Messages.WorkerAttachEnvironmentSetup);

            var message = "";
            foreach (var file in Settings.Files)
            {
                var targetFile = file.Replace("/", @"\");
                targetFile = Regex.Replace(targetFile, @"^\\+", "");
                targetFile = Path.Combine(drive, targetFile);
                if (!File.Exists(targetFile)
                        || File.GetAttributes(targetFile).HasFlag(FileAttributes.Directory))
                    return;
                message += $"{Environment.NewLine}{targetFile}";
            
                var templateFile = targetFile + "-settings";
                if (!File.Exists(templateFile)
                        || DateTime.Compare(File.GetLastWriteTime(targetFile), File.GetLastWriteTime(templateFile)) > 0)
                    File.Copy(targetFile, templateFile, true);
                
                var templateContent = File.ReadAllText(templateFile);
                var targetContent = Settings.ReplacePlaceholders(templateContent);
                File.WriteAllText(targetFile, targetContent);

                File.SetLastWriteTime(templateFile, DateTime.Now);
            }

            if (!string.IsNullOrWhiteSpace(message))
                Notification.Push(Notification.Type.Trace,
                        $"@{Messages.WorkerAttachEnvironmentSetup}{message}");
        }

        private void Service(object payload)
        {
            if (!IsHandleCreated
                    || IsDisposed)
                return;

            var workerTask = (WorkerTask)payload;

            Invoke((MethodInvoker)delegate
            {
                // The timer should only trigger the asynchronous processing,
                // after that it is no longer needed and will be terminated.
                _timer.Dispose();

                try
                {
                    BatchResult batchResult;

                    var applicationPath = Assembly.GetExecutingAssembly().Location;
                    var applicationFile = Path.GetFileName(applicationPath);

                    switch (workerTask.Task)
                    {
                        case Task.Attach:
                            Notification.Push(Notification.Type.Trace, "@" + Messages.WorkerVersion);
                            Notification.Push(Notification.Type.Trace, Messages.WorkerAttachText);
                            Thread.Sleep(1000);

                            Diskpart.CanAttachDisk(workerTask.Drive, workerTask.DiskFile);
                            Diskpart.AttachDisk(workerTask.Drive, workerTask.DiskFile);

                            SetupEnvironment(workerTask.Drive);
                            
                            Notification.Push(Notification.Type.Trace, Messages.WorkerAttachText);
                            batchResult = BatchExec(workerTask.Task, workerTask.Drive + @"\Startup.cmd", "startup");
                            if (batchResult.Failed)
                            {
                                if (batchResult.Output.Length > 0)
                                    Notification.Push(Notification.Type.Trace, "@" + batchResult.Output);
                                throw new DiskpartException(Messages.WorkerAttachFailed, Messages.WorkerAttachBatchFailed, "@" + batchResult.Message);
                            }

                            Notification.Push(Notification.Type.Abort, Messages.WorkerAttach, Messages.WorkerSuccessfullyCompleted);
                            break;

                        case Task.Create:
                            Notification.Push(Notification.Type.Trace, "@" + Messages.WorkerVersion);
                            Notification.Push(Notification.Type.Trace, Messages.WorkerCreateText);
                            Thread.Sleep(1000);

                            Diskpart.CanCreateDisk(workerTask.Drive, workerTask.DiskFile);
                            Diskpart.CreateDisk(workerTask.Drive, workerTask.DiskFile);

                            var applicationDirectory = Path.GetDirectoryName(applicationPath);
                            var applicationName = Path.GetFileNameWithoutExtension(applicationFile);
                            var settingsFile = Path.Combine(applicationDirectory, applicationName + ".ini");
                            File.WriteAllBytes(settingsFile, Resources.GetResource(@"\settings.ini"));

                            Notification.Push(Notification.Type.Abort, Messages.DiskpartCreate, Messages.WorkerSuccessfullyCompleted);
                            break;
                        
                        case Task.Compact:
                            Notification.Push(Notification.Type.Trace, "@" + Messages.WorkerVersion);
                            Notification.Push(Notification.Type.Trace, Messages.WorkerCompactText);
                            Thread.Sleep(1000);

                            Diskpart.CanAttachDisk(workerTask.Drive, workerTask.DiskFile);
                            Diskpart.AttachDisk(workerTask.Drive, workerTask.DiskFile);

                            Notification.Push(Notification.Type.Trace, Messages.DiskpartCompact, Messages.WorkerCompactCleanFilesystem);

                            void DeleteFileEntry(string path)
                            {
                                try
                                {
                                    var fileAttributes = File.GetAttributes(path);
                                    if (fileAttributes.HasFlag(FileAttributes.Directory)
                                            && Directory.Exists(path))
                                        Directory.Delete(path, true);
                                    if (!fileAttributes.HasFlag(FileAttributes.Directory)
                                            && File.Exists(path))
                                        File.Delete(path);
                                }
                                catch
                                {
                                }
                            }

                            var tempDirectory = Path.Combine(workerTask.Drive, "Temp");
                            DeleteFileEntry(tempDirectory);
                            Directory.CreateDirectory(tempDirectory);

                            var recycleDirectory = Path.Combine(workerTask.Drive, "$RECYCLE.BIN");
                            DeleteFileEntry(recycleDirectory);
                            
                            Diskpart.CanDetachDisk(workerTask.Drive, workerTask.DiskFile);
                            Diskpart.DetachDisk(workerTask.Drive, workerTask.DiskFile);
                            
                            Diskpart.CanCompactDisk(workerTask.Drive, workerTask.DiskFile);
                            Diskpart.CompactDisk(workerTask.Drive, workerTask.DiskFile);
                            
                            Notification.Push(Notification.Type.Abort, Messages.DiskpartCompact, Messages.WorkerSuccessfullyCompleted);
                            
                            break;
                        
                        case Task.Detach:
                            Notification.Push(Notification.Type.Trace, "@" + Messages.WorkerVersion);
                            Notification.Push(Notification.Type.Trace, Messages.WorkerDetachText);
                            Thread.Sleep(1000);

                            Diskpart.CanDetachDisk(workerTask.Drive, workerTask.DiskFile);

                            batchResult = BatchExec(workerTask.Task, workerTask.Drive + @"\Startup.cmd", "exit");
                            if (batchResult.Failed)
                            {
                                if (batchResult.Output.Length > 0)
                                    Notification.Push(Notification.Type.Trace, "@" + batchResult.Output);
                                throw new DiskpartException(Messages.WorkerDetachFailed, Messages.WorkerAttachBatchFailed, "@" + batchResult.Message);
                            }
                            
                            GetProcesses()
                                .FindAll(processInfo => processInfo.Path != null)
                                .FindAll(processInfo => processInfo.Path.StartsWith(workerTask.Drive))
                                .ForEach(processInfo => processInfo.Process.CloseMainWindow());
                            Thread.Sleep(3000);
                            
                            GetProcesses()
                                .FindAll(processInfo => processInfo.Path != null)
                                .FindAll(processInfo => processInfo.Path.StartsWith(workerTask.Drive))
                                .ForEach(KillProcess);

                            Diskpart.DetachDisk(workerTask.Drive, workerTask.DiskFile);

                            Notification.Push(Notification.Type.Abort, Messages.WorkerDetach, Messages.WorkerSuccessfullyCompleted);
                            break;
                     
                        case Task.Shortcuts:
                            Notification.Push(Notification.Type.Trace, "@" + Messages.WorkerVersion);
                            Notification.Push(Notification.Type.Trace, Messages.WorkerShortcutsText);
                            Thread.Sleep(1000);
                            
                            CreateShortcut(workerTask.Drive, workerTask.DiskFile, Task.Attach);
                            CreateShortcut(workerTask.Drive, workerTask.DiskFile, Task.Detach);
                            CreateShortcut(workerTask.Drive, workerTask.DiskFile, Task.Compact);

                            Notification.Push(Notification.Type.Abort, Messages.WorkerShortcuts, Messages.WorkerSuccessfullyCompleted);
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
                    else if (exception is DiskpartException diskpartException)
                        Notification.Push(Notification.Type.Error, diskpartException.Messages);
                    else Notification.Push(Notification.Type.Error, Messages.WorkerUnexpectedErrorOccurred, exception);

                    if (new []{Task.Attach, Task.Create, Task.Compact}.Contains(workerTask.Task))
                        Diskpart.AbortDisk(workerTask.Drive, workerTask.DiskFile);
                }
            });
        }

        void Notification.INotification.Receive(Notification.Message message)
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