// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Platform
// Creates, starts and controls a virtual environment.
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
using System.Collections.Generic;
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
        
        private struct DiskpartPorperties
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

        /// It is a balancing act between notifications that work comparable to
        /// a trace log and a usable exception handling.

        private static byte[] GetResource(string resourceName)
        {
            resourceName = String.Format("{0}.{1}.{2}", typeof(Diskpart).Namespace, "Resources", resourceName);
            resourceName = new Regex("[\\./\\\\]+").Replace(resourceName, ".");
            resourceName = new Regex("\\s").Replace(resourceName, "_");
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                byte[] buffer = new byte[(int)stream.Length];
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

        private static DiskpartResult DiskpartExec(DiskpartTask diskpartTask, DiskpartPorperties diskpartPorperties)
        {
            string diskpartScriptName = "diskpart." + diskpartTask.ToString().ToLower();
            string diskpartScript = Diskpart.GetTextResource(diskpartScriptName);
            foreach (FieldInfo field in typeof(DiskpartPorperties).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                diskpartScript = diskpartScript.Replace(String.Format("#[{0}]", field.Name.ToLower()),
                        (field.GetValue(diskpartPorperties) ?? "").ToString());

            // In case the cleanup does not work and not so much junk
            // accumulates in the temp directory, fixed file names are used.

            string diskpartScriptTempFile = Path.GetTempFileName();
            string diskpartScriptDirectory = Path.GetDirectoryName(diskpartScriptTempFile);
            string diskpartScriptFile = Path.Combine(diskpartScriptDirectory, diskpartScriptName);
            File.Delete(diskpartScriptFile);
            File.Move(diskpartScriptTempFile, diskpartScriptFile);

            try
            {
                File.WriteAllBytes(diskpartScriptFile, Encoding.ASCII.GetBytes(diskpartScript));

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow  = true,
                    
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    
                    FileName  = "diskpart.exe",
                    Arguments = "/s " + diskpartScriptFile,

                    RedirectStandardError  = true,
                    RedirectStandardOutput = true
                };
                process.Start();
                process.WaitForExit();

                DiskpartResult diskpartResult = new DiskpartResult();
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
            DiskpartResult diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Compact, new DiskpartPorperties() {File = diskFile});
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
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Attach, new DiskpartPorperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach, Messages.DiskpartAttachDetectVolume);
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.List, new DiskpartPorperties());
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
            Regex volumeNumberPattern = new Regex(@"^\s*Volume\s+(\d+)\s+([A-Z]\s+)?" + Path.GetFileNameWithoutExtension(diskFile), RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Match volumeNumberMatch = volumeNumberPattern.Match(diskpartResult.Output);
            if (!volumeNumberMatch.Success)
                throw new DiskpartException(Messages.DiskpartAttachFailed, Messages.DiskpartVolumeNotFound, "@" + diskpartResult.Output);
            int volumeNumber = int.Parse(volumeNumberMatch.Groups[1].Value);
            Notification.Push(Notification.Type.Trace, Messages.DiskpartAttach, String.Format(Messages.DiskpartAttachAssign, volumeNumber, drive));
            diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Assign, new DiskpartPorperties()
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
            DiskpartResult diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Detach, new DiskpartPorperties() {File = diskFile});
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartDetachFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);
        }

        private static char GetNextDriveLetter()
        {
            List<char> availableDriveLetters = new List<char>();
            for (char letter = 'A'; letter < 'Z'; letter++)
                availableDriveLetters.Add(letter);
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
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
                string fileContentText = Encoding.ASCII.GetString(fileContent);
                foreach (string key in replacements.Keys)
                    fileContentText = fileContentText.Replace(String.Format("#[{0}]", key.ToLower()),
                            replacements[key]);
                fileContent = Encoding.ASCII.GetBytes(fileContentText);
            }
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

            DiskpartPorperties diskpartPorperties = new DiskpartPorperties()
            {
                File   = diskFile,
                Type   = Program.DISK_TYPE,
                Size   = Program.DISK_SIZE,
                Style  = Program.DISK_STYLE,
                Format = Program.DISK_FORMAT,
                Name   = Path.GetFileNameWithoutExtension(diskFile)
            };

            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate, Messages.DiskpartCreateDiskpart);
            DiskpartResult diskpartResult = Diskpart.DiskpartExec(DiskpartTask.Create, diskpartPorperties);
            if (diskpartResult.Failed)
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartUnexpectedErrorOccurred, "@" + diskpartResult.Output);

            char tempDriveLetter = Diskpart.GetNextDriveLetter();
            if (tempDriveLetter < 'A')
                throw new DiskpartException(Messages.DiskpartCreateFailed, Messages.DiskpartNoLetterAvailable);
            string tempDrive = tempDriveLetter.ToString() + ":";
            Diskpart.AttachDisk(tempDrive, diskFile);
            
            Notification.Push(Notification.Type.Trace, Messages.DiskpartCreate, Messages.DiskpartCreateInitializationFileSystem);
            Directory.CreateDirectory(tempDrive + @"\Database");
            Directory.CreateDirectory(tempDrive + @"\Documents");
            Directory.CreateDirectory(tempDrive + @"\Documents\Music");
            Directory.CreateDirectory(tempDrive + @"\Documents\Pictures");
            Directory.CreateDirectory(tempDrive + @"\Documents\Projects");
            Directory.CreateDirectory(tempDrive + @"\Documents\Settings");
            Directory.CreateDirectory(tempDrive + @"\Documents\Videos");
            Directory.CreateDirectory(tempDrive + @"\Install");
            Directory.CreateDirectory(tempDrive + @"\Program Portables");
            Directory.CreateDirectory(tempDrive + @"\Resources");
            Directory.CreateDirectory(tempDrive + @"\Temp");

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            replacements.Add("drive", drive);
            replacements.Add("name", Path.GetFileNameWithoutExtension(diskFile));
            replacements.Add("version", String.Format("{0}.x", Assembly.GetExecutingAssembly().GetName().Version.Major));

            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Resources\drive.ico");
            Diskpart.MigrateResourcePlatformFile(tempDrive, @"\Resources\drive.png");
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