// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Startup
// Program starter for the virtual environment.
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
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Startup
{
    internal static class Scanner
    {
        // https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
        // Please read twice: 260 vs. 256 vs. 259 -1 characters
        // I don't understand how the documentary comes to 260 characters :-|
        // - drive letter (C:)             2 Chars 
        // - backslash                     1 Char
        // - max. path characters        256 Chars
        //                     Subtotal  259 Chars
        // - terminating null character   -1 Char 
        //                        Total  258 Chars
        // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem -> LongPathsEnabled
        // Possible customizations is ignored. That's too much :-|
        private const int FILE_SYSTEM_MAX_PATH = 258;

        private static readonly string SYSTEM_DRIVE;
        private static readonly string SYSTEM_MSO_CACHE_PATH;
        private static readonly string SYSTEM_RECYCLE_BIN_PATH;
        private static readonly string SYSTEM_TEMP_PATH;
        private static readonly string SYSTEM_VOLUME_INFORMATION_PATH;
        private static readonly string SYSTEM_ROOT_PATH;
        private static readonly string SYSTEM_WINDOWS_CSC_PATH;
        private static readonly string SYSTEM_WINDOWS_DEBUG_PATH;
        private static readonly string SYSTEM_WINDOWS_INSTALLER_PATH;
        private static readonly string SYSTEM_WINDOWS_LOGS_PATH;
        private static readonly string SYSTEM_WINDOWS_PREFETCH_PATH;
        private static readonly string SYSTEM_WINDOWS_SOFTWARE_DISTRIBUTION_PATH;
        private static readonly string SYSTEM_WINDOWS_TEMP_PATH;
        private static readonly string SYSTEM_WINDOWS_UUS_PATH;
        private static readonly string SYSTEM_WINDOWS_WAAS_PATH;
        private static readonly string SYSTEM_WINDOWS_WINSXS_PATH;
            
        private static readonly string USER_LOCAL_TEMP_PATH;
        private static readonly string USER_LOCALLOW_TEMP_PATH;
        private static readonly string USER_ROAMING_TEMP_PATH;
        private static readonly string USER_DOWNLOADS_PATH;
        
        private static readonly List<string> SYSTEM_NOT_RELEVANT_DIRECTORIES;
        
        // Parallel does not bring any advantages here, as the scan and compare
        // functions are not CPU-intensive enough

        static Scanner()
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (String.IsNullOrEmpty(systemDrive))
               systemDrive = "C:";
            if (!systemDrive.EndsWith(@"\"))
                systemDrive += @"\";
            SYSTEM_DRIVE = systemDrive.ToLower();
            SYSTEM_MSO_CACHE_PATH = Path.Combine(SYSTEM_DRIVE, "MSOCache");
            SYSTEM_RECYCLE_BIN_PATH = Path.Combine(SYSTEM_DRIVE, "$Recycle.Bin");
            SYSTEM_TEMP_PATH = Path.Combine(SYSTEM_DRIVE, "Temp");
            SYSTEM_VOLUME_INFORMATION_PATH = Path.Combine(SYSTEM_DRIVE, "System Volume Information");

            SYSTEM_ROOT_PATH = Environment.GetEnvironmentVariable("SystemRoot");
            SYSTEM_WINDOWS_CSC_PATH = Path.Combine(SYSTEM_ROOT_PATH, "CSC");
            SYSTEM_WINDOWS_DEBUG_PATH = Path.Combine(SYSTEM_ROOT_PATH, "Debug");
            SYSTEM_WINDOWS_INSTALLER_PATH = Path.Combine(SYSTEM_ROOT_PATH, "Installer");
            SYSTEM_WINDOWS_LOGS_PATH = Path.Combine(SYSTEM_ROOT_PATH, "Logs");
            SYSTEM_WINDOWS_PREFETCH_PATH = Path.Combine(SYSTEM_ROOT_PATH, "Prefetch");
            SYSTEM_WINDOWS_SOFTWARE_DISTRIBUTION_PATH = Path.Combine(SYSTEM_ROOT_PATH, "SoftwareDistribution");
            SYSTEM_WINDOWS_TEMP_PATH = Path.Combine(SYSTEM_ROOT_PATH, "Temp");
            SYSTEM_WINDOWS_UUS_PATH = Path.Combine(SYSTEM_ROOT_PATH, "UUS");
            SYSTEM_WINDOWS_WAAS_PATH = Path.Combine(SYSTEM_ROOT_PATH, "WaaS");
            SYSTEM_WINDOWS_WINSXS_PATH = Path.Combine(SYSTEM_ROOT_PATH, "WinSxS");
            
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            USER_LOCAL_TEMP_PATH = Path.Combine(userProfile, @"AppData\Local\Temp");
            USER_LOCALLOW_TEMP_PATH = Path.Combine(userProfile, @"AppData\LocalLow\Temp");
            USER_ROAMING_TEMP_PATH = Path.Combine(userProfile, @"AppData\Roaming\Temp");
            USER_DOWNLOADS_PATH = Path.Combine(userProfile, @"Downloads");

            SYSTEM_NOT_RELEVANT_DIRECTORIES = new List<string>
            {
                $@"{SYSTEM_MSO_CACHE_PATH}\".ToLower(),
                $@"{SYSTEM_RECYCLE_BIN_PATH}\".ToLower(),
                $@"{SYSTEM_TEMP_PATH}\".ToLower(),
                $@"{SYSTEM_VOLUME_INFORMATION_PATH}\".ToLower(),

                $@"{SYSTEM_WINDOWS_CSC_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_DEBUG_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_INSTALLER_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_LOGS_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_PREFETCH_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_SOFTWARE_DISTRIBUTION_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_TEMP_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_UUS_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_WAAS_PATH}\".ToLower(),
                $@"{SYSTEM_WINDOWS_WINSXS_PATH}\".ToLower(),
                
                $@"{USER_LOCAL_TEMP_PATH}\".ToLower(),
                $@"{USER_LOCALLOW_TEMP_PATH}\".ToLower(),
                $@"{USER_ROAMING_TEMP_PATH}\".ToLower(),
                $@"{USER_DOWNLOADS_PATH}\".ToLower()
            };
        }

        private static void WriteScanRecord(FileInfo output, string scanRecord)
        {
            lock (output)
                using (var writer = new StreamWriter(output.FullName, true))
                    writer.WriteLine(scanRecord);
        }

        private static String ComputeDateTimeHash(DateTime dateTime)
        {
            return $"{dateTime.Hour % 12:D2}{dateTime.Minute:D2}";
        }

        private static string ComputePathHash(string path)
        {
            var hashString = Math.Abs(path.GetHashCode()).ToString();
            return hashString
                .Substring(0, Math.Min(3, hashString.Length))
                .PadLeft(3, '0');
        }

        internal static void Scan(int depth)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var scanFileSystemOutput = new FileInfo($"{timestamp}-F.scan");
            SYSTEM_NOT_RELEVANT_DIRECTORIES.Add($@"{scanFileSystemOutput.FullName.ToLower()}\");
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (String.IsNullOrEmpty(systemDrive))
                systemDrive = "C:";
            if (!systemDrive.EndsWith(@"\"))
                systemDrive += @"\";
            Messages.Push(Messages.Type.Trace, "Scan file system");
            ScanFileSystem(systemDrive, depth, scanFileSystemOutput);

            // PerformanceData and Users are ignored. PerformanceData should
            // only be used read-only and Users is a real-time copy/reference to
            // CurrentUser that is automatically maintained by Windows.
            var scanRegistryOutput = new FileInfo($"{timestamp}-R.scan");
            Messages.Push(Messages.Type.Trace, "Scan registry HKEY_CLASSES_ROOT");
            using (RegistryKey rootKey = Registry.ClassesRoot)
                ScanRegistry(rootKey, "HKEY_CLASSES_ROOT", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, "Scan registry HKEY_CURRENT_CONFIG");
            using (RegistryKey rootKey = Registry.CurrentConfig)
                ScanRegistry(rootKey, "HKEY_CURRENT_CONFIG", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, "Scan registry HKEY_CURRENT_USER");
            using (RegistryKey rootKey = Registry.CurrentUser)
                ScanRegistry(rootKey, "HKEY_CURRENT_USER", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, "Scan registry HKEY_LOCAL_MACHINE");
            using (RegistryKey rootKey = Registry.LocalMachine)
                ScanRegistry(rootKey, "HKEY_LOCAL_MACHINE", depth, scanRegistryOutput);
            
            Messages.Push(Messages.Type.Trace, "Scan completed");
            
            var compareOutput = new FileInfo($"{timestamp}.compare");
            CompareFileSystemScans(compareOutput);
            CompareRegistryScans(compareOutput);
            if (!File.Exists(compareOutput.FullName))
                return;
            Messages.Push(Messages.Type.Trace, $"Compare output in: {compareOutput.Name}");
            Messages.Push(Messages.Type.Trace, "Compare completed");
        }

        private static IEnumerable<string> ExtractLines(IReadOnlyDictionary<string, FileInfo> dictionary, string hash)
        {
            if (dictionary.ContainsKey(hash))
                return File.ReadAllLines(dictionary[hash].FullName);
            return Array.Empty<string>();
        }

        private static Dictionary<string, FileInfo> ExtractHashes(string file)
        {
            var collector = new Dictionary<string, FileInfo>();
            try
            {
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var hash = line.Split('\t').Last();
                        if (!collector.ContainsKey(hash))
                            collector[hash] = new FileInfo(Path.GetTempFileName());
                        using (var writer = new StreamWriter(collector[hash].FullName, true))
                            writer.WriteLine(line);
                    }
                }
            }
            catch (Exception)
            {
                DeleteFiles(collector.Values.ToArray());
            }
            return collector;
        }
        
        private static void DeleteFiles(IEnumerable<FileInfo> fileInfos)
        {
            foreach (var fileInfo in fileInfos)
                try
                {
                    if (File.Exists(fileInfo.FullName))
                        File.Delete(fileInfo.FullName);
                }
                catch (Exception)
                {
                }
        }

        private static string CollectFileSystemInfos(string path)
        {
            if (path.Length > FILE_SYSTEM_MAX_PATH)
                return path;
            
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return $"{fileInfo.FullName}"
                       + $"\t{fileInfo.LastWriteTime:yyyyMMddHHmmss}"
                       + $"\t{fileInfo.Length}";
            }
            
            if (Directory.Exists(path))
            {
                var collectionBuilder = new StringBuilder();
                Action<string> collectionBuilderAppendLine = value =>
                {
                    lock (collectionBuilder)
                        collectionBuilder.AppendLine(value);
                };
                
                collectionBuilderAppendLine($"{Path.GetFullPath(path)}"
                        + $"\t{Directory.GetLastWriteTime(path):yyyyMMddHHmmss}");
                foreach (var subDirectory in Directory.GetDirectories(path))
                    collectionBuilderAppendLine(subDirectory.Length <= FILE_SYSTEM_MAX_PATH
                        ? CollectFileSystemInfos(subDirectory)
                        : subDirectory);
                foreach (var file in Directory.GetFiles(path))
                    collectionBuilderAppendLine(file.Length <= FILE_SYSTEM_MAX_PATH
                        ? CollectFileSystemInfos(file)
                        : file);
                return collectionBuilder.ToString();
            }
            
            return path;
        }

        private static void ScanFileSystem(string path, int depth, FileInfo output)
        {
            if (SYSTEM_NOT_RELEVANT_DIRECTORIES.Contains($@"{path.ToLower()}\"))
                return;
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                return;
            
            try
            {
                var currentDepth = path.Count(symbol => symbol == '\\');
                if (Directory.Exists(path)
                        && currentDepth >= depth
                        && depth >= 0)
                {
                    var filesystemInfos = CollectFileSystemInfos(path);
                    using (var sha256 = SHA256.Create())
                    {
                        var inputBytes = Encoding.UTF8.GetBytes(filesystemInfos);
                        var hashBytes = sha256.ComputeHash(inputBytes);
                        filesystemInfos = Convert.ToBase64String(hashBytes);
                    }
                    var scanRecord = $"{path}\t{filesystemInfos}\t{ComputeDateTimeHash(Directory.GetLastWriteTime(path))}";
                    WriteScanRecord(output, scanRecord);
                }
                else if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var scanRecord = $"{fileInfo.FullName}"
                            + $"\t{fileInfo.LastWriteTime:yyyyMMddHHmmss}"
                            + $"\t{fileInfo.Length}"
                            + $"\t{ComputeDateTimeHash(fileInfo.LastWriteTime)}";
                    WriteScanRecord(output, scanRecord);
                }
                else if (Directory.Exists(path))
                {
                    var scanRecord = $"{Path.GetFullPath(path)}"
                            + $"\t{Directory.GetLastWriteTime(path):yyyyMMddHHmmss}"
                            + $"\t{ComputeDateTimeHash(Directory.GetLastWriteTime(path))}";
                    WriteScanRecord(output, scanRecord);
                    foreach (var subDirectory in Directory.GetDirectories(path))
                        ScanFileSystem(subDirectory, depth, output);
                    foreach (var file in Directory.GetFiles(path))
                        ScanFileSystem(file, depth, output);
                }
                else WriteScanRecord(output, $"{path}\t0000");
            }
            catch (UnauthorizedAccessException)
            {
                WriteScanRecord(output, $"{path}\t0000");
            }
        }

        private static string CollectRegistryKeys(RegistryKey registryKey, string path)
        {
            if (registryKey == null)
                return path;

            var collectionBuilder = new StringBuilder();
            Action<string> collectionBuilderAppendLine = value =>
            {
                lock (collectionBuilder)
                    collectionBuilder.AppendLine(value);
            };

            var collector = registryKey.GetValueNames()
                .Select(valueName => $"{valueName}:{registryKey.GetValue(valueName)}")
                .ToList();
            collectionBuilderAppendLine(String.Join(";", collector));
            
            foreach (var subKeyName in registryKey.GetSubKeyNames())
                try
                {
                    using (var subKey = registryKey.OpenSubKey(subKeyName))
                        collectionBuilderAppendLine(CollectRegistryKeys(subKey, $@"{path}\{subKeyName}"));
                }
                catch (SecurityException)
                {
                    collectionBuilderAppendLine(path);
                }
            
            return collectionBuilder.ToString();
        }

        private static void ScanRegistry(RegistryKey registryKey, string path, int depth, FileInfo output)
        {
            if (registryKey == null)
                return;
         
            var currentDepth = path.Count(symbol => symbol == '\\') +1;
            if (currentDepth >= depth)
            {
                var registryKeys = CollectRegistryKeys(registryKey, path);
                using (var sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(registryKeys);
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    registryKeys = Convert.ToBase64String(hashBytes);
                }
                var scanRecord = $"{path}\t{registryKeys}\t{ComputePathHash(path)}";
                WriteScanRecord(output, scanRecord);
            }
            else
            {
                var collector = registryKey.GetValueNames()
                    .Select(valueName => $"{valueName}:{registryKey.GetValue(valueName)}")
                    .ToList();
                var hash = String.Join(";", collector);
                using (SHA256 sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(hash);
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    hash = Convert.ToBase64String(hashBytes);
                }
                var scanRecord = $"{path}\t{hash}\t{ComputePathHash(path)}";
                WriteScanRecord(output, scanRecord);
                
                foreach (var subKeyName in registryKey.GetSubKeyNames())
                    try
                    {
                        using (var subKey = registryKey.OpenSubKey(subKeyName))
                            ScanRegistry(subKey, $@"{path}\{subKeyName}", depth, output);
                    }
                    catch (SecurityException)
                    {
                        WriteScanRecord(output, $"{path}\t000");
                    }
            }
        }
        
        private static void CompareFileSystemScans(FileInfo output)
        {
            var scanFilePattern = new Regex($@"^\d{{8}}-\d{{6}}-F\.scan$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var scanFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), $"*.scan")
                .Where(file => scanFilePattern.IsMatch(Path.GetFileName(file)))
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .Take(2)
                .ToList();
            if (scanFiles.Count < 2)
                return;
            Messages.Push(Messages.Type.Trace, "Compare file system with previous scan");
            CompareScans(scanFiles[1], scanFiles[0], output);
        }

        private static void CompareRegistryScans(FileInfo output)
        {
            var scanFilePattern = new Regex($@"^\d{{8}}-\d{{6}}-R\.scan$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var scanFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), $"*.scan")
                .Where(file => scanFilePattern.IsMatch(Path.GetFileName(file)))
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .Take(2)
                .ToList();
            if (scanFiles.Count < 2)
                return;
            Messages.Push(Messages.Type.Trace, "Compare registry with previous scan");
            CompareScans(scanFiles[1], scanFiles[0], output);
        }

        private static void CompareScans(FileInfo previousFile, FileInfo compareFile, FileInfo output)
        {
            var collector = new List<string>();
            var previousHashes = new Dictionary<string, FileInfo>();
            var compareHashes = new Dictionary<string, FileInfo>();
            try
            {
                foreach (var hashEntry in ExtractHashes(previousFile.FullName))
                    previousHashes.Add(hashEntry.Key, hashEntry.Value);
                foreach (var hashEntry in ExtractHashes(compareFile.FullName))
                    compareHashes.Add(hashEntry.Key, hashEntry.Value);
                
                foreach (var hash in compareHashes.Keys.ToList())
                {
                    var previousLines = ExtractLines(previousHashes, hash).ToHashSet();
                    var compareLines = ExtractLines(compareHashes, hash);
                    foreach (var line in compareLines)
                        if (!previousLines.Contains(line))
                            collector.Add(line.Split('\t')[0]);
                    DeleteFiles(new [] {previousHashes[hash]});
                    previousHashes.Remove(hash);
                    DeleteFiles(new [] {compareHashes[hash]});
                    compareHashes.Remove(hash);
                }
            }
            finally
            {
                DeleteFiles(previousHashes.Values);
                DeleteFiles(compareHashes.Values);
            }

            collector.Sort();
            using (var writer = new StreamWriter(output.FullName))
            {
                writer.Write("");
                foreach (var line in collector)
                    writer.WriteLine(line);
            }
        }
    }
}