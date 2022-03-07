// LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
// Folgenden Seanox Software Solutions oder kurz Seanox genannt.
// Diese Software unterliegt der Version 2 der Apache License.
//
// Virtual Environment ShiftDown
// Downgrades the priority of overactive processes.
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
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace VirtualEnvironment.ShiftDown
{
    // Settings for a XML based configuration.
    // - based on the filename from the ShiftDown service
    // - XML data is mapped to settings via serialization
    // - here there is no validation only indirectly it is checked
    //   during serialization whether the data types fit.
    [XmlRoot("settings")]
    public class Settings
    {
        private const int WORKERS = 5;
        private const int PROCESSOR_LOAD_MAX_PERCENT = 25;
        private const int NORMALIZATION_TIME_SECONDS = 5;

        private int _workers;
        private int _processorLoadMax;
        private int _normalizationTime;

        private string _suspensions;
        private string _isolations;

        public Settings()
        {
            _workers = WORKERS;
            _processorLoadMax = PROCESSOR_LOAD_MAX_PERCENT;
            _normalizationTime = NORMALIZATION_TIME_SECONDS;
            
            _suspensions = "";
            _isolations  = "";
        }
        
        private static string FILE
        {
            get
            {
                var applicationPath = Assembly.GetExecutingAssembly().Location;
                return Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".xml");
            }
        }
        
        internal static Settings Load()
        {
            if (!File.Exists(FILE))
                return new Settings();
            
            try
            {
                var serializer = new XmlSerializer(typeof(Settings));
                using (var reader = new StreamReader(FILE))
                    return (Settings)serializer.Deserialize(reader);
            }
            catch (Exception exception)
            {
                throw new SettingsException(("The settings file is incorrect:"
                        + $"{Environment.NewLine}{exception.Message}"
                        + $"{Environment.NewLine}{exception.InnerException?.Message ?? ""}").Trim(), exception);
            }
        }

        internal class SettingsException : Exception
        {
            internal SettingsException(string message, Exception cause) : base(message, cause) {}
        }
        
        [XmlElement("workers")]
        public int Workers
        {
            get => _workers;
            set => _workers = Math.Max(Math.Min(value, 25), 1);
        }
        
        [XmlElement("processorLoadMax")]
        public int ProcessorLoadMax
        {
            get => _processorLoadMax;
            set => _processorLoadMax = Math.Max(Math.Min(value, 100), 0);
        }
        
        [XmlElement("normalizationTime")]
        public int NormalizationTime
        {
            get => _normalizationTime;
            set => _normalizationTime = Math.Max(value, 1);
        }

        [XmlElement("suspensions")]
        public string Suspensions
        {
            get => _suspensions;
            set => _suspensions = (value ?? "").Trim();
        }
        
        [XmlElement("isolations")]
        public string Isolations
        {
            get => _isolations;
            set => _isolations = (value ?? "").Trim();
        }
    }
}