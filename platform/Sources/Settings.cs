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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal static class Settings
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileSection(
            string lpAppName,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);
        
        internal static readonly Dictionary<string, string> Environment;
        
        internal static readonly HashSet<string> Filesystem;
        
        internal static readonly HashSet<string> Registry;
        
        internal static readonly HashSet<string> Patches;
        
        private const string SECTION_ENVIRONMENT = "ENVIRONMENT";
        private const string SECTION_FILESYSTEM = "FILESYSTEM";
        private const string SECTION_REGISTRY = "REGISTRY";
        private const string SECTION_PATCHES = "PATCHES";
        
        private static readonly Regex PATTERN_PLACEHOLDER =
            new Regex(@"#\[\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)\s*\]", RegexOptions.IgnoreCase);

        static Settings()
        {
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
                Environment[(string)entry.Key] = (string)entry.Value;
            Filesystem = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Registry = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Patches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var iniFilePath = Path.Combine(Path.GetDirectoryName(applicationPath),
                Path.GetFileNameWithoutExtension(applicationPath) + ".ini");
            var iniFile = new FileInfo(iniFilePath);
            if (!iniFile.Exists)
                return;
            
            // With the exception of FILESYSTEM, placeholders and system
            // environment variables are replaced in the configuration values.
            // With FILESYSTEM, the environment variables remain unchanged as
            // they are required in the path for the storage. This serves both
            // to reduce the path length and to mask the drive letter.

            foreach (var key in GetSectionKeys(iniFile, SECTION_ENVIRONMENT))
                Environment.Add(key, NormalizeValue(GetSectionKey(iniFile, SECTION_ENVIRONMENT, key)));
            foreach (var line in GetSectionLines(iniFile, SECTION_FILESYSTEM))
                Filesystem.Add(NormalizeValuePlaceholder(line));
            foreach (var line in GetSectionLines(iniFile, SECTION_REGISTRY))
                Registry.Add(NormalizeValue(line));
            foreach (var line in GetSectionLines(iniFile, SECTION_PATCHES))
                Patches.Add(NormalizeValue(line));
        }

        private static IEnumerable<string> GetSectionKeys(FileInfo file, string section)
        {
            if (!file.Exists)
                return Array.Empty<string>();
            var buffer = new StringBuilder((int)file.Length);
            GetPrivateProfileString(section, null, null, buffer, (uint)buffer.Capacity, file.FullName);
            return buffer.ToString().Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetSectionKey(FileInfo file, string section, string key, string defaultValue = "")
        {
            var result = new StringBuilder((int)file.Length);
            GetPrivateProfileString(section, key, defaultValue, result, (uint)result.Capacity, file.FullName);
            return result.ToString();
        }

        private static IEnumerable<string> GetSectionLines(FileInfo file, string section)
        {
            if (!file.Exists)
                return Array.Empty<string>();
            var buffer = new StringBuilder((int)file.Length);
            GetPrivateProfileSection(section, buffer, (uint)buffer.Capacity, file.FullName);
            return buffer.ToString()
                .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line =>
                    !String.IsNullOrEmpty(line)
                        && !line.StartsWith(";"));
        }

        private static string NormalizeValuePlaceholder(string value)
        {
            return PATTERN_PLACEHOLDER.Replace(value, match =>
                Environment.TryGetValue(match.Groups[1].Value, value: out var expression)
                    ? expression : match.ToString()).Trim();
        }

        private static string NormalizeValue(string value)
        {
            value = NormalizeValuePlaceholder(value);
            value = System.Environment.ExpandEnvironmentVariables(value).Trim();
            return value;
        }
    }
}