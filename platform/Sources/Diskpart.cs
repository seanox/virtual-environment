// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
// Copyright (C) 2023 Seanox Software Solutions
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
using System.Text;

namespace VirtualEnvironment.Platform
{
    internal static class Diskpart
    {
        private enum DiskpartTask
        {
            Attach,
            Compact,
            Create,
            Detach
        }
        
        private struct DiskpartProperties
        {
            internal string File;
            internal string Type;
            internal int    Size;
            internal string Style;
            internal string Format;
            internal string Name;
            internal string Drive;
        }

        // It is a balancing act between notifications that work comparable to
        // a trace log and a usable exception handling.

        private struct DiskpartResult
        {
            internal string Output;
            internal bool   Failed;
        }

        private static DiskpartResult DiskpartExec(DiskpartTask diskpartTask, DiskpartProperties diskpartProperties)
        {
            var diskpartScriptName = "diskpart." + diskpartTask.ToString().ToLower();
            var diskpartScript = Resources.GetTextResource(diskpartScriptName);
            diskpartScript = typeof(DiskpartProperties).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Aggregate(diskpartScript, (current, field) =>
                            current.Replace($"#[{field.Name.ToLower()}]", (field.GetValue(diskpartProperties) ?? "").ToString()));

            // In case the cleanup does not work and not so much junk
            // accumulates in the temp directory, fixed file names are used.

            var diskpartScriptTempFile = Path.GetTempFileName();
            var diskpartScriptDirectory = Path.GetDirectoryName(diskpartScriptTempFile);
            var diskpartScriptFile = Path.Combine(diskpartScriptDirectory, diskpartScriptName);
            File.Delete(diskpartScriptFile);
            File.Move(diskpartScriptTempFile, diskpartScriptFile);

            try
            {
                File.WriteAllBytes(diskpartScriptFile, Encoding.ASCII.GetBytes(diskpartScript));

                var process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName  = "diskpart.exe",
                    Arguments = "/s " + diskpartScriptFile,
                    RedirectStandardError  = true,
                    RedirectStandardOutput = true
                };
                process.Start();
                process.WaitForExit();

                var diskpartResult = new DiskpartResult();
                diskpartResult.Output = process.StandardError.ReadToEnd().Trim();
                if (diskpartResult.Output.Length <= 0)
                    diskpartResult.Output = process.StandardOutput.ReadToEnd().Trim();
                else diskpartResult.Failed = true;
                if (process.ExitCode != 0)
                    diskpartResult.Failed = true;
                return diskpartResult;
            }
            catch (Exception exception)
            {
                return new DiskpartResult()
                {
                    Output = exception.Message,
                    Failed = true
                };
            }
            finally
            {
                if (File.Exists(diskpartScriptFile))
                    File.Delete(diskpartScriptFile);
            }
        }

        internal static void CanCompactDisk(string drive, string diskFile)
        {
            if (!File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartCompactFailed, Messages.DiskpartFileNotExists);
        }

        internal static void CompactDisk(string drive, string diskFile)
        {
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCompact);
            CanCompactDisk(drive, diskFile);

            Notification.Push(Notification.Type.Trace, Messages.DiskpartCompact, Messages.DiskpartCompactDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Compact, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartCompactFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
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

        internal static void CanAttachDisk(string drive, string diskFile)
        {
            var volume = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            var driveInfo = new DriveInfo(drive);
            if (driveInfo.IsReady)
            {
                if (!driveInfo.VolumeLabel.Equals(volume, StringComparison.OrdinalIgnoreCase)
                        || GetProcesses(drive).Any())
                    throw new DiskpartAbortException(Messages.DiskpartAttachAbort, Messages.DiskpartDriveAlreadyUsed);
                return;
            }
            
            if (Directory.Exists(drive))
                throw new DiskpartAbortException(Messages.DiskpartAttachAbort, Messages.DiskpartDriveAlreadyExists);
            if (!File.Exists(diskFile))
                throw new DiskpartAbortException(Messages.DiskpartAttachAbort, Messages.DiskpartFileNotExists);
        }

        internal static void AttachDisk(string drive, string diskFile)
        {
            // Because of the GPT used, it is important when attaching:
            // - The first partition is preserve partition, it cannot be mount
            // - The second partition is the real data partition 
            
            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach);
            CanAttachDisk(drive, diskFile);
            if (!Directory.Exists(drive))
            {
                Notification.Push(Notification.Type.Trace,
                    Messages.DiskpartAttach,
                    String.Format(Messages.DiskpartAttachDiskpart, drive));
                var diskpartResult = DiskpartExec(DiskpartTask.Attach, new DiskpartProperties() {
                    File = diskFile,
                    Drive = drive.Substring(0, 1)
                });
                if (diskpartResult.Failed)
                    throw new DiskpartException(Messages.DiskpartAttachFailed,
                        Messages.DiskpartUnexpectedErrorOccurred,
                        "@" + diskpartResult.Output);
            }
            else
            {
                Notification.Push(Notification.Type.Trace,
                    Messages.DiskpartAttach,
                    String.Format(Messages.DiskpartAttachExistingDrive, drive));
            }
        }

        internal static void CanDetachDisk(string drive, string diskFile)
        {
            if (!File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartFileNotExists);
        }

        internal static void AbortDisk(string drive, string diskFile, bool abort = false)
        {
            DiskpartExec(DiskpartTask.Detach, new DiskpartProperties() {File = diskFile});
        }

        internal static void DetachDisk(string drive, string diskFile, bool abort = false)
        {
            Notification.Push(Notification.Type.Trace, Messages.DiskpartDetach);
            CanDetachDisk(drive, diskFile);

            Notification.Push(Notification.Type.Trace, Messages.DiskpartDetach, Messages.DiskpartDetachDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Detach, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
        }

        private static void MigrateResourcePlatformFile(string drive, string resourcePlatformPath, Dictionary<string, string> replacements = null)
        {
            var fileContent = Resources.GetResource(@"\platform\" + resourcePlatformPath);
            if (replacements != null)
            {
                var fileContentText = Encoding.ASCII.GetString(fileContent);
                foreach (var key in replacements.Keys)
                    fileContentText = fileContentText.Replace($"#[{key.ToLower()}]", replacements[key]);
                fileContent = Encoding.ASCII.GetBytes(fileContentText);
            }
            var targetDirectory = Path.GetDirectoryName(drive + resourcePlatformPath);
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            File.WriteAllBytes(drive + resourcePlatformPath, fileContent);
        }

        internal static void CanCreateDisk(string drive, string diskFile)
        {
            if (Directory.Exists(drive))
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartDriveAlreadyExists);
            if (File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartFileAlreadyExists);
        }

        internal static void CreateDisk(string drive, string diskFile)
        {
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate);
            CanCreateDisk(drive, diskFile);

            var diskpartProperties = new DiskpartProperties()
            {
                File   = diskFile,
                Type   = Program.DISK_TYPE,
                Size   = Program.DISK_SIZE,
                Style  = Program.DISK_STYLE,
                Format = Program.DISK_FORMAT,
                Name   = Path.GetFileNameWithoutExtension(diskFile)
            };

            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate, Messages.DiskpartCreateDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Create, diskpartProperties);
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);

            AttachDisk(drive, diskFile);
            
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate, Messages.DiskpartCreateInitializationFileSystem);
            Directory.CreateDirectory(drive + @"\Documents\Music");
            Directory.CreateDirectory(drive + @"\Documents\Pictures");
            Directory.CreateDirectory(drive + @"\Documents\Videos");
            Directory.CreateDirectory(drive + @"\Programs\Macros\macros");
            Directory.CreateDirectory(drive + @"\Resources");
            Directory.CreateDirectory(drive + @"\Settings");
            Directory.CreateDirectory(drive + @"\Storage");
            Directory.CreateDirectory(drive + @"\Temp");

            var replacements = new Dictionary<string, string>();
            replacements.Add("drive", drive);
            replacements.Add("name", Path.GetFileNameWithoutExtension(diskFile));
            replacements.Add("version", $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.x");

            MigrateResourcePlatformFile(drive, @"\Programs\Platform\startup.exe");
            MigrateResourcePlatformFile(drive, @"\Programs\Platform\launcher.exe");
            MigrateResourcePlatformFile(drive, @"\Programs\Platform\launcher.xml");
            MigrateResourcePlatformFile(drive, @"\Programs\Macros\macros.cmd");
            MigrateResourcePlatformFile(drive, @"\Programs\Macros\macro.cmd");
            MigrateResourcePlatformFile(drive, @"\Resources\platform.ico");
            MigrateResourcePlatformFile(drive, @"\Resources\platform.png");
            MigrateResourcePlatformFile(drive, @"\AutoRun.inf", replacements);
            MigrateResourcePlatformFile(drive, @"\Startup.cmd", replacements);

            DetachDisk(drive, diskFile);
        }
    }

    internal class DiskpartException : Exception
    {
        internal string[] Messages { get; }

        internal DiskpartException(params string[] messages)
        {
            Messages = messages;
        }
    }
    
    internal class DiskpartAbortException : DiskpartException
    {
        internal string[] Messages { get; }

        internal DiskpartAbortException(params string[] messages)
        {
            Messages = messages;
        }
    }    
}