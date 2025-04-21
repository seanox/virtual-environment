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
using System.Text;

namespace VirtualEnvironment.Startup
{
    internal static class Messages
    {
        private static readonly List<ISubscriber> Subscriptions;
        
        static Messages()
        {
            Subscriptions = new List<ISubscriber>();
        }
        
        internal static void Subscribe(ISubscriber recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            if (!Subscriptions.Contains(recipient))
                Subscriptions.Add(recipient);
        }

        internal static void Unsubscribe(ISubscriber recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException();
            if (Subscriptions.Contains(recipient))
                Subscriptions.Remove(recipient);
        }
        
        internal interface ISubscriber
        {
            void Receive(Message message);
        }

        internal static void Push(params Message[] messages)
        {
            foreach (var recipient in Subscriptions)
                foreach (var message in messages)
                    recipient.Receive(message);
        }
        
        internal static void Push(Type type, string content)
        {
            Push(new Message(type, content));
        }

        internal static void Push(Type type, string content, bool exit)
        {
            Push(new Message(type, content, exit));
        }

        internal enum Type
        {
            Error,
            Warning,
            Trace,
            Text,
            Message,
            Exit
        }

        internal readonly struct Message
        {
            internal Type Type { get; }
            internal string Content { get; }
            internal bool Exit { get; }

            internal Message(Type type, string content)
            {
                Type = type;
                Content = content;
                Exit = false;
            }
            
            internal Message(Type type, string content, bool exit)
            {
                Type = type;
                Content = content;
                Exit = exit;
            }
            
            public override string ToString()
            {
                var stringBuilder = new StringBuilder(Type.ToString().ToUpper());
                if (!string.IsNullOrWhiteSpace(Content))
                    stringBuilder.Append($": {Content.Trim()}");
                if (Exit)
                    stringBuilder.Append(" (exit)");
                return stringBuilder.ToString();
            }
        }
    }
}