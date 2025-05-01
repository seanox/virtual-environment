// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Platform
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirtualEnvironment.Platform
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
        
        internal static void Push(Type type, params object[] data)
        {
            Push(new Message(type, data));
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
            internal string Context { get; }
            internal object Data { get; }

            internal Message(Type type, object data)
                : this(type, null, data)
            {
            }
            
            internal Message(Type type, string context, object data)
            {
                Type = type;
                if (!String.IsNullOrWhiteSpace(context))
                    context = Regex.Replace(context, @"[\r\n]+", " ").Trim();
                Context = !String.IsNullOrWhiteSpace(context) ? context : null;
                Data = data;
            }
            
            public override string ToString()
            {
                var stringBuilder = new StringBuilder(Type.ToString().ToUpper())
                    .Append(" ");

                var content = Data;

                if (!String.IsNullOrWhiteSpace(Context))
                    stringBuilder.AppendLine(Context);

                if (Data is Exception exception
                        && !String.IsNullOrWhiteSpace(exception.Message))
                {
                    stringBuilder.AppendLine(exception.Message.Trim());
                    if (!(exception.StackTrace is null))
                        content = exception.StackTrace
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    else content = null;
                }
                
                if (content is IEnumerable<string> strings)
                    content = string.Join(Environment.NewLine,
                        strings.Where(line => !string.IsNullOrWhiteSpace(line)));
                
                content = content?.ToString().Trim();
                if (!String.IsNullOrEmpty((string)content))
                    stringBuilder.Append(content);

                return stringBuilder.ToString().Trim();
            }
        }
    }
}