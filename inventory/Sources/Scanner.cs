// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Inventory
// Scans and extracts changes in the file system and registry.
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

namespace VirtualEnvironment.Inventory
{
    internal static class Scanner
    {
        // Parallel does not bring any advantages here, as the scan and compare
        // functions are not CPU-intensive enough
        
        private static void WriteScanRecord(FileInfo output, string scanRecord)
        {
            lock (output)
                using (var writer = new StreamWriter(output.FullName, true))
                    writer.WriteLine(scanRecord);
        }

        private static string ComputeDateTimeHash(DateTime dateTime)
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

        internal static FileInfo Scan(int depth)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            
            var scanFileSystemOutput = new FileInfo($"{timestamp}-F.scan");
            Messages.Push(Messages.Type.Trace, "Scan file system");
            ScanFileSystem(Paths.SYSTEM_DRIVE_PATH, depth, scanFileSystemOutput);

            // PerformanceData and Users are ignored. PerformanceData should
            // only be used read-only and Users is a real-time copy/reference to
            // CurrentUser that is automatically maintained by Windows.
            var scanRegistryOutput = new FileInfo($"{timestamp}-R.scan");
            Messages.Push(Messages.Type.Trace, $"Scan registry {Registry.ClassesRoot.Name}");
            using (RegistryKey rootKey = Registry.ClassesRoot)
                ScanRegistry(rootKey, "HKEY_CLASSES_ROOT", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, $"Scan registry {Registry.CurrentConfig.Name}");
            using (RegistryKey rootKey = Registry.CurrentConfig)
                ScanRegistry(rootKey, "HKEY_CURRENT_CONFIG", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, $"Scan registry {Registry.CurrentUser.Name}");
            using (RegistryKey rootKey = Registry.CurrentUser)
                ScanRegistry(rootKey, "HKEY_CURRENT_USER", depth, scanRegistryOutput);
            Messages.Push(Messages.Type.Trace, $"Scan registry {Registry.LocalMachine.Name}");
            using (RegistryKey rootKey = Registry.LocalMachine)
                ScanRegistry(rootKey, "HKEY_LOCAL_MACHINE", depth, scanRegistryOutput);
            
            Messages.Push(Messages.Type.Trace, "Scan completed");
            
            var compareOutput = new FileInfo($"{timestamp}.compare");
            CompareFileSystemScans(compareOutput);
            CompareRegistryScans(compareOutput);
            if (!File.Exists(compareOutput.FullName))
                return null;
            Messages.Push(Messages.Type.Trace, $"Compare output in: {compareOutput.Name}");
            Messages.Push(Messages.Type.Trace, "Compare completed");
            return compareOutput;
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
                    while (!((line = reader.ReadLine()) is null))
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
            if (path.Length > Paths.FILE_SYSTEM_MAX_PATH)
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
                StringBuilderAppendLineSynchronized(collectionBuilder,
                    $"{Path.GetFullPath(path)}"
                    + $"\t{Directory.GetLastWriteTime(path):yyyyMMddHHmmss}");
                foreach (var subDirectory in Directory.GetDirectories(path))
                    StringBuilderAppendLineSynchronized(collectionBuilder, subDirectory.Length <= Paths.FILE_SYSTEM_MAX_PATH
                        ? CollectFileSystemInfos(subDirectory)
                        : subDirectory);
                foreach (var file in Directory.GetFiles(path))
                    StringBuilderAppendLineSynchronized(collectionBuilder, file.Length <= Paths.FILE_SYSTEM_MAX_PATH
                        ? CollectFileSystemInfos(file)
                        : file);
                return collectionBuilder.ToString();
            }
            
            return path;
        }

        private static void ScanFileSystem(string path, int depth, FileInfo output)
        {
            try
            {
                if (String.Equals(output.FullName, Paths.PathNormalize(path), StringComparison.OrdinalIgnoreCase)
                        || File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                    return;

                var currentDepth = path.Count(symbol => symbol == Path.DirectorySeparatorChar);
                if (Directory.Exists(path)
                        && currentDepth > depth
                        && depth >= 0)
                {
                    var filesystemInfos = CollectFileSystemInfos(path);
                    using (var sha256 = SHA256.Create())
                    {
                        var inputBytes = Encoding.UTF8.GetBytes(filesystemInfos);
                        var hashBytes = sha256.ComputeHash(inputBytes);
                        filesystemInfos = Convert.ToBase64String(hashBytes);
                    }
                    var scanRecord = $"{Paths.PathAbstract(path)}"
                            + $"\t{filesystemInfos}"
                            + $"\t{ComputeDateTimeHash(Directory.GetLastWriteTime(path))}";
                    WriteScanRecord(output, scanRecord);
                }
                else if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    var scanRecord = $"{Paths.PathAbstract(fileInfo.FullName)}"
                            + $"\t{fileInfo.LastWriteTime:yyyyMMddHHmmss}"
                            + $"\t{fileInfo.Length}"
                            + $"\t{ComputeDateTimeHash(fileInfo.LastWriteTime)}";
                    WriteScanRecord(output, scanRecord);
                }
                else if (Directory.Exists(path))
                {
                    var scanRecord = $"{Paths.PathAbstract(Path.GetFullPath(path))}"
                            + $"\t{Directory.GetLastWriteTime(path):yyyyMMddHHmmss}"
                            + $"\t{ComputeDateTimeHash(Directory.GetLastWriteTime(path))}";
                    WriteScanRecord(output, scanRecord);
                    foreach (var subDirectory in Directory.GetDirectories(path))
                        ScanFileSystem(subDirectory, depth, output);
                    foreach (var file in Directory.GetFiles(path))
                        ScanFileSystem(file, depth, output);
                }
                else WriteScanRecord(output, $"{Paths.PathAbstract(path)}\t0000");
            }
            catch (Exception exception)
            {
                if (!(exception is UnauthorizedAccessException)
                        && !(exception is FileNotFoundException)
                        && !(exception is DirectoryNotFoundException))
                    throw exception;
                WriteScanRecord(output, $"{Paths.PathAbstract(path)}\t0000");
            }
        }
        
        private static void StringBuilderAppendLineSynchronized(StringBuilder stringBuilder, string line)
        {
            lock (stringBuilder)
                stringBuilder.AppendLine(line);
        }

        private static string CollectRegistryKeys(RegistryKey registryKey, string path)
        {
            if (registryKey is null)
                return path;

            var collectionBuilder = new StringBuilder();
            StringBuilderAppendLineSynchronized(collectionBuilder, $":{ComputeRegistryKeyHash(registryKey)}");

            foreach (var valueName in registryKey.GetValueNames())
                StringBuilderAppendLineSynchronized(collectionBuilder,
                    $"{valueName}:{ComputeRegistryKeyHash(registryKey, valueName)}");
            
            foreach (var subKeyName in registryKey.GetSubKeyNames())
                try
                {
                    using (var subKey = registryKey.OpenSubKey(subKeyName))
                        StringBuilderAppendLineSynchronized(collectionBuilder, CollectRegistryKeys(subKey, $@"{path}\{subKeyName}"));
                }
                catch (SecurityException)
                {
                    StringBuilderAppendLineSynchronized(collectionBuilder, path);
                }
            
            return collectionBuilder.ToString();
        }

        private static string ComputeRegistryKeyHash(RegistryKey registryKey, string valueName = "")
        {
            var registryKeyValue = registryKey.GetValue(valueName);
            var registryKeyHash = !(registryKeyValue is null)
                ? $"{registryKeyValue.GetType().Name}\t{registryKeyValue}"
                : $"{null}\t{null}";
            using (SHA256 sha256 = SHA256.Create())
                return Convert.ToBase64String(
                    sha256.ComputeHash(
                        Encoding.UTF8.GetBytes(registryKeyHash)));
        }

        private static void ScanRegistry(RegistryKey registryKey, string path, int depth, FileInfo output)
        {
            if (registryKey is null)
                return;
            
            var currentDepth = path.Count(symbol => symbol == Path.DirectorySeparatorChar);
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
                var scanRecordHash = ComputeRegistryKeyHash(registryKey);
                var scanRecord = $"{path}\t{scanRecordHash}\t{ComputePathHash(path)}";
                WriteScanRecord(output, scanRecord);

                foreach (var valueName in registryKey.GetValueNames())
                {
                    if (String.IsNullOrWhiteSpace(valueName))
                        continue;
                    scanRecordHash = ComputeRegistryKeyHash(registryKey, valueName);
                    scanRecord = $"{path}:{valueName}\t{scanRecordHash}\t{ComputePathHash(path)}";
                    WriteScanRecord(output, scanRecord);
                }
                        
                foreach (var subKeyName in registryKey.GetSubKeyNames())
                    try
                    {
                        using (var subKey = registryKey.OpenSubKey(subKeyName))
                            ScanRegistry(subKey, $@"{path}\{subKeyName}", depth, output);
                    }
                    catch (SecurityException)
                    {
                        WriteScanRecord(output, $"{path}\\{subKeyName}\t000");
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
                    if (previousHashes.ContainsKey(hash))
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
            
            if (collector.Count <= 0)
                return;
            collector.Sort();
            using (var writer = new StreamWriter(output.FullName, true))
            {
                if (new FileInfo(output.FullName).Length > 0)
                    writer.WriteLine("");
                foreach (var line in collector)
                    writer.WriteLine(line);
            }
        }
    }
}