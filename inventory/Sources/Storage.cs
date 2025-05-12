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
using System.Runtime.InteropServices;
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

        private static readonly Regex REGISTRY_KEY_PATTERN =
            new Regex(@"^(\w+)((?:\\[\s\w-]+)*)(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKCR_KEY_PATTERN =
            new Regex(@"^(HKEY_CLASSES_ROOT|HKCR)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKCU_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_USER|HKCU)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKLM_KEY_PATTERN =
            new Regex(@"^(HKEY_LOCAL_MACHINE|HKLM)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKU_KEY_PATTERN =
            new Regex(@"^(HKEY_USERS|HKU)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKCC_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_CONFIG|HKCC)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        
        private static readonly Regex ENCODING_BASE64_PATTERN =
            new Regex(@"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$");

        private static string STORAGE => 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        private static FileInfo FileSystemNormalizeLocation(string location)
        {
            location = Regex.Replace(location, "%%([A-Z])%", "$1:");
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
                    throw new IOException();

                if (recursive)
                {
                    if (!Directory.Exists(locationFileInfo.FullName))
                        return;
                    var directory = new DirectoryInfo(locationFileInfo.FullName);
                    foreach (var file in directory.GetFiles())
                        MirrorFileSystemLocation(timestamp, Paths.PathAbstract(file.FullName));
                    foreach (var subDirectory in directory.GetDirectories())
                        MirrorFileSystemLocation(timestamp, Paths.PathAbstract(subDirectory.FullName), true);
                    return;
                }
                
                if (Directory.Exists(locationFileInfo.FullName))
                {
                    Directory.CreateDirectory(storageFileInfo.FullName);
                }
                else if (File.Exists(locationFileInfo.FullName))
                {
                    Directory.CreateDirectory(storageFileInfo.DirectoryName);
                    File.Copy(locationFileInfo.FullName, storageFileInfo.FullName, overwrite: true);
                }
            }
            catch (Exception exception)
            {
                Messages.Push(Messages.Type.Warning,
                    "Mirror file system failed",
                    FileSystemNormalizeLocation(location).FullName);
            }
        }
        
        private static string NormalizeValue(string value)
        {
            return Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }

        internal static void MirrorRegistryKey(string timestamp, string registryKey, bool recursive = false)
        {
            try
            {
                Func<string, string, string> FormatMessage = (message, value) =>
                    String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

                Func<object, RegistryValueKind, string> FormatRegistryKeyValue = (value, type) =>
                {
                    if (RegistryValueKind.None == type
                            || value is null)
                        return "";
                    
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
                            var text = (string)value;
                            if (String.IsNullOrEmpty(text))
                                return "";
                            if (ENCODING_BASE64_PATTERN.IsMatch(text))
                                return Convert.ToBase64String(
                                    Encoding.UTF8.GetBytes(text));
                            return text;
                        default:
                            return value.ToString();
                    }                
                };

                var storageDirectory = Path.Combine(STORAGE, timestamp);
                if (!Directory.Exists(storageDirectory))
                    Directory.CreateDirectory(storageDirectory);
                
                var registryKeyValueName = REGISTRY_KEY_PATTERN.Match(registryKey).Groups[3].Value;
                var registryKeyValueNameNormal = NormalizeValue(registryKeyValueName);
                if (!String.IsNullOrWhiteSpace(registryKeyValueName)
                        && String.IsNullOrWhiteSpace(registryKeyValueNameNormal))
                    throw new Exception(FormatMessage("Unexpected empty registry key value name", registryKey)) ;
                if (!String.IsNullOrWhiteSpace(registryKeyValueNameNormal))
                    registryKey = Regex.Replace(registryKey, @":.*$", ""); 
                
                var registryKeyNormal = NormalizeValue(registryKey);

                RegistryKey registryRootKey;
                if (REGISTRY_HKCR_KEY_PATTERN.IsMatch(registryKeyNormal))
                    registryRootKey = Registry.ClassesRoot;
                else if (REGISTRY_HKCU_KEY_PATTERN.IsMatch(registryKeyNormal))
                    registryRootKey = Registry.CurrentUser;
                else if (REGISTRY_HKLM_KEY_PATTERN.IsMatch(registryKeyNormal))
                    registryRootKey = Registry.LocalMachine;
                else if (REGISTRY_HKU_KEY_PATTERN.IsMatch(registryKeyNormal))
                    registryRootKey = Registry.Users;
                else if (REGISTRY_HKCC_KEY_PATTERN.IsMatch(registryKeyNormal))
                    registryRootKey = Registry.CurrentConfig;
                else return;
                
                var registrySubKeyNormal = REGISTRY_KEY_PATTERN.Match(registryKeyNormal).Groups[2].Value;
                if (String.IsNullOrEmpty(registrySubKeyNormal))
                    throw new Exception(FormatMessage("Unexpected registry key", registryKeyNormal)) ;
                registrySubKeyNormal = Regex.Replace(registrySubKeyNormal, @"^\\+", "");
                if (String.IsNullOrEmpty(registrySubKeyNormal))
                    throw new Exception(FormatMessage("Unexpected empty registry key", registryKey)) ;

                using (var registrySubKey = registryRootKey.OpenSubKey(registrySubKeyNormal))
                {
                    if (registrySubKey is null)
                        return;
                    
                    foreach (var valueName in registrySubKey.GetValueNames())
                    {
                        if (!String.IsNullOrWhiteSpace(registryKeyValueNameNormal)
                                && valueName != registryKeyValueNameNormal)
                            continue;
                        
                        var valueType = registrySubKey.GetValueKind(valueName);
                        var valueValue = FormatRegistryKeyValue(registrySubKey.GetValue(valueName), valueType);
                     
                        var payload = $"{REGISTRY_DATA_RECORD_REG}:{registryKey}{Environment.NewLine}";
                        if (!String.IsNullOrEmpty(valueName))
                            payload += $"{REGISTRY_DATA_RECORD_VAL}:{valueName}{Environment.NewLine}";
                        if (valueType != null)
                            payload += $"{REGISTRY_DATA_RECORD_TYP}:{valueType}{Environment.NewLine}";
                        if (!String.IsNullOrEmpty(valueValue))
                            payload += $"{REGISTRY_DATA_RECORD_DAT}:{valueValue}{Environment.NewLine}";
                        payload += Environment.NewLine;

                        var output = Path.Combine(STORAGE, timestamp, $"{registryRootKey.Name}");
                        File.AppendAllText(output, payload);
                    }
                
                    foreach (var subKeyName in registrySubKey.GetSubKeyNames())
                        using (var subKey = registrySubKey.OpenSubKey(subKeyName))
                            if (subKey != null)
                                MirrorRegistryKey(timestamp, $@"{registryKey}\{subKeyName}");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}