// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment Settings
// Starts a batch script with the same name minimized.
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

namespace VirtualEnvironment.Settings
{
    internal static class Program
    {
        private static readonly Regex PATTERN_SECTION_SETTINGS =
                new Regex(@"(?:^|[\r\n])\s*\[\s*SETTINGS\s*\](([\r\n]|.)*?)\s*(?:[\r\n]\s*\[|$)", RegexOptions.IgnoreCase);
        private static readonly Regex PATTERN_SECTION_KEY_VALUE =
                new Regex(@"^\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)(?:\s*[\s:=]\s*(.*))?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex PATTERN_SECTION_FILES =
                new Regex(@"(?:^|[\r\n])\s*\[\s*FILES\s*\](([\r\n]|.)*?)\s*(?:[\r\n]\s*\[|$)", RegexOptions.IgnoreCase);
        private static readonly Regex PATTERN_SECTION_FILE =
                new Regex(@"^\s*-\s*([/\\].*?)\s*$");
        private static readonly Regex PATTERN_LINES =
                new Regex(@"(\r\n)+|(\n\r)+|[\r\n]");
        private static readonly Regex PATTERN_PLACEHOLDER =
                new Regex(@"#\[\s*([a-z_](?:[\w\.\-]*[a-z0-9_])?)\s*\]", RegexOptions.IgnoreCase);

        internal static class Version
        {
            // Groups: 1. Number, 2. Release Date, 3. Release Year
            private const string PATTERN = @"^\s*(\d+(?:\.\d+){2})\.((\d{4})\d{4})\s*$";

            private static System.Version AssemblyVersion => typeof(Version).Assembly.GetName().Version;

            internal static string Number => $"{AssemblyVersion.Major}.{AssemblyVersion.Minor}.{AssemblyVersion.Revision}";
            
            internal static string Date => $"{AssemblyVersion.Build}";
            
            internal static string Year => Regex.Replace(Date, @"^(\d{4}).*$", "$1");
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

        private static void ReplaceFilePlaceholders(string file, Dictionary<string, string> settings)
        {
            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var targetFile = Path.GetPathRoot(applicationPath).Substring(0, 2) + file.Replace("/", @"\");
            if (!File.Exists(targetFile))
                return;
            
            Console.WriteLine("- " + targetFile);
            
            var templateFile = targetFile + "-settings";
            if (!File.Exists(templateFile)
                    || DateTime.Compare(File.GetLastWriteTime(targetFile), File.GetLastWriteTime(templateFile)) > 0)
                File.Copy(targetFile, templateFile, true);
            
            var templateContent = File.ReadAllText(templateFile);
            var targetContent = Program.ReplacePlaceholders(templateContent, settings);
            File.WriteAllText(targetFile, targetContent);

            File.SetLastWriteTime(templateFile, DateTime.Now);
        }

        private static void Main(string[] arguments)
        {
            Console.WriteLine($"Seanox Settings [Version 00000]");
            Console.WriteLine($"Copyright (C) {Version.Year} Seanox Software Solutions");
            Console.WriteLine($"Placeholder replacement in files");
            
            if (arguments == null
                    || arguments.Length < 1
                    || !File.Exists(arguments[0]))
            {
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                Console.WriteLine("usage: " + Path.GetFileName(applicationPath) + " <file>");
                return;
            }

            var fileContent = File.ReadAllText(arguments[0]);

            var settingDirectory = new Dictionary<string, string>();
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                if (settingDirectory.ContainsKey(key.ToString().ToLower()))
                    settingDirectory.Remove(key.ToString().ToLower());
                settingDirectory.Add(key.ToString().ToLower(), Environment.GetEnvironmentVariable(key.ToString()));
            }

            var settingsSectionMatch = PATTERN_SECTION_SETTINGS.Match(fileContent);
            var settingsSection = "";
            if (settingsSectionMatch.Success)
                settingsSection = settingsSectionMatch.Groups[1].Value;
            var settingsLines = PATTERN_LINES.Split(settingsSection);
            foreach (var line in settingsLines.Where(line => PATTERN_SECTION_KEY_VALUE.IsMatch(line)))
            {
                var key = PATTERN_SECTION_KEY_VALUE.Replace(line, "$1").ToLower();
                var value = Program.ReplacePlaceholders(PATTERN_SECTION_KEY_VALUE.Replace(line, "$2"), settingDirectory);
                if (settingDirectory.ContainsKey(key))
                    settingDirectory.Remove(key);
                settingDirectory.Add(key, value);
            }
            
            var filesSectionMatch = PATTERN_SECTION_FILES.Match(fileContent);
            var filesSection = "";
            if (filesSectionMatch.Success)
                filesSection = filesSectionMatch.Groups[1].Value;
            var filesLines = PATTERN_LINES.Split(filesSection).Where(line => PATTERN_SECTION_FILE.IsMatch(line)).ToArray();
            if (filesLines.Length <= 0)
                Console.WriteLine("- no file defined");
            foreach (var line in filesLines)
                ReplaceFilePlaceholders(PATTERN_SECTION_FILE.Replace(line, "$1"), settingDirectory);
        }
    }
}