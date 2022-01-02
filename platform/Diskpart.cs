// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
// Copyright (C) 2022 Seanox Software Solutions
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Platform
{
    internal static class Diskpart
    {
        private enum DiskpartTask
        {
            Assign,
            Attach,
            Compact,
            Create,
            Detach,
            List
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
            internal int    Number;
        }

        // It is a balancing act between notifications that work comparable to
        // a trace log and a usable exception handling.

        private static byte[] GetResource(string resourceName)
        {
            resourceName = String.Format("{0}.{1}.{2}", typeof(Diskpart).Namespace, "Resources", resourceName);
            resourceName = new Regex("[\\./\\\\]+").Replace(resourceName, ".");
            resourceName = new Regex("\\s").Replace(resourceName, "_");
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                var buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                return buffer;
            }
        }

        private static string GetTextResource(string resourceName)
        {
            return Encoding.ASCII.GetString(Diskpart.GetResource(resourceName));
        }

        private struct DiskpartResult
        {
            internal string Output;
            internal bool   Failed;
        }

        private static DiskpartResult DiskpartExec(DiskpartTask diskpartTask, DiskpartProperties diskpartProperties)
        {
            var diskpartScriptName = "diskpart." + diskpartTask.ToString().ToLower();
            var diskpartScript = Diskpart.GetTextResource(diskpartScriptName);
            diskpartScript = typeof(DiskpartProperties).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Aggregate(diskpartScript, (current, field) =>
                            current.Replace(String.Format("#[{0}]", field.Name.ToLower()),
                                (field.GetValue(diskpartProperties) ?? "").ToString()));

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
                diskpartResult.Output = (process.StandardError.ReadToEnd() ?? "").Trim();
                if (diskpartResult.Output.Length <= 0)
                    diskpartResult.Output = (process.StandardOutput.ReadToEnd() ?? "").Trim();
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
            Diskpart.CanCompactDisk(drive, diskFile);

            Notification.Push(Notification.Type.Trace, Messages.DiskpartCompact, Messages.DiskpartCompactDiskpart);
            var diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Compact, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartCompactFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
        }

        internal static void CanAttachDisk(string drive, string diskFile)
        {
            if (Directory.Exists(drive))
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartDriveAlreadyExists);
            if (!File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartFileNotExists);
        }

        internal static void AttachDisk(string drive, string diskFile)
        {
            DiskpartResult diskpartResult;

            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach);
            Diskpart.CanAttachDisk(drive, diskFile);

            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach, Messages.DiskpartAttachDiskpart);
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Attach, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach, Messages.DiskpartAttachDetectVolume);
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.List, new DiskpartProperties());
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
            var volumeNumberPattern = new Regex(@"^\s*Volume\s+(\d+)\s+([A-Z]\s+)?" + Path.GetFileNameWithoutExtension(diskFile), RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var volumeNumberMatch = volumeNumberPattern.Match(diskpartResult.Output);
            if (!volumeNumberMatch.Success)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartVolumeNotFound, "@" + diskpartResult.Output);
            var volumeNumber = int.Parse(volumeNumberMatch.Groups[1].Value);
            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach, String.Format(Messages.DiskpartAttachAssign, volumeNumber, drive));
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Assign, new DiskpartProperties()
            {
                Number = volumeNumber,
                Drive  = drive.Substring(0, 1)
            });
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
        }

        internal static void CanDetachDisk(string drive, string diskFile)
        {
            if (!Directory.Exists(drive))
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartDriveNotExists);
            if (!File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartFileNotExists);
        }

        internal static void DetachDisk(string drive, string diskFile)
        {
            Notification.Push(Notification.Type.Trace, Messages.DiskpartDetach);
            Diskpart.CanDetachDisk(drive, diskFile);

            Notification.Push(Notification.Type.Trace, Messages.DiskpartDetach, Messages.DiskpartDetachDiskpart);
            var diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Detach, new DiskpartProperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
        }

        private static char GetNextDriveLetter()
        {
            var availableDriveLetters = new List<char>();
            for (var letter = 'A'; letter < 'Z'; letter++)
                availableDriveLetters.Add(letter);
            foreach (var driveInfo in DriveInfo.GetDrives())
                availableDriveLetters.Remove(driveInfo.Name.ToUpper().ToCharArray()[0]);
            return availableDriveLetters.FirstOrDefault();
        }

        private static void MigrateResourcePlatformFile(string drive, string resourcePlatformPath)
        {
            Diskpart.MigrateResourcePlatformFile(drive, resourcePlatformPath, null);
        }

        private static void MigrateResourcePlatformFile(string drive, string resourcePlatformPath, Dictionary<string, string> replacements)
        {
            byte[] fileContent = Diskpart.GetResource(@"\platform\" + resourcePlatformPath);
            if (replacements != null)
            {
                var fileContentText = Encoding.ASCII.GetString(fileContent);
                foreach (var key in replacements.Keys)
                    fileContentText = fileContentText.Replace(String.Format("#[{0}]", key.ToLower()),
                            replacements[key]);
                fileContent = Encoding.ASCII.GetBytes(fileContentText);
            }
            var targetDirectory = Path.GetDirectoryName(drive + resourcePlatformPath);
            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            File.WriteAllBytes(drive + resourcePlatformPath, fileContent);
        }

        internal static void CanCreateDisk(string drive, string diskFile)
        {
            if (File.Exists(diskFile))
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartFileAlreadyExists);
        }

        internal static void CreateDisk(string drive, string diskFile)
        {
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate);
            Diskpart.CanCreateDisk(drive, diskFile);

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
            var diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Create, diskpartProperties);
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);

            var tempDriveLetter = Diskpart.GetNextDriveLetter();
            if (tempDriveLetter < 'A')
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartNoLetterAvailable);
            var tempDrive = tempDriveLetter.ToString() + ":";
            Diskpart.AttachDisk(tempDrive, diskFile);
            
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate, Messages.DiskpartCreateInitializationFileSystem);
            Directory.CreateDirectory(tempDrive + @"\Database");
            Directory.CreateDirectory(tempDrive + @"\Documents");
            Directory.CreateDirectory(tempDrive + @"\Documents\Music");
            Directory.CreateDirectory(tempDrive + @"\Documents\Pictures");
            Directory.CreateDirectory(tempDrive + @"\Documents\Projects");
            Directory.CreateDirectory(tempDrive + @"\Documents\Videos");
            Directory.CreateDirectory(tempDrive + @"\Install");
            Directory.CreateDirectory(tempDrive + @"\Program Portables");
            Directory.CreateDirectory(tempDrive + @"\Resources");
            Directory.CreateDirectory(tempDrive + @"\Settings");
            Directory.CreateDirectory(tempDrive + @"\Temp");

            var replacements = new Dictionary<string, string>();
            replacements.Add("drive", drive);
            replacements.Add("name", Path.GetFileNameWithoutExtension(diskFile));
            replacements.Add("version", String.Format("{0}.x", Assembly.GetExecutingAssembly().GetName().Version.Major));

            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Program Portables\Extensions\startup.exe");
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Resources\drive.ico");
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Resources\drive.png");
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Settings\settings.exe");
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\AutoRun.inf", replacements);
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Startup.cmd", replacements);

            Diskpart.DetachDisk(tempDrive, diskFile);
        }
    }

    internal class DiskpartException : Exception
    {
        internal string[] Messages { get; }

        internal DiskpartException(params string[] messages)
        {
            this.Messages = messages;
        }
    }
}