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
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Startup
{
    internal class Datastore
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            int dwFlags
        );
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            [Out] System.Text.StringBuilder lpBuffer,
            int nSize,
            IntPtr Arguments);
        
        private static string GetErrorMessage(int code)
        {
            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            const int LANG_ENGLISH = 0x09;
            const int SUBLANG_ENGLISH_US = 0x01;
            const int BUFFER_SIZE = 512;
            
            Func<int, int, int> makeLanguageId = (primaryLanguage, subLanguage) => 
                ((subLanguage << 10) | primaryLanguage);
            
            var buffer = new StringBuilder(BUFFER_SIZE);
            var result = FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                code,
                makeLanguageId(LANG_ENGLISH, SUBLANG_ENGLISH_US),
                buffer,
                buffer.Capacity,
                IntPtr.Zero
            );
            
            if (result > 0)
                return buffer.ToString();
            return null;
        }
        
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
        
        private static readonly Regex ENCODING_BASE64_PATTERN =
            new Regex(@"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$");

        private readonly string _datastore;  
        private readonly string[] _registry;  
        private readonly string[] _settings;  

        internal Datastore(Manifest manifest)
        {
            _registry = manifest.Registry?.ToArray() ?? Array.Empty<string>();
            _settings = manifest.Settings?.ToArray() ?? Array.Empty<string>();
            _datastore = NormalizeValue(manifest.Datastore);
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
            if (_registry == null)
                return;
            foreach (var registryKey in _registry)
            {
                Func<string, string, string> formatMessage = (message, value) =>
                    String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

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
                else continue;

                var registrySubKeyNormal = REGISTRY_KEY_PATTERN.Match(registryKeyNormal).Groups[2].Value;
                if (String.IsNullOrEmpty(registrySubKeyNormal))
                    throw new DataException(formatMessage("Unexpected registry key", registryKeyNormal)) ;
                registrySubKeyNormal = Regex.Replace(registrySubKeyNormal, @"^\\+", "");
                if (String.IsNullOrEmpty(registrySubKeyNormal))
                    throw new DataException(formatMessage("Unexpected empty registry key", registryKey)) ;

                using (var registrySubKey = registryRootKey.OpenSubKey(registrySubKeyNormal))
                    if (registrySubKey == null)
                        continue;
                using (var registryBaseKey = registryRootKey.OpenSubKey("", true))
                    if (registryBaseKey != null)
                        registryBaseKey.DeleteSubKeyTree(registrySubKeyNormal);
            }
        }

        internal void DeleteExistingSettings()
        {
            if (_settings == null)
                return;
            foreach (var location in _settings)
            {
                var locationNormal = NormalizeValue(location);
                if (File.Exists(locationNormal))
                    File.Delete(locationNormal);
                if (Directory.Exists(locationNormal))
                    Directory.Delete(locationNormal, true);
            }
        }

        internal void RestoreRegistry()
        {
            var location = Path.Combine(_datastore, REGISTRY_DATA);
            if (File.Exists(location))
            {
                Func<string, string, string> formatMessage = (message, value) =>
                    String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

                Func<char, Queue<string>, string> fetchQueueEntry = (assignment, queue) =>
                    assignment == '1' && queue.Count > 0 ? queue.Dequeue() : "";

                Func<string, RegistryValueKind, object> convertRegistryKeyValue = (value, type) =>
                {
                    if (RegistryValueKind.DWord == type)
                        return Convert.ToInt32(value);
                    if (RegistryValueKind.QWord == type)
                        return Convert.ToInt64(value);
                    if (RegistryValueKind.Binary == type)
                        return Convert.FromBase64String(value);
                    if (RegistryValueKind.MultiString != type)
                        return value;
                    return "";
                };

                var registryMirrorContent = File.ReadAllText(location).Trim();
                var registryMirrorParts = Regex.Split(registryMirrorContent, @"(?:\r\n){2}|(?:\n\r){2}|(?:\r){2}|(?:\n){2}");
                foreach (var part in registryMirrorParts)
                {
                    var lines = new Queue<string>(
                        part.Split(new[] { "\r\n", "\n", "\r" },
                            StringSplitOptions.None));
                    if (lines.Count <= 0)
                        continue;
                    var assignment  = lines.ToArray()[lines.Count -1];
                    if (!Regex.IsMatch(assignment, "^[01]{4}$"))
                        continue;
                    var registryKeyPath = fetchQueueEntry(assignment[0], lines);
                    var registryKeyValueName = fetchQueueEntry(assignment[1], lines);
                    var registryKeyValueTypeText = fetchQueueEntry(assignment[2], lines);
                    var registryKeyValue = fetchQueueEntry(assignment[3], lines);

                    var registryKeyValueType = RegistryValueKind.None;
                    if (!String.IsNullOrWhiteSpace(registryKeyValueTypeText))
                        registryKeyValueType = (RegistryValueKind)Enum.Parse(
                            typeof(RegistryValueKind), registryKeyValueTypeText, ignoreCase: true);

                    var registryKeyPathNormal = NormalizeValue(registryKeyPath);
                    
                    RegistryKey registryRootKey;
                    if (REGISTRY_HKCR_KEY_PATTERN.IsMatch(registryKeyPathNormal))
                        registryRootKey = Registry.ClassesRoot;
                    else if (REGISTRY_HKCU_KEY_PATTERN.IsMatch(registryKeyPathNormal))
                        registryRootKey = Registry.CurrentUser;
                    else if (REGISTRY_HKLM_KEY_PATTERN.IsMatch(registryKeyPathNormal))
                        registryRootKey = Registry.LocalMachine;
                    else if (REGISTRY_HKU_KEY_PATTERN.IsMatch(registryKeyPathNormal))
                        registryRootKey = Registry.Users;
                    else if (REGISTRY_HKCC_KEY_PATTERN.IsMatch(registryKeyPathNormal))
                        registryRootKey = Registry.CurrentConfig;
                    else return;

                    var registrySubKeyNormal = REGISTRY_KEY_PATTERN.Match(registryKeyPathNormal).Groups[2].Value;
                    if (String.IsNullOrEmpty(registrySubKeyNormal))
                        throw new DataException(formatMessage("Unexpected registry key", registryKeyPathNormal)) ;
                    registrySubKeyNormal = Regex.Replace(registrySubKeyNormal, @"^\\+", "");
                    if (String.IsNullOrEmpty(registrySubKeyNormal))
                        throw new DataException(formatMessage("Unexpected empty registry key", registryKeyPathNormal)) ;
                    
                    using (var registryKey = registryRootKey.CreateSubKey(registrySubKeyNormal))
                    {
                        if (RegistryValueKind.None == registryKeyValueType)
                            continue;
                        registryKey.SetValue(registryKeyValueName,
                            convertRegistryKeyValue(registryKeyValue, registryKeyValueType),
                            registryKeyValueType);
                    }
                }
            }
        }

        private void RestoreSettingsLocation(string location)
        {
            var locationLength = NormalizePath(location).Length;
            if (locationLength > FILE_SYSTEM_MAX_PATH
                    || locationLength + _datastore.Length > FILE_SYSTEM_MAX_PATH)
                return;
            
            var destination = location;
            if (Regex.IsMatch( destination, @"^[a-zA-Z]:\\"))
                destination =  destination.Substring(3);
            destination = Path.GetFullPath(Path.Combine(_datastore,  destination));
            
            var locationNormal = NormalizeValue(location);
            
            // References to the data store / mirror directory are excluded in
            // order to avoid possible recursion issues.
            var canonicalLocation = Path.GetFullPath(locationNormal);
            canonicalLocation = canonicalLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalLocation += Path.DirectorySeparatorChar;
            var canonicalDatastore = _datastore.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalDatastore += Path.DirectorySeparatorChar;
            if (canonicalLocation.StartsWith(canonicalDatastore, StringComparison.OrdinalIgnoreCase))
                return;

            var linkType = 0;
            if (Directory.Exists(destination))
                linkType = 1;
            else if (!File.Exists(destination))
                return;

            if (CreateSymbolicLink(canonicalLocation, destination, linkType))
                return;
            
            var code = Marshal.GetLastWin32Error();
            var message = GetErrorMessage(code);
            var context = "Symbolic link cannot be created";
            if (message != null)
                throw new DataException($"{context}: {message}");
            throw new DataException($"{context} (code {code})");
        }

        internal void RestoreSettings()
        {
            foreach (var location in _settings)
                RestoreSettingsLocation(location);
        }

        private static void MirrorRegistryKey(string destination, string registryKey)
        {
            Func<string, string, string> formatMessage = (message, value) =>
                String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

            Func<object, RegistryValueKind, string> formatRegistryKeyValue = (value, type) =>
            {
                if (RegistryValueKind.None == type
                        || value == null)
                    return "";
                if (RegistryValueKind.DWord == type)
                    return ((int)value).ToString();
                if (RegistryValueKind.QWord == type)
                    return ((long)value).ToString();                
                if (RegistryValueKind.Binary == type)
                    return Convert.ToBase64String((byte[])value);
                if (RegistryValueKind.MultiString == type)
                {
                    var block = String.Join(Environment.NewLine, (string[])value);
                    var bytes = Encoding.UTF8.GetBytes(block);
                    return Convert.ToBase64String(bytes);
                }
                if (RegistryValueKind.String != type)
                    return value.ToString();
                var valueText = (string)value;
                if (String.IsNullOrEmpty(valueText))
                    return "";
                if (ENCODING_BASE64_PATTERN.IsMatch(valueText))
                {
                    var bytes = Encoding.UTF8.GetBytes(valueText);
                    return Convert.ToBase64String(bytes);
                }
                return valueText;
            };

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

            using (var registrySubKey = registryRootKey.OpenSubKey(registrySubKeyNormal))
            {
                if (registrySubKey == null)
                    return;
                
                foreach (var valueName in registrySubKey.GetValueNames())
                {
                    var valueType = registrySubKey.GetValueKind(valueName);
                    var valueValue = formatRegistryKeyValue(registrySubKey.GetValue(valueName), valueType);
                 
                    var assignment = "";
                    assignment += !String.IsNullOrEmpty(registryKey) ? 1 : 0;
                    assignment += !String.IsNullOrEmpty(valueName) ? 1 : 0;
                    assignment += valueType != null ? 1 : 0;
                    assignment += !String.IsNullOrEmpty(valueValue) ? 1 : 0;
                    
                    var payload = $"{registryKey}{Environment.NewLine}";
                    if (!String.IsNullOrEmpty(valueName))
                        payload += $"{valueName}{Environment.NewLine}";
                    if (valueType != null)
                        payload += $"{valueType}{Environment.NewLine}";
                    if (!String.IsNullOrEmpty(valueValue))
                        payload += $"{valueValue}{Environment.NewLine}";
                    payload += $"{assignment}{Environment.NewLine}";
                    payload += Environment.NewLine;

                    File.AppendAllText(destination, payload);
                }
            
                foreach (var subKeyName in registrySubKey.GetSubKeyNames())
                    using (var subKey = registrySubKey.OpenSubKey(subKeyName))
                        if (subKey != null)
                            MirrorRegistryKey(destination, $@"{registryKey}\{subKeyName}");
            }
        }

        internal void MirrorMissingRegistry()
        {
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
                        $@"^{Regex.Escape(registryKey)}(\\[\s\w\-%]+)*(::[^\x00-\x1F\\]+)*\s::\s[a-zA-Z]+\s*$",
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
            var locationLength = NormalizePath(location).Length;
            if (locationLength > FILE_SYSTEM_MAX_PATH
                    || locationLength + _datastore.Length > FILE_SYSTEM_MAX_PATH)
                return;

            var destination = location;
            if (Regex.IsMatch( destination, @"^[a-zA-Z]:\\"))
                destination =  destination.Substring(3);
            destination = Path.GetFullPath(Path.Combine(_datastore,  destination));
            
            var locationNormal = NormalizeValue(location);
            
            // References to the data store / mirror directory are excluded in
            // order to avoid possible recursion issues.
            var canonicalLocation = Path.GetFullPath(locationNormal);
            canonicalLocation = canonicalLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalLocation += Path.DirectorySeparatorChar;
            var canonicalDatastore = _datastore.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            canonicalDatastore += Path.DirectorySeparatorChar;
            if (canonicalLocation.StartsWith(canonicalDatastore,StringComparison.OrdinalIgnoreCase))
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
                var path = location;
                if (Regex.IsMatch(path, @"^[a-zA-Z]:\\"))
                    path = path.Substring(3);
                var mirrorLocation= Path.Combine(_datastore, path);
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
    
    internal class DatastoreException : Exception
    {
        internal DatastoreException(string message) : base(message)
        {
        }
    }
}