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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Inventory
{
    internal static class Storage
    {
        // NOTE: HKEY_CURRENT_USER is an alias that refers to the specific user
        // branch in HKEY_USERS. Windows synchronizes both root keys in real
        // time. Therefore changes, incl. creation, modification and deletion of
        // keys, are only required on one of the root keys.

        // NOTE: Registry access and exception handling in relation to
        // asynchronous changes in real time and authorizations. There are many
        // reasons why registry access can fail. In addition, multiple accesses
        // are often required that cannot be atomized. Nevertheless, the errors
        // are not caught because the assumption is that the target application
        // is not active when mirroring and deploying and therefore the
        // resources should not change.  
        
        private const string REGISTRY_DATA_RECORD_REG = "REG";
        private const string REGISTRY_DATA_RECORD_VAL = "VAL";
        private const string REGISTRY_DATA_RECORD_TYP = "TYP";
        private const string REGISTRY_DATA_RECORD_DAT = "DAT";

        private static readonly string REGISTRY_KEY_PATH_PATTERN =
            @"(?:\\((?:[^\x00-\x1F:\\]+)(?:\\[^\x00-\x1F:\\]+)*))?(?::(\w[^\x00-\x1F]*\w))?";
        private static readonly Regex REGISTRY_KEY_PATTERN =
            new Regex(@"^(\w+)" + REGISTRY_KEY_PATH_PATTERN + "$");
        private static readonly Regex REGISTRY_HKCR_KEY_PATTERN =
            new Regex(@"^(HKEY_CLASSES_ROOT|HKCR)" + REGISTRY_KEY_PATH_PATTERN + "$");
        private static readonly Regex REGISTRY_HKCU_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_USER|HKCU)" + REGISTRY_KEY_PATH_PATTERN + "$");
        private static readonly Regex REGISTRY_HKLM_KEY_PATTERN =
            new Regex(@"^(HKEY_LOCAL_MACHINE|HKLM)" + REGISTRY_KEY_PATH_PATTERN + "$");
        private static readonly Regex REGISTRY_HKU_KEY_PATTERN =
            new Regex(@"^(HKEY_USERS|HKU)" + REGISTRY_KEY_PATH_PATTERN + "$");
        private static readonly Regex REGISTRY_HKCC_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_CONFIG|HKCC)" + REGISTRY_KEY_PATH_PATTERN + "$");
        
        private static readonly Regex ENCODING_BASE64_PATTERN =
            new Regex(@"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$");

        private static readonly Regex TEXT_ASCII_7_PATTERN =
            new Regex(@"^[\x20-\x7F]*$");
        
        private static readonly List<string> FILESYSTEM_EXCLUDES;
        private static readonly List<string> REGISTRY_EXCLUDES;
        
        private static string STORAGE => 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        static Storage()
        {
            FILESYSTEM_EXCLUDES = new List<string>
            {
                Paths.SYSTEM_VOLUME_INFORMATION_PATH.ToLower(),
                
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "MSOCache")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "Temp")).ToLower(),

                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "bootstat.dat")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "dumpfile.dmp")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "pagefile.sys")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_DRIVE_PATH, "swapfile.sys")).ToLower(),

                Paths.PathNormalize(Path.Combine(Paths.USER_PROFILE_PATH, @"AppData\Local\Temp")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.USER_PROFILE_PATH, @"AppData\LocalLow\Temp")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.USER_PROFILE_PATH, @"AppData\Roaming\Temp")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.USER_PROFILE_PATH, @"Downloads")).ToLower(),
            
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "CSC")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "Debug")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "Installer")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "Logs")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "Prefetch")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "SoftwareDistribution")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "Temp")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "UUS")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "WaaS")).ToLower(),
                Paths.PathNormalize(Path.Combine(Paths.SYSTEM_ROOT_PATH, "WinSxS")).ToLower()
            };
            
            REGISTRY_EXCLUDES = new List<string>
            {
                (@"HKEY_CLASSES_ROOT\Local Settings\MuiCache").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\TypedURLs").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSaveMRU").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Map Network Drive MRU").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search").ToLower(),
                (@"HKEY_CURRENT_USER\Software\Microsoft\Windows\ShellNoRoam\MUICache").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Telemetry").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers").ToLower(),
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection").ToLower(),
                (@"HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs").ToLower(),
                (@"HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders").ToLower()
            };
        }
                
        private static FileInfo FileSystemNormalizeLocation(string location)
        {
            location = Regex.Replace(location, "%([A-Za-z]:)%", "$1");
            location = Environment.ExpandEnvironmentVariables(location);
            return new FileInfo(location.Trim());
        }

        internal static void MirrorFileSystemLocation(string timestamp, string location, bool recursive = false)
        {
            try
            {
                var storageDirectory = Path.Combine(STORAGE, timestamp);
                if (!Directory.Exists(storageDirectory))
                    Directory.CreateDirectory(storageDirectory);

                var locationFileInfo = FileSystemNormalizeLocation(location);
                var storageFileInfo = new FileInfo(Path.Combine(STORAGE, timestamp, location));
                if (locationFileInfo.FullName.Length > Paths.FILE_SYSTEM_MAX_PATH
                        || storageFileInfo.FullName.Length > Paths.FILE_SYSTEM_MAX_PATH)
                    throw new IOException("Maximum path length exceeded");
                
                var locationFilter = locationFileInfo.FullName.ToLower();
                if (FILESYSTEM_EXCLUDES.Contains(locationFilter)
                        || FILESYSTEM_EXCLUDES.Any(exclude =>
                            locationFilter.StartsWith($@"{exclude}{Path.DirectorySeparatorChar}")))
                    return;
                if (locationFileInfo.FullName.StartsWith($"{Paths.SYSTEM_DRIVE_PATH}$",
                        StringComparison.OrdinalIgnoreCase))
                    return;
                if (File.Exists(locationFileInfo.FullName)
                        && locationFileInfo.Length > 1000 * 1024 * 1024)
                    throw new IOException("Maximum file length exceeded");

                if (Directory.Exists(locationFileInfo.FullName))
                {
                    Directory.CreateDirectory(storageFileInfo.FullName);

                    if (!recursive)
                        return;
                    
                    var directory = new DirectoryInfo(locationFileInfo.FullName);
                    foreach (var file in directory.GetFiles())
                        MirrorFileSystemLocation(timestamp, Paths.PathAbstract(file.FullName));
                    foreach (var subDirectory in directory.GetDirectories())
                        MirrorFileSystemLocation(timestamp, Paths.PathAbstract(subDirectory.FullName), true);
                }
                else if (File.Exists(locationFileInfo.FullName))
                {
                    Directory.CreateDirectory(storageFileInfo.DirectoryName);
                    File.Copy(locationFileInfo.FullName, storageFileInfo.FullName, overwrite: true);
                }
            }
            catch (Exception)
            {
                Messages.Push(Messages.Type.Warning,
                    "Mirror file system failed",
                    FileSystemNormalizeLocation(location).FullName);
            }
        }

        private static void MirrorRegistryKeyEntry(string timestamp, RegistryKey registryKey, string valueName = "")
        {
            Func<object, RegistryValueKind?, string> FormatRegistryKeyValue = (value, type) =>
            {
                if (RegistryValueKind.None == type
                        || type is null
                        || value is null)
                    return null;
                    
                switch (type)
                {
                    case RegistryValueKind.DWord:
                        return Convert.ToInt32(value).ToString();
                    case RegistryValueKind.QWord:
                        return Convert.ToInt64(value).ToString();
                    case RegistryValueKind.Binary:
                        return Convert.ToBase64String((byte[])value);
                    case RegistryValueKind.MultiString:
                        var block = String.Join(Environment.NewLine, (string[])value);
                        return Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(block));
                    case RegistryValueKind.String:
                    default:
                        var text = Convert.ToString(value);
                        if (String.IsNullOrEmpty(text))
                            return "";
                        if (ENCODING_BASE64_PATTERN.IsMatch(text))
                            return Convert.ToBase64String(
                                Encoding.UTF8.GetBytes(text));
                        if (TEXT_ASCII_7_PATTERN.IsMatch(text))
                            return text;
                        return Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(text));
                }                
            };

            var valueValue = registryKey.GetValue(valueName);
            var valueType = valueValue is null
                    ? (RegistryValueKind?)null
                    : registryKey.GetValueKind(valueName);
                     
            var payload = $"{REGISTRY_DATA_RECORD_REG}:{registryKey}{Environment.NewLine}";
            if (!String.IsNullOrEmpty(valueName))
                payload += $"{REGISTRY_DATA_RECORD_VAL}:{valueName}{Environment.NewLine}";
            if (!(valueType is null))
                payload += $"{REGISTRY_DATA_RECORD_TYP}:{valueType}{Environment.NewLine}";
            valueValue = FormatRegistryKeyValue(valueValue, valueType);
            if (!String.IsNullOrEmpty((string)valueValue))
                payload += $"{REGISTRY_DATA_RECORD_DAT}:{valueValue}{Environment.NewLine}";
            payload += Environment.NewLine;

            var registryRootKey = Regex.Replace(registryKey.Name, @"\\.*$", "");
            var output = Path.Combine(STORAGE, timestamp, registryRootKey);
            File.AppendAllText(output, payload);
        }

        internal static void MirrorRegistryKey(string timestamp, string registryKey, bool recursive = false)
        {
            try
            {
                Func<string, string, string> FormatMessage = (message, value) =>
                    String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

                Func<string, int, string> RegistryKeyPatternMatchGroup = (record, group) =>
                {
                    var match = REGISTRY_KEY_PATTERN.Match(record);
                    if (!match.Success
                            || !match.Groups[group].Success)
                        return "";
                    return match.Groups[group].Value.Trim();
                };
                
                var storageDirectory = Path.Combine(STORAGE, timestamp);
                if (!Directory.Exists(storageDirectory))
                    Directory.CreateDirectory(storageDirectory);
                
                var registrySubKeyValueName = RegistryKeyPatternMatchGroup(registryKey, 3);
                var registrySubKeyPath = RegistryKeyPatternMatchGroup(registryKey, 2);
                if (String.IsNullOrWhiteSpace(registrySubKeyPath))
                    return;

                RegistryKey registryRootKey;
                if (REGISTRY_HKCR_KEY_PATTERN.IsMatch(registryKey))
                    registryRootKey = Registry.ClassesRoot;
                else if (REGISTRY_HKCU_KEY_PATTERN.IsMatch(registryKey))
                    registryRootKey = Registry.CurrentUser;
                else if (REGISTRY_HKLM_KEY_PATTERN.IsMatch(registryKey))
                    registryRootKey = Registry.LocalMachine;
                else if (REGISTRY_HKU_KEY_PATTERN.IsMatch(registryKey))
                    registryRootKey = Registry.Users;
                else if (REGISTRY_HKCC_KEY_PATTERN.IsMatch(registryKey))
                    registryRootKey = Registry.CurrentConfig;
                else return;

                var registrySubKeyPathFilter = ($@"{registryRootKey.Name}\{registrySubKeyPath}").ToLower();
                if (REGISTRY_EXCLUDES.Contains(registrySubKeyPathFilter)
                        || REGISTRY_EXCLUDES.Any(exclude =>
                            registrySubKeyPathFilter.StartsWith($@"{exclude}\")))
                    return;

                using (var registrySubKey = registryRootKey.OpenSubKey(registrySubKeyPath))
                {
                    if (registrySubKey is null)
                        return;

                    MirrorRegistryKeyEntry(timestamp, registrySubKey, registrySubKeyValueName);
                    if (registryKey.Contains(":")
                            || !recursive)
                        return;

                    registrySubKey.GetValueNames()
                        .Where(subKeyValueName => !String.IsNullOrWhiteSpace(subKeyValueName))
                        .ToList()
                        .ForEach(subKeyValueName =>
                            MirrorRegistryKeyEntry(timestamp, registrySubKey, subKeyValueName));

                    registrySubKey.GetSubKeyNames()
                        .Where(subKeyName => !String.IsNullOrWhiteSpace(subKeyName))
                        .Select(subKeyName =>
                        {
                            using (var subKey = registrySubKey.OpenSubKey(subKeyName))
                                return subKey != null ? $@"{registryKey}\{subKeyName}" : null;
                        })
                        .Where(subKeyName => !String.IsNullOrWhiteSpace(subKeyName))
                        .ToList()
                        .ForEach(subKeyName =>
                            MirrorRegistryKey(timestamp, subKeyName, true));
                }
            }
            catch (Exception)
            {
                Messages.Push(Messages.Type.Warning,
                    "Mirror registry failed",
                    registryKey);
            }
        }
    }
}