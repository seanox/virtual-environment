// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Launcher
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
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Startup
{
    internal class DataStore
    {
        private const string REGISTRY_DATA = "registry.data";

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

        private static readonly Regex REGISTRY_KEY_PATTERN =
            new Regex(@"^(\w+)((?:\\[\s\w-]+)*)$");
        private static readonly Regex REGISTRY_HKCR_KEY_PATTERN =
            new Regex(@"^(HKEY_CLASSES_ROOT|HKCR)(\\[\s\w-]+)*$");
        private static readonly Regex REGISTRY_HKCU_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_USER|HKCU)(\\[\s\w-]+)*$");
        private static readonly Regex REGISTRY_HKLM_KEY_PATTERN =
            new Regex(@"^(HKEY_LOCAL_MACHINE|HKLM)(\\[\s\w-]+)*$");
        private static readonly Regex REGISTRY_HKU_KEY_PATTERN =
            new Regex(@"^(HKEY_USERS|HKU)(\\[\s\w-]+)*$");
        private static readonly Regex REGISTRY_HKCC_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_CONFIG|HKCC)(\\[\s\w-]+)*$");

        private readonly string _datastore;  
        private readonly string[] _registry;  
        private readonly string[] _settings;  

        internal DataStore(Manifest manifest)
        {
            _registry = manifest.Registry?.ToArray() ?? Array.Empty<string>();
            _settings = manifest.Settings?.ToArray() ?? Array.Empty<string>();
            _datastore = NormalizeValue(manifest.DataStore);
            if (String.IsNullOrWhiteSpace(_datastore))
                _datastore = ".";
            _datastore = Path.GetFullPath(_datastore);
        }
        
        private static string NormalizeValue(string value)
        {
            return Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }
        
        internal void CreateMirrorDirectory()
        {
            if (!Directory.Exists(_datastore))
                Directory.CreateDirectory(_datastore);
        }

        internal void DeleteExistingRegistry()
        {
        }

        internal void DeleteExistingSettings()
        {
        }

        internal void RestoreRegistry()
        {
        }

        internal void RestoreSettings()
        {
        }
        
        private static T RegistryAccess<T>(Func<T> registryAction, T defaultValue = default)
        {
            try
            {
                return registryAction();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        private static void MirrorRegistryKey(string destination, string registryKey)
        {
            Func<string, string, string> formatMessage = (message, value) =>
                String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

            Func<string, string, string> formatRegistryKeyValueName = (key, name) =>
                String.IsNullOrEmpty(name) ? key : $@"{key}::{name}";

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
                throw new DataException(formatMessage("Unexpected registry key", registryKeyNormal)) ;
            registrySubKeyNormal = Regex.Replace(registrySubKeyNormal, @"^\\+", "");
            if (String.IsNullOrEmpty(registrySubKeyNormal))
                throw new DataException(formatMessage("Unexpected empty registry key", registryKey)) ;
            
            var registrySubKey = RegistryAccess(() => registryRootKey.OpenSubKey(registrySubKeyNormal));
            if (registrySubKey == null)
                return;
            
            foreach (var valueName in RegistryAccess(() => registrySubKey.GetValueNames(), Array.Empty<string>()))
            {
                try
                {
                    var value = registrySubKey.GetValue(valueName);
                    var type = registrySubKey.GetValueKind(valueName);
                    File.AppendAllText(destination,
                        $"{formatRegistryKeyValueName(registryKey, valueName)} :: {type}{Environment.NewLine}"
                            + $"{value}{Environment.NewLine}"
                            + $"{Environment.NewLine}");
                }
                catch (Exception)
                {
                    // Asynchronous changes in real time and authorizations.
                    // There are many reasons why access can fail. In addition,
                    // there are two calls that cannot be atomized. 
                }
            }
            
            foreach (var subKeyName in RegistryAccess(() =>
                         registrySubKey.GetSubKeyNames(), Array.Empty<string>()))
            {
                var subKey = RegistryAccess(() => registrySubKey.OpenSubKey(subKeyName));
                if (subKey != null)
                    MirrorRegistryKey(destination, $@"{registryKey}\{subKeyName}");
            }
        }

        internal void MirrorMissingRegistry()
        {
            // NOTE: HKEY_CURRENT_USER is an alias that refers to the specific
            // user branch in HKEY_USERS. Windows synchronizes both root keys in
            // real time. Therefore changes, including creation, modification
            // and deletion of keys, are only required on one of the root keys.
            
            if (_registry == null)
                return;
            var destination = Path.Combine(_datastore, REGISTRY_DATA);
            var registryKeys = new List<string>();
            if (File.Exists(destination))
            {
                var registryMirrorContent = File.ReadAllText(destination);
                registryMirrorContent = Regex.Replace(registryMirrorContent, @"[\r\n]+", "\n");
                registryKeys.AddRange(_registry.Where(registryKey =>
                    !Regex.IsMatch(registryMirrorContent,
                        $@"^{Regex.Escape(registryKey)}(\\[\s\w-]+)*(::[^\x00-\x1F\\]+)*\s::\s[a-zA-Z]+\s*$",
                        RegexOptions.Multiline)));
            } else registryKeys.AddRange(_registry);
            foreach (var registryKey in registryKeys.ToArray())
                MirrorRegistryKey(destination, registryKey);
        }
        
        internal void MirrorRegistry()
        {
            // NOTE: HKEY_CURRENT_USER is an alias that refers to the specific
            // user branch in HKEY_USERS. Windows synchronizes both root keys in
            // real time. Therefore changes, including creation, modification
            // and deletion of keys, are only required on one of the root keys.

            if (_registry == null)
                return;
            var destination = Path.Combine(_datastore, REGISTRY_DATA);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var tempDestination = $"{destination}-{timestamp}";
            if (File.Exists(tempDestination))
                File.Delete(tempDestination);
            foreach (var registryKey in _registry)
                MirrorRegistryKey(tempDestination, registryKey);
            if (!File.Exists(tempDestination))
                return;
            if (File.Exists(destination))
                File.Delete(destination);
            File.Move(tempDestination, destination);
        }

        private static string NormalizePath(string path)
        {
            var directorySeparator = Path.DirectorySeparatorChar.ToString();
            path = Regex.Replace(path, "[\\/]+", directorySeparator).Trim();

            var queue = new Queue<string>();
            foreach (var part in path.Split(Path.DirectorySeparatorChar))
            {
                if (part == "..")
                     if (queue.Count > 0)
                         queue.Dequeue();
                 if (!String.IsNullOrWhiteSpace(part)
                         && part != ".")
                     queue.Enqueue(part);
            }

            return String.Join(Path.DirectorySeparatorChar.ToString(), queue);
        }

        private void MirrorSettingsLocation(string location)
        {
            location = NormalizePath(location);
            if (location.Length > FILE_SYSTEM_MAX_PATH
                    || location.Length + _datastore.Length > FILE_SYSTEM_MAX_PATH)
                return;
            
            if (Regex.IsMatch(location, @"^[a-zA-Z]:\\"))
                location = location.Substring(3);
            var destination = Path.GetFullPath(Path.Combine(_datastore, location));
            var locationNormal = NormalizeValue(location);
            
            // References to the data store / mirror directory are excluded in
            // order to avoid possible recursion issues.
            var canonicalLocation = Path.GetFullPath(locationNormal);
            canonicalLocation = canonicalLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalLocation += Path.DirectorySeparatorChar;
            var canonicalDataStore = _datastore.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalDataStore += Path.DirectorySeparatorChar;
            if (canonicalLocation.StartsWith(canonicalDataStore,StringComparison.OrdinalIgnoreCase))
                return;
            
            if (Directory.Exists(locationNormal))
            {
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);
                foreach (var file in Directory.GetFiles(locationNormal))
                    MirrorSettingsLocation(Path.Combine(location, Path.GetFileName(file)));
                foreach (var subDirectory in Directory.GetDirectories(locationNormal))
                    MirrorSettingsLocation(Path.Combine(location, Path.GetFileName(subDirectory)));
            } else if (File.Exists(locationNormal))
                File.Copy(locationNormal, destination, overwrite: true);
        }
        
        internal void MirrorMissingSettings()
        {
            if (_settings == null)
                return;
            var locations = new List<string>();
            foreach (var location in _settings)
            {
                var mirrorLocation= Path.Combine(_datastore, location.Substring(3));
                if (!File.Exists(mirrorLocation)
                        && !Directory.Exists(mirrorLocation))
                    locations.Add(location);
            }
            foreach (var location in locations.ToArray())
                MirrorSettingsLocation(location);
        }

        internal void MirrorSettings()
        {
            if (_settings == null)
                return;
            foreach (var location in _settings)
                MirrorSettingsLocation(location);
        }
    }
    
    internal class DataStoreException : Exception
    {
        internal DataStoreException(string message) : base(message)
        {
        }
    }
}