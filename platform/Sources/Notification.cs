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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal static class Notification
    {
        internal enum Type
        {
            Error,
            Warning,
            Trace,
            Abort
        }

        internal readonly struct Message
        {
            internal Type   Type { get; }
            internal string Text { get; }

            internal Message(Type type, string text)
            {
                Type = type;
                Text = text;
            }
        }

        private static readonly List<INotification> _subscriptions;

        static Notification()
        {
            _subscriptions = new List<INotification>();

            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var loggingFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".log");
            if (File.Exists(loggingFile)
                    && new FileInfo(loggingFile).Length > 0)
                using (var streamWriter = File.AppendText(loggingFile))
                    streamWriter.WriteLine();
        }

        internal static void Push(Type type, string message, Exception exception)
        {
            Push(type, message, $"@{exception}");
        }
        
        internal static void Push(Type type, params string[] messages)
        {
            var publication = String.Join("\r\n", messages.Where(message =>
                    !message.Trim().StartsWith("@")));
            if (publication.Length > 0)
                _subscriptions.ForEach(recipient =>
                        recipient.Receive(new Message(type, publication)));

            var applicationPath = Assembly.GetExecutingAssembly().Location;
            var loggingFile = Path.Combine(Path.GetDirectoryName(applicationPath),
                    Path.GetFileNameWithoutExtension(applicationPath) + ".log");
            while (messages.Length > 0
                   && Regex.IsMatch(messages[0], "^((@+)|(\\s+))$"))
                messages = messages.Skip(1).ToArray();
            while (messages.Length > 0
                   && Regex.IsMatch(messages[messages.Length -1], "^((@+)|(\\s+))$"))
                messages = messages.Take(messages.Length -1).ToArray();
            messages = messages.Select(message =>
                    Messages.DiskpartUnexpectedErrorOccurred == message
                            || Messages.WorkerUnexpectedErrorOccurred == message
                        ? String.Format(message, Path.GetFileName(loggingFile)) : message).ToArray();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var output = String.Join("\r\n", messages.Select(message => Regex.Replace(message, "^@+", "")).ToArray()); 
            output = $"{timestamp} {(Type.Abort.Equals(type) ? Type.Trace : type).ToString().ToUpper()} " + output;
            var prefix = $"{timestamp}  ...  ";
            output = Regex.Replace(output, @"[^\S\r\n]*((\r\n)|(\n\r)|[\r\n])[^\S\r\n]*", "\r\n" + prefix);
            using (var streamWriter = File.AppendText(loggingFile))
                streamWriter.WriteLine(output);
        }

        internal static void Subscribe(INotification recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            if (!_subscriptions.Contains(recipient))
                _subscriptions.Add(recipient);
        }

        internal static void UnSubscribe(INotification recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            if (_subscriptions.Contains(recipient))
                _subscriptions.Remove(recipient);
        }

        internal interface INotification
        {
            void Receive(Message message);
        }
    }
}