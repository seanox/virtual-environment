// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Launcher
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
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace VirtualEnvironment.Startup
{
    [XmlRoot("manifest")]
    public class Manifest
    {
        private string _destination;
        private string _arguments;
        private string _workingDirectory;
        private string _dataStore;
                
        private string[] _settings;

        private static readonly Regex REGISTRY_HKCR_KEY_PATTERN =
            new Regex(@"^(HKEY_CLASSES_ROOT|HKCR)(\\\w+)*$");
        private static readonly Regex REGISTRY_HKCU_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_USER|HKCU)(\\\w+)*$");
        private static readonly Regex REGISTRY_HKLM_KEY_PATTERN =
            new Regex(@"^(HKEY_LOCAL_MACHINE|HKLM)(\\\w+)*$");
        private static readonly Regex REGISTRY_HKU_KEY_PATTERN =
            new Regex(@"^(HKEY_USERS|HKU)(\\\w+)*$");
        private static readonly Regex REGISTRY_HKCC_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_CONFIG|HKCC)(\\\w+)*$");

        private static readonly Regex ENVIRONMENT_VARIABLE_ANTI_PATTERN =
            new Regex(@"=");

        // https://learn.microsoft.com/de-de/windows/win32/api/processenv/nf-processenv-setenvironmentvariablea
        private const int ENVIRONMENT_MAX_VARIABLE = 32767;
        
        internal static string File
        {
            get
            {
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                return Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".xml");
            }
        }

        internal class ValidationException : Exception
        {
            private string[] _messages;

            internal ValidationException(string[] messages)
            {
                _messages = messages;
            }
        }
        
        private static bool ValidatePath(string path)
        {
            try { Path.GetFullPath(path); return true; }
            catch { return false; }
        }

        private static Manifest Validate(Manifest manifest)
        {
            var messages = new List<string>();

            if (!ValidatePath(NormalizeValue(manifest.DataStore)))
                messages.Add($"Invalid datastore: {manifest.DataStore}");

            if (!ValidatePath(NormalizeValue(manifest.Destination)))
                messages.Add($"Invalid destination: {manifest.Destination}");

            if (!ValidatePath(NormalizeValue(manifest.WorkingDirectory)))
                messages.Add($"Invalid working directory: {manifest.WorkingDirectory}");

            if (manifest.Environment != null)
                messages.AddRange(
                    from variable in manifest.Environment
                    let nameNormal = NormalizeValue(variable.Name)
                    where nameNormal.Length > ENVIRONMENT_MAX_VARIABLE
                          || ENVIRONMENT_VARIABLE_ANTI_PATTERN.IsMatch(nameNormal)
                    select $"Invalid environment variable: {variable.Name}");

            if (manifest.Registry != null)
                messages.AddRange(
                    from registryKey in manifest.Registry
                    let registryKeyNormal = NormalizeValue(registryKey)
                    where !REGISTRY_HKCR_KEY_PATTERN.IsMatch(registryKeyNormal)
                          && !REGISTRY_HKCU_KEY_PATTERN.IsMatch(registryKeyNormal)
                          && !REGISTRY_HKLM_KEY_PATTERN.IsMatch(registryKeyNormal)
                          && !REGISTRY_HKU_KEY_PATTERN.IsMatch(registryKeyNormal)
                          && !REGISTRY_HKCC_KEY_PATTERN.IsMatch(registryKeyNormal)
                    select $"Invalid registry key: {registryKey}");

            if (manifest.Settings != null)
                messages.AddRange(
                    from location in manifest.Settings
                    let locationNormal = NormalizeValue(location)
                    where (!ValidatePath(locationNormal)
                           || new Regex(@"^[a-zA-Z]:\\").IsMatch(locationNormal))
                    select $"Invalid settings location: {location}");

            if (messages.Count > 0)
                throw new ValidationException(messages.ToArray());

            return manifest;
        }
        
        internal static Manifest Load()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Manifest));
                using (var reader = new StreamReader(File))
                    return Validate((Manifest)serializer.Deserialize(reader));
            }
            catch (FileNotFoundException exception)
            {
                throw new ManifestException("Manifest file is missing:"
                    + $"{System.Environment.NewLine}{File}", exception);
            }
            catch (Exception exception)
            {
                throw new ManifestException(("Manifest file is incorrect:"
                    + $"{System.Environment.NewLine}{exception.Message}"
                    + $"{System.Environment.NewLine}{exception.InnerException?.Message ?? ""}").Trim(), exception);
            }
        }

        internal static string NormalizeValue(string value)
        {
            return System.Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }
        
        internal class ManifestException : Exception
        {
            internal ManifestException(string message, Exception cause) : base(message, cause) {}
        }

        [XmlElement("destination")]
        public string Destination
        {
            get => _destination;
            set => _destination = value?.Trim();
        }

        [XmlElement("arguments")]
        public string Arguments
        {
            get => _arguments;
            set => _arguments = value?.Trim();
        }

        [XmlElement("workingDirectory")]
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set => _workingDirectory = value?.Trim();
        }

        [XmlElement("datastore")]
        public string DataStore
        {
            get => _dataStore;
            set => _dataStore = value?.Trim();
        }

        [XmlArray("environment")]
        [XmlArrayItem("variable")]
        public Variable[] Environment { get; set; }

        [XmlArray("registry")]
        [XmlArrayItem("key")]
        public string[] Registry { get; set; }

        [XmlArray("settings")]
        [XmlArrayItem("location")]
        public string[] Settings
        {
            get => _settings;
            set => _settings = value?.Select(entry => entry?.Trim()).ToArray();
        }
    }

    public class Variable
    {
        private string _name;
        private string _value;

        [XmlElement("name")]
        public string Name
        {
            get => _name;
            set => _name = value?.Trim();
        }

        [XmlElement("value")]
        public string Value
        {
            get => _value;
            set => _value = value?.Trim();
        }
    }    
}