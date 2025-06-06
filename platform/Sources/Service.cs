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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace VirtualEnvironment.Platform
{
    internal static class Service
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, 
            StringBuilder lpBuffer, int nSize, IntPtr Arguments);
        
        private const int BATCH_PROCESS_IDLE_TIMEOUT_SECONDS = 30;
        
        private static readonly Regex PATTERN_PLACEHOLDER =
            new Regex(@"#\[\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)\s*\]", RegexOptions.IgnoreCase);

        private static readonly Regex PATTERN_FILESYSTEM_ENTRY =
            new Regex(@"^[A-Za-z]:([\\/]+( *)[^\x00-\x20\\/:*?""<>|])([\\/]+( *)[^\x00-\x20:*?""<>|]*)*$", RegexOptions.IgnoreCase);

        private const string PLATFORM_PATH_RECYCLE_BIN = @"$RECYCLE.BIN";
        private const string PLATFORM_PATH_STORAGE = @"Storage";
        private const string PLATFORM_PATH_STORAGE_PLATFORM_DATA = @"Storage\platform.data";
        private const string PLATFORM_PATH_TEMP = @"Temp";
        
        private static int NotificationDelay =>
            Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly() ? 25 : 1000;

        private static int ShutdownTimeout =>
            Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly() ? 3000 : 5000;
        
        private class StorageSymLink : IComparable<StorageSymLink>
        {
            internal DirectoryInfo StorageDirectory { get; }
            internal string StoragePath { get; }
            internal DirectoryInfo TargetDirectory { get; }
            internal string TargetName { get; }
            internal FileInfo TargetMountPoint { get; }
        
            internal StorageSymLink(DirectoryInfo storage, string path)
            {
                StorageDirectory = storage;
                StoragePath = path;
                
                // Only path that...
                // - begin with %, it is a question of mapping in the storage
                // - for which a corresponding entry exists in Storage
                // - begins with a drive after resolving
                // - does not contain any invalid path characters
                // - drive of the path must exist

                var pathNormal = Environment.ExpandEnvironmentVariables(
                    Regex.Replace(path, "^%%([A-Za-z])%", "$1:")).Trim(); 
                if (!PATTERN_FILESYSTEM_ENTRY.IsMatch(pathNormal))
                    throw new StorageSymLinkException("Target drive is missing");
                if (!File.Exists(Path.Combine(storage.FullName, path))
                        && !Directory.Exists(Path.Combine(storage.FullName, path)))
                    throw new StorageSymLinkException("Source does not exist");
                var rootPath = Path.GetPathRoot(pathNormal);
                if (String.IsNullOrEmpty(rootPath)
                        || !Directory.Exists(rootPath))
                    throw new StorageSymLinkException("Target drive does not exist");
                
                pathNormal = Path.GetFullPath(pathNormal);
                TargetDirectory = new DirectoryInfo(Path.GetDirectoryName(pathNormal) ?? Path.GetPathRoot(pathNormal));
                TargetName = Path.GetFileName(pathNormal); 
                    
                TargetMountPoint = null;
                var pathSegments = new List<String>(TargetDirectory.FullName.Split(Path.DirectorySeparatorChar));
                for (var index = 1; index < pathSegments.Count; index++)
                {
                    var mountPoint = Path.Combine(pathSegments.GetRange(0, index + 1).ToArray());
                    if (Directory.Exists(mountPoint))
                        continue;
                    TargetMountPoint = new FileInfo(mountPoint);
                    break;
                }
            }   

            internal void Create()
            {
                Directory.CreateDirectory(TargetDirectory.FullName);

                var target = Path.Combine(StorageDirectory.FullName, StoragePath);
                var symlink = Path.Combine(TargetDirectory.FullName, TargetName);
                if (Directory.Exists(target))
                {
                    if (CreateSymbolicLink(symlink, target, 1))
                        return;
                }
                else if (File.Exists(target))
                {
                    if (CreateSymbolicLink(symlink, target, 0))
                        return;
                }
                else throw new FileNotFoundException(target);
                var errorCode = Marshal.GetLastWin32Error();
                var errorMessage = new StringBuilder(256);
                FormatMessage(0x1000, IntPtr.Zero, errorCode, 0, errorMessage, errorMessage.Capacity, IntPtr.Zero);
                throw new IOException($"{errorCode}: {errorMessage}");
            }

            public int CompareTo(StorageSymLink compare)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(
                    TargetMountPoint?.FullName, 
                    compare.TargetMountPoint?.FullName);
            }

            public override string ToString()
            {
                return Path.Combine(TargetDirectory.FullName, TargetName);
            }
        }
        
        private class StorageSymLinkException : Exception
        {
            internal StorageSymLinkException(string message)
                : base(message)
            {
            }
        }
        
        private static void AttachCustomizeFile(string drive, string file)
        {
            var targetFile = file.Replace("/", @"\").Trim();
            if (!targetFile.StartsWith(@"\"))
                return;
            
            targetFile = Regex.Replace(targetFile, @"^\\+", String.Empty);
            targetFile = Path.Combine(drive, targetFile);
            if (!File.Exists(targetFile)
                    || File.GetAttributes(targetFile).HasFlag(FileAttributes.Directory))
                return;
            
            Messages.Push(Messages.Type.Trace, Resources.ServiceAttachEnvironment, Resources.ServiceAttachCustomizeFiles);
            Messages.Push(Messages.Type.Trace, Resources.ServiceAttachEnvironment, Resources.ServiceAttachCustomizeFiles, targetFile);
            
            var templateFile = targetFile + "-template";
            if (!File.Exists(templateFile)
                    || DateTime.Compare(File.GetLastWriteTime(targetFile), File.GetLastWriteTime(templateFile)) > 0)
                File.Copy(targetFile, templateFile, true);
            var templateContent = File.ReadAllText(templateFile);
            var targetContent = PATTERN_PLACEHOLDER.Replace(templateContent, match =>
            {
                var key = match.Groups[1].Value;
                return Settings.Environment.TryGetValue(key, value: out var expression)
                    ? expression : match.ToString();
            });
            File.WriteAllText(targetFile, targetContent);
            File.SetLastWriteTime(templateFile, DateTime.Now);
        }

        private static bool PathDriveExists(string path)
        {
            var root = Path.GetPathRoot(path);
            if (String.IsNullOrEmpty(root))
                return false;
            return DriveInfo.GetDrives().Any(driveInfo =>
                    driveInfo.Name.Equals(root, StringComparison.OrdinalIgnoreCase));
        }

        private static void AttachHostFilesystem(string drive)
        {
            var storage = new DirectoryInfo(Path.Combine(drive, PLATFORM_PATH_STORAGE));
            var storageSymLinks = Settings.Filesystem
                .Select(path =>
                    {
                        Messages.Push(Messages.Type.Trace, Resources.ServiceAttachHostFilesystem, path);            
                        try { return new StorageSymLink(storage, path); }
                        catch (Exception exception)
                        {
                            Messages.Push(Messages.Type.Error, Resources.ServiceAttachHostFilesystem, exception.Message);            
                            return null;
                        }
                    })
                .Where(storageSymLink => storageSymLink != null)
                .OrderBy(storageSymLink => storageSymLink)
                .ToArray();
            foreach (var storageSymLink in storageSymLinks)
            {
                Messages.Push(Messages.Type.Trace, Resources.ServiceAttachEnvironment, Resources.ServiceAttachHostFilesystem);
                Messages.Push(Messages.Type.Trace, Resources.ServiceAttachEnvironment, Resources.ServiceAttachHostFilesystem, storageSymLink.ToString());
                storageSymLink.Create();
                File.AppendAllLines(
                    Path.Combine(drive, PLATFORM_PATH_STORAGE_PLATFORM_DATA),
                    new[] {storageSymLink.TargetMountPoint.FullName});
            }
        }
        
        private static void AttachHostRegistry(string drive)
        {
            // TODO:
        }

        internal static void Attach(string drive, string diskFile)
        {
            // If the platform crashes or terminated unexpectedly, the file 
            // /Storage/platform.data remains in the virtual filesystem. Changes
            // to the host filesystem and registry recorded in this file may
            // still exist and must be cleaned up to prevent issues with
            // resource initialization during the next startup. The cleanup runs 
            // automatically, and upon successful completion, platform.data is
            // deleted.
            var platformData = Path.Combine(drive, PLATFORM_PATH_STORAGE_PLATFORM_DATA);
            if (File.Exists(platformData))
            {
                Messages.Push(Messages.Type.Trace, Resources.ServiceRestore, Resources.ServiceRestoreText);
                Thread.Sleep(NotificationDelay);
                DetachHostFilesystem(drive, true);
                DetachHostRegistry(drive, true);
            }
            
            Messages.Push(Messages.Type.Trace, Resources.ServiceAttach, Resources.ServiceAttachText);
            Thread.Sleep(NotificationDelay);

            Diskpart.CanAttachDisk(drive, diskFile);
            Diskpart.AttachDisk(drive, diskFile);
            
            Messages.Push(Messages.Type.Trace, Resources.ServiceAttachEnvironment);
            AttachHostFilesystem(drive);
            AttachHostRegistry(drive);
            foreach (var file in Settings.Customs)
                AttachCustomizeFile(drive, file);
                            
            Messages.Push(Messages.Type.Trace, Resources.ServiceAttach, Resources.ServiceAttachText);
            var batchResult = BatchExec(BatchType.Attach, drive + @"\Startup.cmd", "startup");
            if (batchResult.Failed)
            {
                if (batchResult.Output.Length > 0)
                    Messages.Push(Messages.Type.Trace, Resources.ServiceAttach, batchResult.Output);
                throw new ServiceException(Resources.ServiceAttachFailed, Resources.ServiceBatchFailed, batchResult.Message);
            }

            Messages.Push(Messages.Type.Trace, Resources.ServiceAttach, Resources.ApplicationCompleted);
            Messages.Push(Messages.Type.Exit);
        }

        internal static void Create(string drive, string diskFile)
        {
            // Resources for the creation are not contained in the platform.dll,
            // as these would have to contain the platform.dll itself, which is
            // an endless recurrence. This is why create is not supported in
            // this case.  
            if (Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly())
                throw new InvalidOperationException("Method not supported");
            
            Messages.Push(Messages.Type.Trace, Resources.ServiceCreate, Resources.ServiceCreateText);
            Thread.Sleep(NotificationDelay);

            Diskpart.CanCreateDisk(drive, diskFile);
            Diskpart.CreateDisk(drive, diskFile);

            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var applicationFile = Path.GetFileName(applicationPath);
            var applicationDirectory = Path.GetDirectoryName(applicationPath);
            var applicationName = Path.GetFileNameWithoutExtension(applicationFile);
            var settingsFile = Path.Combine(applicationDirectory, applicationName + ".ini");
            File.WriteAllBytes(settingsFile, Resources.Files[@"\settings.ini"]);

            Messages.Push(Messages.Type.Trace, Resources.ServiceCreate, Resources.ApplicationCompleted);
            Messages.Push(Messages.Type.Exit);
        }

        internal static void Compact(string drive, string diskFile)
        {
            Messages.Push(Messages.Type.Trace, Resources.ServiceCompact, Resources.ServiceCompactText);
            Thread.Sleep(NotificationDelay);

            Diskpart.CanAttachDisk(drive, diskFile);
            Diskpart.AttachDisk(drive, diskFile);

            Messages.Push(Messages.Type.Trace, Resources.ServiceCompact, Resources.ServiceCompactCleanFilesystem);

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
                catch (Exception)
                {
                }
            }

            var tempDirectory = Path.Combine(drive, PLATFORM_PATH_TEMP);
            DeleteFileEntry(tempDirectory);
            Directory.CreateDirectory(tempDirectory);

            var recycleDirectory = Path.Combine(drive, PLATFORM_PATH_RECYCLE_BIN);
            DeleteFileEntry(recycleDirectory);
            
            Diskpart.CanDetachDisk(drive, diskFile);
            Diskpart.DetachDisk(drive, diskFile);
            
            Diskpart.CanCompactDisk(drive, diskFile);
            Diskpart.CompactDisk(drive, diskFile);
            
            Messages.Push(Messages.Type.Trace, Resources.ServiceCompact, Resources.ApplicationCompleted);
            Messages.Push(Messages.Type.Exit);
        }

        private static List<Process> GetProcesses(string drive)
        {
            var wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var collection = searcher.Get())
            {
                var query = from process in Process.GetProcesses()
                    join managementObject in collection.Cast<ManagementObject>()
                        on process.Id equals (int)(uint)managementObject["ProcessId"]
                    where managementObject["ExecutablePath"] != null
                            && ((string)managementObject["ExecutablePath"])
                                    .StartsWith(drive, StringComparison.OrdinalIgnoreCase)
                    select process;
                return query.ToList();
            }
        }
        
        private static void KillProcess(Process process)
        {
            // The process is completed in three stages. First friendly, then
            // gentle, then hard. Here, gentle and hard are implemented.
            // Friendly had no effect before. Gentle tries to end the process
            // with the process structure. Occurring errors are ignored.
            
            try
            {
                var taskkill = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "taskkill.exe",
                        Arguments = $"/t /pid {process.Id}"
                    }
                };
                taskkill.Start();
                taskkill.WaitForExit();
            }
            catch (Exception)
            {
            }
            finally
            {
                // Killing the processes can block (e.g. system protection by
                // the virus scanner). Therefore, we try up to 3 times with a
                // small pause. After three attempts, the process is ignored and
                // the drive is detached.
                for (var index = 0; index < 3; index++)
                {
                    Thread.Sleep(ShutdownTimeout /2);
                    try
                    {
                        Process.GetProcessById(process.Id);
                        process.Kill();
                        break;
                    }
                    catch (Exception exception)
                    {
                        if (exception is ArgumentException)
                            break;
                        Messages.Push(Messages.Type.Warning,
                                String.Format(Resources.ServiceDetachBlocked,
                                        process.ProcessName, exception.Message));
                    }
                }
            }
        }
        
        private struct BatchResult
        {
            internal string Message;
            internal string Output;
            internal bool   Failed;
        }

        private enum BatchType
        {
            Attach,
            Detach
        }

        private static BatchResult BatchExec(BatchType type, string fileName, params string[] arguments)
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
                        && BatchType.Detach.Equals(type))
                    return;
                processStartInfo.EnvironmentVariables[name] = value;
            });

            foreach(KeyValuePair<string, string> value in Settings.Environment)
                SetEnvironmentVariableIfNecessary(value.Key, value.Value);
            
            SetEnvironmentVariableIfNecessary("PLATFORM_NAME", applicationName);
            SetEnvironmentVariableIfNecessary("PLATFORM_HOME", applicationDirectory);
            SetEnvironmentVariableIfNecessary("PLATFORM_DISK", diskFile);
            SetEnvironmentVariableIfNecessary("PLATFORM_APP", applicationPath);
            var rootPath = Path.GetPathRoot(fileName);
            SetEnvironmentVariableIfNecessary("PLATFORM_HOMEDRIVE", rootPath.Substring(0, 2));

            var batchResult = new BatchResult() {Output = String.Empty};
            
            try
            {
                var process = Process.Start(processStartInfo);
                process.OutputDataReceived += (sender, eventArgs) =>
                    batchResult.Output += $"{Environment.NewLine}{eventArgs.Data}";
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (sender, eventArgs) =>
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
                        catch (Exception)
                        {
                        }
                        throw new TimeoutException(Resources.ServiceBatchFreezeDetection);
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
        
        private static void DetachHostFilesystem(string drive, bool restore = false)
        {
            var platformData = Path.Combine(drive, PLATFORM_PATH_STORAGE_PLATFORM_DATA);
            if (!File.Exists(platformData))
                return;
            File.ReadLines(platformData)
                .Where(line => PATTERN_FILESYSTEM_ENTRY.IsMatch(line))
                .ToList()
                .ForEach(path =>
                {
                    var traceContext = restore ? Resources.ServiceRestore : Resources.ServiceDetach;
                    Messages.Push(Messages.Type.Trace, traceContext, Resources.ServiceDetachHostFilesystem);
                    Messages.Push(Messages.Type.Trace, traceContext, Resources.ServiceDetachHostFilesystem, path);
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    else if (File.Exists(path))
                        File.Delete(path);
                });
        }
        
        private static void DetachHostRegistry(string drive, bool restore = false)
        {
            // TODO:
        }

        internal static void Detach(string drive, string diskFile)
        {
            Messages.Push(Messages.Type.Trace, Resources.ServiceDetach, Resources.ServiceDetachText);
            Thread.Sleep(NotificationDelay);

            Diskpart.CanDetachDisk(drive, diskFile);
            var batchResult = BatchExec(BatchType.Detach, drive + @"\Startup.cmd", "exit");
            if (batchResult.Failed)
            {
                if (batchResult.Output.Length > 0)
                    Messages.Push(Messages.Type.Trace, Resources.ServiceDetach, batchResult.Output);
                throw new ServiceException(Resources.ServiceDetachFailed, Resources.ServiceBatchFailed, batchResult.Message);
            }
            
            // In DLL mode, launcher.exe must be excluded as it acts as the main
            // process. This applies specifically to the cases of system
            // shutdown and session ending, as launcher.exe receives these
            // events and uses the ShutdownBlockReason function to control the
            // shutdown process.

            var processes = GetProcesses(drive);
            if (Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly())
                processes = processes.Where(process => !process.ProcessName.Equals("launcher", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            processes.ForEach(process => process.CloseMainWindow());
            Thread.Sleep(ShutdownTimeout);

            processes = GetProcesses(drive);
            if (Assembly.GetExecutingAssembly() != Assembly.GetEntryAssembly())
                processes = processes.Where(process => !process.ProcessName.Equals("launcher", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            processes.ForEach(KillProcess);
            
            DetachHostFilesystem(drive);
            DetachHostRegistry(drive);
            
            Diskpart.DetachDisk(drive, diskFile);

            Messages.Push(Messages.Type.Trace, Resources.ServiceDetach, Resources.ApplicationCompleted);
            Messages.Push(Messages.Type.Exit);
        }
        
        private enum ShortcutType
        {
            Attach,
            Compact,
            Detach
        }

        private static void CreateShortcut(string drive, string diskFile, ShortcutType type)
        {
            var applicationPath = Path.GetDirectoryName(diskFile);
            var applicationName = Path.GetFileNameWithoutExtension(diskFile);
            var wshShell = new WshShell();
            var shortcutFile = Path.Combine(applicationPath, applicationName + "." + type.ToString().ToLower() + ".lnk");
            if (File.Exists(shortcutFile))
                File.Delete(shortcutFile);
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutFile);
            shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
            shortcut.Arguments = drive + " " + type.ToString().ToLower();
            shortcut.IconLocation = shortcut.TargetPath; 
            shortcut.Save();
        }

        internal static void Shortcuts(string drive, string diskFile)
        {
            Messages.Push(Messages.Type.Trace, Resources.ServiceShortcuts, Resources.ServiceShortcutsText);
            Thread.Sleep(NotificationDelay);
                            
            CreateShortcut(drive, diskFile, ShortcutType.Attach);
            CreateShortcut(drive, diskFile, ShortcutType.Detach);
            CreateShortcut(drive, diskFile, ShortcutType.Compact);

            Messages.Push(Messages.Type.Trace, Resources.ServiceShortcuts, Resources.ApplicationCompleted);
            Messages.Push(Messages.Type.Exit);
        }
    }
    
    internal class ServiceException : Exception
    {
        internal string Context { get; }
        internal string Details { get; }

        internal ServiceException(string context, string message, string details = null)
            : base(message)
        {
            Context = context;
            Details = details;
        }
    }
}