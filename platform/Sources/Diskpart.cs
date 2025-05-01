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
using System.Text;

namespace VirtualEnvironment.Platform
{
    internal static class Diskpart
    {
        private const string DISK_TYPE   = "expandable";
        private const int    DISK_SIZE   = 128000;
        private const string DISK_STYLE  = "GPT";
        private const string DISK_FORMAT = "NTFS";

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
            var diskpartScript = Resources.Texts[diskpartScriptName];
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
                throw new DiskpartException(Resources.DiskpartCompactFailed, Resources.DiskpartFileNotExists);
        }

        internal static void CompactDisk(string drive, string diskFile)
        {
            Messages.Push(Messages.Type.Trace, Resources.DiskpartCompact);
            CanCompactDisk(drive, diskFile);

            Messages.Push(Messages.Type.Trace, Resources.DiskpartCompact, Resources.DiskpartCompactDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Compact, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Resources.DiskpartCompactFailed,
                    Resources.DiskpartUnexpectedErrorOccurred,
                    diskpartResult.Output);
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

        private static bool IsVirtualDisk(string drive)
        {
            var query = "SELECT * FROM Win32_DiskDrive";
            using (var disks = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject disk in disks.Get())
                {
                    var partitions = disk.GetRelated("Win32_DiskPartition");
                    foreach (ManagementObject partition in partitions)
                    {
                        var logicalDisks = partition.GetRelated("Win32_LogicalDisk");
                        foreach (ManagementObject logicalDisk in logicalDisks)
                        {
                            var logicalDrive = logicalDisk["DeviceID"]?.ToString();
                            if (!drive.Equals(logicalDrive, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            var pnpDeviceId = disk["PNPDeviceID"]?.ToString();
                            if (pnpDeviceId != null
                                    && pnpDeviceId.IndexOf("VIRTUAL_DISK", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                            
                            var mediaType = disk["MediaType"]?.ToString();
                            if (mediaType != null
                                    && mediaType.IndexOf("VIRTUAL", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        internal static void CanAttachDisk(string drive, string diskFile)
        {
            var volume = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            var driveInfo = new DriveInfo(drive);
            if (driveInfo.IsReady)
            {
                if (!driveInfo.VolumeLabel.Equals(volume, StringComparison.OrdinalIgnoreCase)
                        || GetProcesses(drive).Any()
                        || !IsVirtualDisk(drive))
                    throw new DiskpartAbortException(Resources.DiskpartAttachAbort, Resources.DiskpartDriveAlreadyUsed);
                return;
            }
            
            if (Directory.Exists(drive))
                throw new DiskpartAbortException(Resources.DiskpartAttachAbort, Resources.DiskpartDriveAlreadyExists);
            if (!File.Exists(diskFile))
                throw new DiskpartAbortException(Resources.DiskpartAttachAbort, Resources.DiskpartFileNotExists);
        }

        internal static void AttachDisk(string drive, string diskFile)
        {
            // Because of the GPT used, it is important when attaching:
            // - The first partition is preserve partition, it cannot be mount
            // - The second partition is the real data partition 
            
            Messages.Push(Messages.Type.Trace, Resources.DiskpartAttach);
            CanAttachDisk(drive, diskFile);
            if (!Directory.Exists(drive))
            {
                Messages.Push(Messages.Type.Trace,
                    Resources.DiskpartAttach,
                    String.Format(Resources.DiskpartAttachDiskpart, drive));
                var diskpartResult = DiskpartExec(DiskpartTask.Attach, new DiskpartProperties() {
                    File = diskFile,
                    Drive = drive.Substring(0, 1)
                });
                if (diskpartResult.Failed)
                    throw new DiskpartException(Resources.DiskpartAttachFailed,
                        Resources.DiskpartUnexpectedErrorOccurred,
                        diskpartResult.Output);
            }
            else
            {
                Messages.Push(Messages.Type.Trace,
                    Resources.DiskpartAttach,
                    String.Format(Resources.DiskpartAttachExistingDrive, drive));
            }
        }

        internal static void CanDetachDisk(string drive, string diskFile)
        {
            if (!File.Exists(diskFile))
                throw new DiskpartException(Resources.DiskpartDetachFailed, Resources.DiskpartFileNotExists);
        }

        internal static void AbortDisk(string drive, string diskFile, bool abort = false)
        {
            DiskpartExec(DiskpartTask.Detach, new DiskpartProperties() {File = diskFile});
        }

        internal static void DetachDisk(string drive, string diskFile, bool abort = false)
        {
            Messages.Push(Messages.Type.Trace, Resources.DiskpartDetach);
            CanDetachDisk(drive, diskFile);

            Messages.Push(Messages.Type.Trace, Resources.DiskpartDetach, Resources.DiskpartDetachDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Detach, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Resources.DiskpartDetachFailed,
                    Resources.DiskpartUnexpectedErrorOccurred,
                    diskpartResult.Output);
        }

        private static void MigrateResourcePlatformFile(string drive, string resourcePlatformPath, Dictionary<string, string> replacements = null)
        {
            var fileContent = Resources.Files[@"\platform\" + resourcePlatformPath];
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
                throw new DiskpartException(Resources.DiskpartCreateFailed, Resources.DiskpartDriveAlreadyExists);
            if (File.Exists(diskFile))
                throw new DiskpartException(Resources.DiskpartCreateFailed, Resources.DiskpartFileAlreadyExists);
        }

        internal static void CreateDisk(string drive, string diskFile)
        {
            Messages.Push(Messages.Type.Trace, Resources.DiskpartCreate);
            CanCreateDisk(drive, diskFile);

            var diskpartProperties = new DiskpartProperties()
            {
                File   = diskFile,
                Type   = DISK_TYPE,
                Size   = DISK_SIZE,
                Style  = DISK_STYLE,
                Format = DISK_FORMAT,
                Name   = Path.GetFileNameWithoutExtension(diskFile)
            };

            Messages.Push(Messages.Type.Trace, Resources.DiskpartCreate, Resources.DiskpartCreateDiskpart);
            var diskpartResult = DiskpartExec(DiskpartTask.Create, diskpartProperties);
            if (diskpartResult.Failed)
                throw new DiskpartException(Resources.DiskpartCreateFailed,
                    Resources.DiskpartUnexpectedErrorOccurred,
                    diskpartResult.Output);

            AttachDisk(drive, diskFile);
            
            Messages.Push(Messages.Type.Trace, Resources.DiskpartCreate, Resources.DiskpartCreateInitializationFileSystem);
            Directory.CreateDirectory(drive + @"\Documents\Music");
            Directory.CreateDirectory(drive + @"\Documents\Pictures");
            Directory.CreateDirectory(drive + @"\Documents\Videos");
            Directory.CreateDirectory(drive + @"\Programs\Macros\macros");
            Directory.CreateDirectory(drive + @"\Resources");
            Directory.CreateDirectory(drive + @"\Settings");
            Directory.CreateDirectory(drive + @"\Storage");
            Directory.CreateDirectory(drive + @"\Temp");

            var replacements = new Dictionary<string, string>
            {
                { "drive", drive },
                { "name", Path.GetFileNameWithoutExtension(diskFile) },
                { "version", $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.x" }
            };

            MigrateResourcePlatformFile(drive, @"\Programs\Platform\startup.exe");
            MigrateResourcePlatformFile(drive, @"\Programs\Platform\launcher.exe");
            MigrateResourcePlatformFile(drive, @"\Programs\Platform\launcher.xml");
            MigrateResourcePlatformFile(drive, @"\Programs\Platform\platform.dll");
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
        internal DiskpartAbortException(params string[] messages) : base(messages)
        {
        }
    }    
}