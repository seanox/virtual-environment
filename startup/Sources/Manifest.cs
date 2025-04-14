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
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace VirtualEnvironment.Startup
{
    [XmlRoot("manifest")]
    public class Manifest
    {
        private static readonly Regex REGISTRY_HKCR_KEY_PATTERN =
            new Regex(@"^(HKEY_CLASSES_ROOT|HKCR)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKCU_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_USER|HKCU)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKLM_KEY_PATTERN =
            new Regex(@"^(HKEY_LOCAL_MACHINE|HKLM)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKU_KEY_PATTERN =
            new Regex(@"^(HKEY_USERS|HKU)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");
        private static readonly Regex REGISTRY_HKCC_KEY_PATTERN =
            new Regex(@"^(HKEY_CURRENT_CONFIG|HKCC)(\\[\s\w-]+)*(?::(\w(?:(?:[^\x00-\x1F]*\w)?)?))?$");

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
  
        private static bool ValidatePath(string path)
        {
            try
            {
                if (path.Length == 0)
                    return true;
                Path.GetFullPath(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Manifest Validate(Manifest manifest)
        {
            var issues = new List<string>();
            
            Func<string, string, string> formatMessage = (message, value) =>
                String.IsNullOrWhiteSpace(value) ? message : $"{message}: ${value}";

            if (!ValidatePath(NormalizeValue(manifest.Datastore)))
                issues.Add(formatMessage("Invalid datastore", manifest.Datastore));
            
            if (manifest.Applications == null
                    || manifest.Applications.Length <= 0)
                issues.Add("Missing applications");
            if (manifest.Applications != null)
                foreach (var application in manifest.Applications)
                {
                    var destinationNormal = NormalizeValue(application.Destination); 
                    if (!ValidatePath(destinationNormal)
                            || String.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(destinationNormal)))
                        issues.Add(formatMessage("Invalid destination", application.Destination));

                    if (!ValidatePath(NormalizeValue(application.WorkingDirectory)))
                        issues.Add(formatMessage("Invalid working directory", application.WorkingDirectory));
                }
            
            if (manifest.Applications != null
                    && manifest.Applications.Length > 1
                    && !manifest.Applications.Any(application => application.WaitForExit))
                issues.Add("Invalid applications, missing use of WaitForExit");

            if (manifest.Environment != null)
                issues.AddRange(
                    from variable in manifest.Environment
                    let nameNormal = NormalizeValue(variable.Name)
                    where nameNormal.Length > ENVIRONMENT_MAX_VARIABLE
                        || ENVIRONMENT_VARIABLE_ANTI_PATTERN.IsMatch(nameNormal)
                    select formatMessage("Invalid environment variable", variable.Name));

            if (manifest.Registry != null)
                issues.AddRange(
                    from registryKey in manifest.Registry
                    let registryKeyNormal = NormalizeValue(registryKey)
                    where !REGISTRY_HKCR_KEY_PATTERN.IsMatch(registryKeyNormal)
                        && !REGISTRY_HKCU_KEY_PATTERN.IsMatch(registryKeyNormal)
                        && !REGISTRY_HKLM_KEY_PATTERN.IsMatch(registryKeyNormal)
                        && !REGISTRY_HKU_KEY_PATTERN.IsMatch(registryKeyNormal)
                        && !REGISTRY_HKCC_KEY_PATTERN.IsMatch(registryKeyNormal)
                    select formatMessage("Invalid registry key", registryKey));

            if (manifest.FileSystem != null)
                issues.AddRange(
                    from location in manifest.FileSystem
                    let locationNormal = NormalizeValue(location)
                    where (!ValidatePath(locationNormal)
                        || !Regex.IsMatch(locationNormal, @"^[a-zA-Z]:\\"))
                    select formatMessage("Invalid file system location", location));

            if (issues.Count <= 0)
                return manifest;
            
            foreach (var issue in issues)
                Messages.Push(Messages.Type.Error, issue);
            throw new ManifestValidationException();
        }
        
        internal static Manifest Load()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Manifest));
                using (var reader = new StreamReader(File))
                    return Validate((Manifest)serializer.Deserialize(reader));
            }
            catch (FileNotFoundException)
            {
                throw new ManifestException("Manifest file is missing:"
                    + $"{System.Environment.NewLine}{File}");
            }
        }
        
        internal class ManifestException : Exception
        {
            internal ManifestException()
            {
            }

            internal ManifestException(string message) : base(message)
            {
            }
            
            internal ManifestException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    
        internal class ManifestValidationException : ManifestException
        {
            internal ManifestValidationException()
            {
            }
        }

        private static string NormalizeValue(string value)
        {
            return System.Environment.ExpandEnvironmentVariables(value ?? "").Trim();
        }

        private string _datastore;
        
        [XmlElement("datastore")]
        public string Datastore
        {
            get => _datastore;
            set => _datastore = value?.Trim();
        }

        [XmlArray("applications")]
        [XmlArrayItem("application")]
        public Application[] Applications { get; set; }

        [XmlArray("environment")]
        [XmlArrayItem("variable")]
        public Variable[] Environment { get; set; }

        private string[] _registry;
        
        [XmlArray("registry")]
        [XmlArrayItem("key")]
        public string[] Registry
        {
            get => _registry;
            set => _registry = value?.Select(entry => entry?.Trim()).ToArray();
        }
        
        private string[] _filesystem;

        [XmlArray("filesystem")]
        [XmlArrayItem("location")]
        public string[] FileSystem
        {
            get => _filesystem;
            set => _filesystem = value?.Select(entry => entry?.Trim()).ToArray();
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
    
    public class Application
    {
        private string _destination;
        private string _arguments;
        private string _workingDirectory;

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
        
        [XmlElement("waitForExit")]
        public bool WaitForExit { get; set; }
    }
}