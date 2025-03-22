// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
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
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal static class Settings
    {
        private static Dictionary<string, string> _values;
        
        private static string[] _files;
        
        private static readonly Regex PATTERN_COMMENT =
                new Regex(@"\s*(;.*)\s*$", RegexOptions.Multiline);

        private static readonly Regex PATTERN_EMPTY_LINE =
                new Regex(@"[\r\n]+\s*[\r\n]+", RegexOptions.Singleline);
        
        private static readonly Regex PATTERN_SECTIONS =
                new Regex(@"(?<=(^|[\r\n]))\s*\[\s*([^\r\n\]]+?)\s*\]\s*[\r\n]+\s*(.*?)\s*(?=$|([\r\n]\s*\[))", RegexOptions.Singleline);
        
        private static readonly Regex PATTERN_SECTION_KEY_VALUE =
                new Regex(@"^\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)(?:\s*[\s:=]\s*(.*?))?\s*$", RegexOptions.IgnoreCase);
        
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

        private static string ReplacePlaceholders(string text,
            IReadOnlyDictionary<string, string> settings)
        {
            return PATTERN_PLACEHOLDER.Replace(text, match =>
            {
                var key = match.Groups[1].Value;
                return settings.TryGetValue(key, value: out var expression)
                    ? expression : match.ToString();
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
            
            var settingsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                settingsDictionary[(string)entry.Key] = (string)entry.Value;
            
            var fileContent = File.ReadAllText(iniFile);
            fileContent = PATTERN_COMMENT.Replace(fileContent, "");
            fileContent = PATTERN_EMPTY_LINE.Replace(fileContent, "\r\n");

            var sectionsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in PATTERN_SECTIONS.Matches(fileContent))
                sectionsDictionary[match.Groups[2].Value] = match.Groups[3].Value;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            var settingsSection = sectionsDictionary.TryGetValue("settings", out var settingsContent) ? settingsContent : String.Empty;
            foreach (var settingsLine in PATTERN_LINES.Split(settingsSection))
            {
                if (!PATTERN_SECTION_KEY_VALUE.IsMatch(settingsLine))
                    continue;
                var key = PATTERN_SECTION_KEY_VALUE.Replace(settingsLine, "$1");
                var value = PATTERN_SECTION_KEY_VALUE.Replace(settingsLine, "$2");
                value = Environment.ExpandEnvironmentVariables(value);
                value = Settings.ReplacePlaceholders(value, settingsDictionary);
                settingsDictionary[key] = value;
                values[key] = value;
            }

            var files = new List<string>();
            var filesSection = sectionsDictionary.TryGetValue("files", out var filesContent) ? filesContent : String.Empty;
            foreach (var line in PATTERN_LINES.Split(filesSection))
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