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

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal static class Resources
    {
        internal static byte[] GetResource(string resourceName)
        {
            resourceName = $"{typeof(Diskpart).Namespace}.Resources.{resourceName}";
            resourceName = new Regex("[\\./\\\\]+").Replace(resourceName, ".");
            resourceName = new Regex("\\s").Replace(resourceName, "_");
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                var buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                return buffer;
            }
        }

        internal static string GetTextResource(string resourceName)
        {
            return Encoding.ASCII.GetString(GetResource(resourceName));
        }
    }
}