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

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal partial class Resources
    {
        internal static ResourceFiles Files { get; } = new ResourceFiles();
        internal static ResourceTexts Texts { get; } = new ResourceTexts();
        
        private static byte[] GetResource(string resourceName)
        {
            resourceName = $"{typeof(Resources).Namespace}.Resources.{resourceName}";
            resourceName = new Regex("[\\./\\\\]+").Replace(resourceName, ".");
            resourceName = new Regex("\\s").Replace(resourceName, "_");
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
        }

        internal class ResourceFiles
        {
            internal byte[] this[string resourceName] =>
                GetResource(resourceName);
        }

        internal class ResourceTexts
        {
            internal string this[string resourceName] =>
                Encoding.UTF8.GetString(GetResource(resourceName));
        }
    }
}