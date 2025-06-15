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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VirtualEnvironment.Platform
{
    internal class Registry
    {
        internal static char PathSeparatorChar = '\\';
        internal static char ValueNameSeparatorChar = ':';
        
        internal static readonly Regex RegistryKeyPattern = new Regex(
                @"(HKEY_CLASSES_ROOT|HKCR"
                + @"|HKEY_CURRENT_USER|HKCU"
                + @"|HKEY_LOCAL_MACHINE|HKLM" 
                + @"|HKEY_USERS|HKU"
                + @"|HKEY_CURRENT_CONFIG|HKCC)"
                + @"(?:\\((?:[^\x00-\x1F:\\]+)"
                + @"(?:\\[^\x00-\x1F:\\]+)*))?"
                + @"(?::(\w[^\x00-\x1F]*\w))?",
            RegexOptions.IgnoreCase);
        
        internal enum RootClass
        {
            HKEY_CLASSES_ROOT,
            HKCR,
            HKEY_CURRENT_USER,
            HKCU,
            HKEY_LOCAL_MACHINE,
            HKLM,
            HKEY_USERS,
            HKU,
            HKEY_CURRENT_CONFIG,
            HKCC
        }
 
        private static readonly Dictionary<RootClass, RegistryKey> RegistryKeyMapping = new Dictionary<Registry.RootClass, RegistryKey>()
        {
            { RootClass.HKEY_CLASSES_ROOT, Microsoft.Win32.Registry.ClassesRoot },
            { RootClass.HKCR, Microsoft.Win32.Registry.ClassesRoot },
            { RootClass.HKEY_CURRENT_USER, Microsoft.Win32.Registry.CurrentUser },
            { RootClass.HKCU, Microsoft.Win32.Registry.CurrentUser },
            { RootClass.HKEY_LOCAL_MACHINE, Microsoft.Win32.Registry.LocalMachine },
            { RootClass.HKLM, Microsoft.Win32.Registry.LocalMachine },
            { RootClass.HKEY_USERS, Microsoft.Win32.Registry.Users },
            { RootClass.HKU, Microsoft.Win32.Registry.Users },
            { RootClass.HKEY_CURRENT_CONFIG, Microsoft.Win32.Registry.CurrentConfig },
            { RootClass.HKCC, Microsoft.Win32.Registry.CurrentConfig }
        };

        private static RegistryKey DetermineRegistryRootKey(RootClass rootClass) =>
            RegistryKeyMapping.TryGetValue(rootClass, out var rootClassMappingValue) ? rootClassMappingValue : null;

        internal static bool Exists(RootClass registryRootClass, string registryKey, string valueName = null)
        {
            var registryRootKey = DetermineRegistryRootKey(registryRootClass);
            using (var registrySubKey = registryRootKey.OpenSubKey(registryKey))
            {
                if (registrySubKey == null)
                    return false;
                if (valueName != null)
                    return registrySubKey.GetValue(valueName) != null;
                return true;
            }
        }
    }
}