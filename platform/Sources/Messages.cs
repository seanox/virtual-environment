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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualEnvironment.Startup
{
    internal static class Messages
    {
        private static readonly HashSet<ISubscriber> _subscriptions;
        
        private static readonly object _lock; 
        
        static Messages()
        {
            _subscriptions = new HashSet<ISubscriber>();
            _lock = new object();
        }
        
        internal static void Subscribe(ISubscriber recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            lock (_lock)
                _subscriptions.Add(recipient);
        }

        internal static void Unsubscribe(ISubscriber recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            lock (_lock)
                if (_subscriptions.Contains(recipient))
                    _subscriptions.Remove(recipient);
        }
        
        internal interface ISubscriber
        {
            void Receive(Message message);
        }

        internal static void Push(params Message[] messages)
        {
            List<ISubscriber> recipients;
            lock (_lock)
                recipients = _subscriptions.ToList();
                
            Parallel.ForEach(messages, message =>
                Parallel.ForEach(recipients, recipient =>
                {
                    try { recipient.Receive(message); } catch { }
                }));
        } 
        
        internal static void Push(Type type, params string[] content)
        {
            content = content
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            var stringBuilder = new StringBuilder();
            foreach (var line in content)
                stringBuilder.AppendLine(line);
            Push(new Message(type, stringBuilder.ToString()));
        }

        internal enum Type
        {
            Error,
            Warning,
            Trace,
            Text,
            Data,
            Exit
        }

        internal readonly struct Message
        {
            internal Type Type { get; }
            internal string Content { get; }
            internal string Context { get; }

            internal Message(Type type, string content)
            {
                Type = type;
                Content = content;
                Context = new Func<string>(() =>
                    new StackTrace().GetFrames()
                        .Select(stackFrame => stackFrame.GetMethod().DeclaringType.Name)
                        .FirstOrDefault(name => name != nameof(Messages))
                )();
            }
            
            public override string ToString()
            {
                var stringBuilder = new StringBuilder(Type.ToString().ToUpper());
                if (!string.IsNullOrWhiteSpace(Content))
                    stringBuilder.Append($": {Content.Trim()}");
                return stringBuilder.ToString();
            }
        }
    }
}