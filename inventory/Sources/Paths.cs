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
using System.IO;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Inventory
{
    internal static class Paths
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
        internal const int FILE_SYSTEM_MAX_PATH = 258;

        internal static readonly string SYSTEM_DRIVE;
        internal static readonly string SYSTEM_DRIVE_PATH;

        internal static readonly string SYSTEM_VOLUME_INFORMATION_PATH;

        internal static readonly string SYSTEM_PROGRAM_FILES_PATH;
        internal static readonly string SYSTEM_PROGRAM_FILES_X86_PATH;
        internal static readonly string SYSTEM_PROGRAM_DATA_PATH;

        internal static readonly string SYSTEM_ROOT_PATH;
        
        internal static readonly string SYSTEM_WINDOWS_PATH;
        
        internal static readonly string USER_PROFILE_PATH;

        static Paths()
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (String.IsNullOrEmpty(systemDrive))
                systemDrive = "C:";
            SYSTEM_DRIVE = systemDrive;
            SYSTEM_DRIVE_PATH = PathNormalize(systemDrive).ToUpper() + Path.DirectorySeparatorChar;
            
            SYSTEM_VOLUME_INFORMATION_PATH =
                PathNormalize(Path.Combine(SYSTEM_DRIVE_PATH, "System Volume Information"));

            SYSTEM_PROGRAM_FILES_PATH = PathNormalize(Environment.GetEnvironmentVariable("ProgramFiles"));
            SYSTEM_PROGRAM_FILES_X86_PATH = PathNormalize(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
            SYSTEM_PROGRAM_DATA_PATH = PathNormalize(Environment.GetEnvironmentVariable("ProgramData"));

            SYSTEM_ROOT_PATH = PathNormalize(Environment.GetEnvironmentVariable("SystemRoot"));
            SYSTEM_WINDOWS_PATH = PathNormalize(Path.Combine(SYSTEM_ROOT_PATH, "WinDir"));

            USER_PROFILE_PATH = PathNormalize(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        internal static string PathNormalize(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path.TrimEnd(Path.DirectorySeparatorChar);
            return path;        
        }
        
        private static bool PathStartsWithOrEquals(string path, string pattern)
        {
            path = PathNormalize(path) + Path.DirectorySeparatorChar;
            pattern = PathNormalize(pattern) + Path.DirectorySeparatorChar;
            return path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)
                    || path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
        
        private static string PathAbstractAlias(string path, string pattern, string alias)
        {
            if (!PathStartsWithOrEquals(path, pattern))
                return path;
            path = PathNormalize(path) + Path.DirectorySeparatorChar;
            pattern = PathNormalize(pattern) + Path.DirectorySeparatorChar;
            if (path.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                return alias;
            return PathNormalize(Path.Combine(alias, path.Substring(pattern.Length)));
        }

        internal static string PathAbstract(string path)
        {
            if (PathStartsWithOrEquals(path, USER_PROFILE_PATH))
                return PathAbstractAlias(path, USER_PROFILE_PATH, "%UserProfile%");
            if (PathStartsWithOrEquals(path, SYSTEM_PROGRAM_FILES_PATH))
                return PathAbstractAlias(path, SYSTEM_PROGRAM_FILES_PATH, "%ProgramFiles%");
            if (PathStartsWithOrEquals(path, SYSTEM_PROGRAM_FILES_X86_PATH))
                return PathAbstractAlias(path, SYSTEM_PROGRAM_FILES_X86_PATH, "%ProgramFiles(x86)%");
            if (PathStartsWithOrEquals(path, SYSTEM_PROGRAM_DATA_PATH))
                return PathAbstractAlias(path, SYSTEM_PROGRAM_DATA_PATH, "%ProgramData%");
            if (PathStartsWithOrEquals(path, SYSTEM_ROOT_PATH))
                return PathAbstractAlias(path, SYSTEM_ROOT_PATH, "%SystemRoot%");
            path = Regex.Replace(path, @"^([A-Za-z]):", match =>
                $"%%{match.Groups[1].Value.ToUpper()}%"); 
            return path;
        }
    }
}