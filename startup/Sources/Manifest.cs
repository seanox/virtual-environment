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
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace VirtualEnvironment.Startup
{
    [XmlRoot("manifest")]
    public class Manifest
    {
        internal static string File
        {
            get
            {
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                return Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".xml");
            }
        }

        private static Manifest Validate(Manifest manifest)
        {
            //manifest.DataStore
            //manifest.Destination
            //manifest.WorkingDirectory

            if (manifest.Environment != null)
            {
            }

            if (manifest.Registry != null)
            {
            }

            if (manifest.Settings != null)
            {
            }

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
        public string Destination { get; set; }

        [XmlElement("arguments")]
        public string Arguments { get; set; }

        [XmlElement("workingDirectory")]
        public string WorkingDirectory { get; set; }

        [XmlElement("datastore")]
        public string DataStore { get; set; }

        [XmlArray("environment")]
        [XmlArrayItem("variable")]
        public Variable[] Environment { get; set; }

        [XmlArray("registry")]
        [XmlArrayItem("key")]
        public string[] Registry { get; set; }

        [XmlArray("settings")]
        [XmlArrayItem("location")]
        public string[] Settings { get; set; }
    }

    public class Variable
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }
    }    
}