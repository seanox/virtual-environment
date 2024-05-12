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
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal static class Settings
    {
        private static Dictionary<string, string> _values;
        
        private static string[] _files;
        
        private static readonly Regex PATTERN_COMMENT =
            new Regex(@"(?:^|[\r\n])\s*;[^\r\n]*\S*");

        private static readonly Regex PATTERN_SECTION_SETTINGS =
                new Regex(@"(?:^|[\r\n])\s*\[\s*SETTINGS\s*\](([\r\n]|.)*?)\s*(?:[\r\n]\s*\[|$)", RegexOptions.IgnoreCase);
        
        private static readonly Regex PATTERN_SECTION_KEY_VALUE =
                new Regex(@"^\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)(?:\s*[\s:=]\s*(.*?))?\s*$", RegexOptions.IgnoreCase);
        
        private static readonly Regex PATTERN_SECTION_FILES =
                new Regex(@"(?:^|[\r\n])\s*\[\s*FILES\s*\](([\r\n]|.)*?)\s*(?:[\r\n]\s*\[|$)", RegexOptions.IgnoreCase);
        
        private static readonly Regex PATTERN_FILE =
                new Regex(@"^\s*([/\\].*?)\s*$");
        
        private static readonly Regex PATTERN_LINES =
                new Regex(@"(\r\n)+|(\n\r)+|[\r\n]");
        
        private static readonly Regex PATTERN_PLACEHOLDER =
                new Regex(@"#\[\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)\s*\]", RegexOptions.IgnoreCase);

        internal static string ReplacePlaceholders(string text)
        {
            Settings.Initialize();
            return Settings.ReplacePlaceholders(text, _values);
        }

        private static string ReplacePlaceholders(string text, Dictionary<string, string> settings)
        {
            return PATTERN_PLACEHOLDER.Replace(text, match =>
            {
                var key = match.Groups[1].Value.ToLower();
                if (settings.ContainsKey(key))
                    return settings[key];
                return match.ToString();
            });
        }

        private static void Initialize()
        {
            if (_values != null)
                return;
            
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var iniFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".ini");
            if (!File.Exists(iniFile))
                return;
            
            var settingsDictionary = new Dictionary<string, string>();
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                if (settingsDictionary.ContainsKey(key.ToString().ToLower()))
                    settingsDictionary.Remove(key.ToString().ToLower());
                settingsDictionary.Add(key.ToString().ToLower(), Environment.GetEnvironmentVariable(key.ToString()));
            }

            var fileContent = File.ReadAllText(iniFile);
            fileContent = PATTERN_COMMENT.Replace(fileContent, "");

            var values = new Dictionary<string, string>();
            var settingsSectionMatch = PATTERN_SECTION_SETTINGS.Match(fileContent);
            var settingsSection = "";
            if (settingsSectionMatch.Success)
                settingsSection = settingsSectionMatch.Groups[1].Value;
            var settingsLines = PATTERN_LINES.Split(settingsSection);
            foreach (var line in settingsLines.Where(line => PATTERN_SECTION_KEY_VALUE.IsMatch(line)))
            {
                var key = PATTERN_SECTION_KEY_VALUE.Replace(line, "$1").ToLower();
                var value = PATTERN_SECTION_KEY_VALUE.Replace(line, "$2");
                value = Environment.ExpandEnvironmentVariables(value);
                value = Settings.ReplacePlaceholders(value, settingsDictionary);
                if (settingsDictionary.ContainsKey(key))
                    settingsDictionary.Remove(key);
                settingsDictionary.Add(key, value);
                if (values.ContainsKey(key))
                    values.Remove(key);
                values.Add(key, value);
            }

            var files = new List<string>();
            var filesSectionMatch = PATTERN_SECTION_FILES.Match(fileContent);
            var filesSection = "";
            if (filesSectionMatch.Success)
                filesSection = filesSectionMatch.Groups[1].Value;
            var filesLines = PATTERN_LINES.Split(filesSection);
            foreach (var line in filesLines)
            {
                if (!PATTERN_FILE.Match(line).Success)
                    continue;
                var file = PATTERN_FILE.Replace(line, "$1");
                file = Environment.ExpandEnvironmentVariables(file);
                file = Settings.ReplacePlaceholders(file, settingsDictionary);
                files.Add(file);
            }

            if (_values == null)
                _values = values;
            if (_files == null)
                _files = files.ToArray();
        }

        internal static Dictionary<string, string> Values
        {
            get
            {
                Settings.Initialize();
                if (_values == null)
                    return new Dictionary<string, string>();
                return _values.ToDictionary(entry => entry.Key,
                    entry => entry.Value);
            }
        }

        internal static string[] Files
        {
            get
            {
                Settings.Initialize();
                if (_files == null)
                    return Array.Empty<String>();
                return (string[])_files.Clone();
            }
        }
    }
}