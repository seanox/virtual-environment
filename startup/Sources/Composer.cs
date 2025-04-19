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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VirtualEnvironment.Startup
{
    // Icons are exclusively concerned with the visual appearance of a Windows
    // file of the type: CPL, DLL, DRV, EXE, SCR, SYS, OCX. However, as the use
    // case concentrates on the startup.exe, the other file types are neglected
    // and are not tested.
    
    // In Windows applications, icons are stored in two parts:
    // - RT_GROUP_ICON: This resource represents a directory. It contains
    //   metadata such as the number of icons contained, their sizes, color
    //   depths and other properties. It then refers to the actual image data
    //   stored in the RT_ICON resources.
    // - RT_ICON: These are the entries that contain the individual image data
    //   of the icons. Traditionally, this image data is stored in BMP format
    //   (more precisely in DIB format). Since Windows Vista, PNG-formatted
    //   icons are also supported, as PNG offers efficient compression, high
    //   color depths and transparency. As a result, the binary structure of
    //   this RT_ICON resource can vary depending on whether BMP or PNG data is
    //   used.
    
    // RT_GROUP_ICON
    // Offsets within the structure
    // The data in RT_GROUP_ICON has the following structure:
    //
    // +--------+---------------+---------------+--------------------------------------------+
    // | Offset | Name          | Size          | Description                                |
    // +--------+---------------+---------------+--------------------------------------------+
    // | 0      | idReserved    | 2 Byte        | Always 0                                   |
    // | 2      | idType        | 2 Byte        | 1 for Icons, 2 for Cursors                 |
    // | 4      | idCount       | 2 Byte        | Number of icons in the groupe              |
    // | 6      | icon entries  | 14 bytes each | References to individual RT_ICON resources |
    // +--------+---------------+---------------+--------------------------------------------+

    // RT_GROUP_ICON Icon-Entry (ICONDIRENTRY)
    // Offsets within a single ICONDIRENTRY for RT_GROUP_ICON
    // Each icon entry in the group has the following structure (14 bytes each):
    //
    // +--------+--------------+-----------+-------------------------------------------------+
    // | Offset | Name         | Size      | Description                                     |
    // +--------+--------------+-----------+-------------------------------------------------+
    // | 0      | bWidth       | 1 Byte    | Width of the icon (16x16, 32x32, 48x48, ...)    |
    // | 1      | bHeight      | 1 Byte    | Height of the icon (usually equal to the width) |
    // | 2      | bColorCount  | 1 Byte    | Number of colors (0 = 256 or more)              |
    // | 3      | bReserved    | 1 Byte    | Must be 0                                       |
    // | 4      | wPlanes      | 2 Byte    | Color plans (normally 1)                        |
    // | 6      | wBitCount    | 2 Byte    | Bits per pixel (color depth)                    |
    // | 8      | dwBytesInRes | 4 Byte    | Size of the RT_ICON in bytes                    |
    // | 12     | wId          | 2 Byte    | Resource ID of the RT_ICON that is referenced   |
    // +--------+--------------+-----------+-------------------------------------------------+

    // LIKE ALL RESOURCES, ICONS ARE ALSO SAVED DEPENDING ON THE LANGUAGE.
    // - LANG_NEUTRAL = 0
    // - LANG_ENGLISH_US = 1033 (quasi standard value if not 0)
    // - other languages
    
    // If the icons to be displayed by Windows Explorer are changed in a Windows
    // application, then ...
    // - RT_ICON must be completely replaced
    //   but 1:1 in terms of height, width and color depth
    // - RT_GROUP_ICON dwBytesInRes of the icon entry must be updated
    
    // When updating icons via the official Windows API (BeginUpdateResource,
    // UpdateResource and EndUpdateResource), Windows creates a completely new
    // resource file internally. The RT_ICON data is rearranged and updated
    // together with the metadata in the RT_GROUP_ICON. This means that even if
    // the dwBytesInRes is changed (e.g. larger), no neighboring resources are
    // overwritten, as the resource delimitations are correctly recalculated and
    // isolated.
    
    internal static class Composer
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindResourceEx(IntPtr hModule, string lpType, IntPtr lpName, ushort wLanguage);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumResourceNames(IntPtr hModule, string lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumResourceLanguages(IntPtr hModule, string lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cbData);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw#parameters    
        // https://learn.microsoft.com/en-us/windows/win32/menurc/resource-types
        // https://learn.microsoft.com/en-us/windows/win32/intl/language-identifiers
        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        private const int RT_ICON = 3;
        private const int RT_GROUP_ICON = 14;

        private const int RT_GROUP_ICON_TYPE_ICON = 1;
        private const int RT_GROUP_ICON_TYPE_CURSOR = 2;

        private const int RT_GROUP_ICON_SIZE = 6;
        private const int RT_GROUP_ICON_ENTRY_SIZE = 14;

        private const int RT_GROUP_ICON_OFFSET_RESERVED = 0;
        private const int RT_GROUP_ICON_OFFSET_TYPE = 2;
        private const int RT_GROUP_ICON_OFFSET_COUNT = 4;
        private const int RT_GROUP_ICON_OFFSET_ICON_ENTRIES = 6;
        
        private const int RT_GROUP_ICON_ENTRY_OFFSET_WIDTH = 0;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_HEIGHT = 1;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_COLOR_COUNT = 2;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_RESERVED = 3;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_PLANES = 4;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_BIT_COUNT = 6;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_ICON_SIZE = 8;
        private const int RT_GROUP_ICON_ENTRY_OFFSET_ICON_ID = 12;
            
        private const int LANG_NEUTRAL = 0;
        private const int LANG_ENGLISH_US = 1033;
        
        private static List<int> GetResourceLanguageIds(FileInfo resourceFile, int resourceType, int resourceId)
        {
            var hModule = LoadLibraryEx(resourceFile.FullName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (hModule == IntPtr.Zero)
                return new List<int>();
            try
            {
                var languagesIds = new List<int>();
                EnumResourceLanguages(hModule, $"#{resourceType}", (IntPtr)resourceId, 
                    (hModuleLang, lpTypeLang, lpNameLang, wLanguageLang, lParamLang) =>
                    {
                        languagesIds.Add(wLanguageLang);
                        return true;
                    }, IntPtr.Zero);
                return languagesIds;
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }
        
        private static List<int> GetResourceIds(FileInfo resourceFile, int resourceType)
        {
            var hModule = LoadLibraryEx(resourceFile.FullName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (hModule == IntPtr.Zero)
                return new List<int>();
            try
            {
                var resourceIds = new List<int>();
                EnumResourceNames(hModule, $"#{resourceType}",
                    (hModuleRes, lpTypeRes, lpNameRes, lParamRes) =>
                    {
                        resourceIds.Add(lpNameRes.ToInt32());
                        return true;
                    }, IntPtr.Zero);
                return resourceIds;
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }
        
        private static IconGroup ExtractIconGroup(FileInfo resourceFile, int resourceId, int languageId)
        {
            var hModule = LoadLibraryEx(resourceFile.FullName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (hModule == IntPtr.Zero)
                return default;
            try
            {
                var hRes = FindResourceEx(hModule, $"#{RT_GROUP_ICON}", (IntPtr)resourceId, (ushort)languageId);
                if (hRes == IntPtr.Zero)
                    return default;

                var hData = LoadResource(hModule, hRes);
                if (hData == IntPtr.Zero)
                    return default;

                var data = new byte[SizeofResource(hModule, hRes)];
                Marshal.Copy(hData, data, 0, data.Length);

                var idType = BitConverter.ToInt16(data, RT_GROUP_ICON_OFFSET_TYPE);
                if (idType != RT_GROUP_ICON_TYPE_ICON)
                    return default;
                var iconCount = BitConverter.ToInt16(data, RT_GROUP_ICON_OFFSET_COUNT);
                var iconEntries = new List<IconEntry>();

                for (var index = 0; index < iconCount; index++)
                {
                    var entryOffset = RT_GROUP_ICON_SIZE
                            + (index * RT_GROUP_ICON_ENTRY_SIZE);
                    var iconEntry = new IconEntry
                    {
                        LanguageId = languageId,
                        IconGroupResourceId = resourceId,
                        ResourceId = BitConverter.ToInt16(data, entryOffset + RT_GROUP_ICON_ENTRY_OFFSET_ICON_ID),
                        Width = data[entryOffset + RT_GROUP_ICON_ENTRY_OFFSET_WIDTH],
                        Height = data[entryOffset + RT_GROUP_ICON_ENTRY_OFFSET_HEIGHT],
                        BitCount = BitConverter.ToInt16(data, entryOffset + RT_GROUP_ICON_ENTRY_OFFSET_BIT_COUNT),
                        SizeInBytes = BitConverter.ToInt32(data, entryOffset + RT_GROUP_ICON_ENTRY_OFFSET_ICON_SIZE),
                    };
                    
                    var hResIcon = FindResourceEx(hModule, $"#{RT_ICON}", (IntPtr)iconEntry.ResourceId, (ushort)iconEntry.LanguageId);
                    if (hResIcon == IntPtr.Zero)
                        throw new ComposerException($"Missing RT_ICON: {iconEntry.ResourceId}:{iconEntry.LanguageId}");
                    var iconDataSize = SizeofResource(hModule, hResIcon);
                    var hDataIcon = LoadResource(hModule, hResIcon);
                    if (hDataIcon == IntPtr.Zero)
                        throw new ComposerException($"Missing RT_ICON data: {iconEntry.ResourceId}:{iconEntry.LanguageId}");
                    iconEntry.Data = new byte[iconDataSize];
                    Marshal.Copy(hDataIcon, iconEntry.Data, 0, (int)iconDataSize);

                    iconEntries.Add(iconEntry);
                }

                return new IconGroup
                {
                    ResourceId = resourceId,
                    Type = idType,
                    Icons = iconEntries
                };
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }

        private class IconGroup
        {
            internal int ResourceId { get; set; }
            internal int Type { get; set; }
            internal List<IconEntry> Icons { get; set; }
            internal int Language => (int)Icons.FirstOrDefault()?.LanguageId;
        }

        private class IconEntry
        {
            internal int Width { get; set; }
            internal int Height { get; set; }
            internal int BitCount { get; set; }
            internal int SizeInBytes { get; set; }
            internal int ResourceId { get; set; }
            internal int IconGroupResourceId { get; set; }
            internal int LanguageId { get; set; }
            internal byte[] Data { get; set; }
        }

        private static IconEntry FindMatchingIconEntry(IEnumerable<IconEntry> iconEntryRepository, IconEntry matchingIconEntry)
        {
            return iconEntryRepository
                .OrderBy(iconEntry =>
                    iconEntry.LanguageId == LANG_NEUTRAL ? 0
                    : iconEntry.LanguageId == LANG_ENGLISH_US ? 1
                    : 2)
                .FirstOrDefault(iconEntry =>
                    iconEntry.Width == matchingIconEntry.Width
                    && iconEntry.Height == matchingIconEntry.Height
                    && iconEntry.BitCount == matchingIconEntry.BitCount);
        }
        
        private static byte[] BuildIconGroupResource(IconGroup group)
        {
            var count = group.Icons.Count;
            var totalSize = RT_GROUP_ICON_SIZE
                + (count * RT_GROUP_ICON_ENTRY_SIZE);
            var groupData = new byte[totalSize];

            // Header
            Buffer.BlockCopy(
                BitConverter.GetBytes((ushort)0),
                0,
                groupData,
                0,
                2);
            Buffer.BlockCopy(
                BitConverter.GetBytes((ushort)1),
                0,
                groupData,
                2,
                2);
            Buffer.BlockCopy(
                BitConverter.GetBytes((ushort)count),
                0,
                groupData,
                4,
                2);

            // Icon Entries
            var offset = RT_GROUP_ICON_SIZE;
            foreach (var icon in group.Icons)
            {
                groupData[offset + RT_GROUP_ICON_ENTRY_OFFSET_WIDTH] = (byte)icon.Width;
                groupData[offset + RT_GROUP_ICON_ENTRY_OFFSET_HEIGHT] = (byte)icon.Height;
                groupData[offset + RT_GROUP_ICON_ENTRY_OFFSET_COLOR_COUNT] = 0;
                groupData[offset + RT_GROUP_ICON_ENTRY_OFFSET_RESERVED] = 0;
                Buffer.BlockCopy(
                    BitConverter.GetBytes((ushort)1),
                    0,
                    groupData,
                    offset + RT_GROUP_ICON_ENTRY_OFFSET_PLANES,
                    2);
                Buffer.BlockCopy(
                    BitConverter.GetBytes((ushort)icon.BitCount),
                    0,
                    groupData,
                    offset + RT_GROUP_ICON_ENTRY_OFFSET_BIT_COUNT,
                    2);
                Buffer.BlockCopy(
                    BitConverter.GetBytes((uint)icon.SizeInBytes),
                    0,
                    groupData,
                    offset + RT_GROUP_ICON_ENTRY_OFFSET_ICON_SIZE,
                    4);
                Buffer.BlockCopy(
                    BitConverter.GetBytes((ushort)icon.ResourceId),
                    0,
                    groupData,
                    offset + RT_GROUP_ICON_ENTRY_OFFSET_ICON_ID,
                    2);

                offset += RT_GROUP_ICON_ENTRY_SIZE;
            }

            return groupData;
        }

        private static void ComposeIcon(FileInfo destinationFile, FileInfo sourceFile)
        {
            var sourceIconGroups = new List<IconGroup>();
            foreach (var sourceIconGroupResourceId in GetResourceIds(sourceFile, RT_GROUP_ICON))
                sourceIconGroups.AddRange(GetResourceLanguageIds(sourceFile, RT_GROUP_ICON, sourceIconGroupResourceId)
                    .Select(sourceIconGroupResourceLanguageId =>
                        ExtractIconGroup(sourceFile, sourceIconGroupResourceId, sourceIconGroupResourceLanguageId))
                    .Where(iconGroup => !iconGroup.Equals(default(IconGroup))));
            
            var sourceIconEntries = new List<IconEntry>();
            foreach (var sourceIconGroup in sourceIconGroups)
                sourceIconEntries.AddRange(sourceIconGroup.Icons);

            var destinationIconGroups = new List<IconGroup>();
            foreach (var destinationIconGroupResourceId in GetResourceIds(destinationFile, RT_GROUP_ICON))
                destinationIconGroups.AddRange(GetResourceLanguageIds(destinationFile, RT_GROUP_ICON, destinationIconGroupResourceId)
                    .Select(destinationIconGroupResourceLanguageId =>
                        ExtractIconGroup(destinationFile, destinationIconGroupResourceId, destinationIconGroupResourceLanguageId))
                    .Where(iconGroup => !iconGroup.Equals(default(IconGroup))));
            
            var destinationIconEntries = new List<IconEntry>();
            foreach (var destinationIconGroup in destinationIconGroups)
                destinationIconEntries.AddRange(destinationIconGroup.Icons);

            var hUpdate = BeginUpdateResource(destinationFile.FullName, false);
            if (hUpdate == IntPtr.Zero)
                throw new ComposerException($"Update resource failed: {destinationFile.Name}");
            
            var destinationIconReplacements = new Dictionary<IconEntry, IconEntry>();
            foreach (var destinationIconEntry in destinationIconEntries)
            {
                var iconEntry = FindMatchingIconEntry(sourceIconEntries, destinationIconEntry);
                if (iconEntry.Equals(default(IconEntry)))
                    continue;
                destinationIconReplacements.Add(destinationIconEntry, iconEntry);
                if (!UpdateResource(hUpdate,
                        (IntPtr)RT_ICON,
                        (IntPtr)destinationIconEntry.ResourceId,
                        (ushort)destinationIconEntry.LanguageId,
                        iconEntry.Data,
                        (uint)iconEntry.Data.Length))
                    throw new ComposerException($"Update resource failed: {destinationFile.Name}");
            }
            
            var destinationIconGroupUpdate = destinationIconGroups
                .Where(iconGroup => destinationIconReplacements.Keys
                    .Any(iconEntry => iconEntry.IconGroupResourceId == iconGroup.ResourceId))
                .ToList();
            foreach (var destinationIconGroup in destinationIconGroupUpdate)
            {
                foreach (var destinationIconGroupIconEntry in destinationIconGroup.Icons)
                    if (destinationIconReplacements.ContainsKey(destinationIconGroupIconEntry))
                        destinationIconGroupIconEntry.SizeInBytes =
                            destinationIconReplacements[destinationIconGroupIconEntry].SizeInBytes;
                var iconGroupDataUpdate = BuildIconGroupResource(destinationIconGroup);
                if (!UpdateResource(
                        hUpdate,
                        (IntPtr)RT_GROUP_ICON,
                        (IntPtr)destinationIconGroup.ResourceId,
                        (ushort)destinationIconGroup.Language,
                        iconGroupDataUpdate,
                        (uint)iconGroupDataUpdate.Length))
                    throw new ComposerException($"Update resource failed: {destinationFile.Name}");
            }
            
            if (!EndUpdateResource(hUpdate, false))
                throw new ComposerException($"Update resource failed: {destinationFile.Name}");
        }
        
        private static FileInfo ComposeFileInfo(FileInfo file)
        {
            var directory = Path.GetDirectoryName(file.FullName);
            var name = Path.GetFileNameWithoutExtension(file.FullName);
            var extension = Path.GetExtension(file.FullName);
            return new FileInfo(Path.Combine(directory, $"{name}Start{extension}"));
        }

        internal static void Compose(Application[] applications)
        {
            var mainApplication = applications.FirstOrDefault(
                application => application.WaitForExit);
            if (mainApplication == null)
                Messages.Push(Messages.Type.Error, "Missing main application with WaitForExit", true);
            var destination = Environment.ExpandEnvironmentVariables(mainApplication.Destination ?? "").Trim();
            if (!File.Exists(destination))
                Messages.Push(Messages.Type.Error, "Found main application with unsupported destination", true);

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            var compositeApplicationName = ComposeFileInfo(new FileInfo(destination)).Name;
            var compositeApplicationLocation = Path.Combine(assemblyDirectory, compositeApplicationName);
            var compositeManifestName = $"{Path.GetFileNameWithoutExtension(compositeApplicationName)}.xml";
            var compositeManifestLocation = Path.Combine(assemblyDirectory, compositeManifestName);
                
            Messages.Push(Messages.Type.Trace, $"Compose {compositeApplicationName}");
            if (File.Exists(compositeApplicationLocation))
                File.Delete(compositeApplicationLocation);
            File.Copy(assemblyLocation, compositeApplicationLocation);
            ComposeIcon(new FileInfo(Path.Combine(assemblyDirectory, compositeApplicationName)), new FileInfo(destination));
            Messages.Push(Messages.Type.Trace, $"Compose {compositeManifestName}");
            if (File.Exists(compositeManifestLocation))
                File.Delete(compositeManifestLocation);
            File.Move(Manifest.File, compositeManifestLocation);
            Messages.Push(Messages.Type.Trace, "Compose completed");
        }
        
        private class ComposerException : Exception
        {
            internal ComposerException(string message) : base(message)
            {
            }
        }
    }
}