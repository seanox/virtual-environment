// LICENSE TERMS - Seanox Software Solutions is an open source project,
// hereinafter referred to as Seanox Software Solutions or Seanox for short.
// This software is subject to version 2 of the Apache License.
//
// Virtual Environment Inventory
// Scans and extracts changes in the file system and registry.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Inventory
{
    internal static class Program
    {
        private static readonly Regex COMMAND_PATTERN = new Regex(
            @"^(depth)(?::(\d{1-8}))?$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        
        // Standard depth is based on this path:
        // C:\Users\<Account>\AppData\Local\Packages\<Application>
        private const int SCAN_DEPTH_DEFAULT = 6;

        static Program()
        {
            // Set the default culture to "en-US" to ensure consistent
            // formatting and parsing of data types, such as numbers (decimal
            // point ".") and dates (MM/dd/yyyy format), regardless of system or
            // regional settings.
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        }
        
        [STAThread]
        private static void Main(string[] arguments)
        {
            var applicationPath = Assembly.GetExecutingAssembly().Location;

            try
            {
                Messages.Subscribe(new Subscription());
                
                var depth = SCAN_DEPTH_DEFAULT;
                if (arguments.Length > 0)
                    if (!COMMAND_PATTERN.Match(arguments[0]).Success)
                        throw new InvalidUsageException();
                    else depth = Convert.ToInt32(COMMAND_PATTERN.Match(arguments[0]).Groups[1]);

                Scanner.Scan(depth);
            }
            catch (InvalidUsageException exception)
            {
                Messages.Push(Messages.Type.Exit,
                    data:$"usage: {Path.GetFileName(applicationPath)} [scan:depth]");
            }
            catch (Exception exception)
            {
                var content = $"{exception.GetType().Name} {exception.Message.Trim()}"
                        + $"{Environment.NewLine}{exception.StackTrace}";
                Messages.Push(Messages.Type.Error, content);
            }
        }

        private class InvalidUsageException : Exception
        {
        } 
        
        private class Subscription : Messages.ISubscriber
        {
            private string _context;
            
            private bool _continue;

            private static readonly HashSet<Messages.Type> MESSAGE_TYPE_LIST = new HashSet<Messages.Type>()
            {
                Messages.Type.Error,
                Messages.Type.Warning,
                Messages.Type.Trace,
                Messages.Type.Verbose,
                Messages.Type.Exit
            };

            public void Receive(Messages.Message message)
            {
                if (!MESSAGE_TYPE_LIST.Contains(message.Type)
                        || message.Data is null)
                    return;
                
                // VERBOSE is extended information that lies between TRACE and
                // DEBUG. The message have its own context, as otherwise the
                // information cannot be placed in any context.
                if (Messages.Type.Verbose == message.Type)
                    if (String.IsNullOrWhiteSpace(_context)
                            || String.IsNullOrWhiteSpace(message.Context)
                            || message.Context != _context)
                        return;
                
                // VERBOSE is logged as an extension of TRACE and is therefore
                // converted to TRACE so that logging can pick up the previous
                // context of TRACE and continue the logging block. 
                if (Messages.Type.Verbose == message.Type)
                    message = message.ConvertTo(Messages.Type.Trace);
                
                var content = message.ToString().Trim();
                if (String.IsNullOrWhiteSpace(content))
                    return;

                try
                {
                    if (!_continue)
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
                        var version = assembly.GetName().Version;
                        var build = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(attribute => attribute.Key == "Build")?.Value;
                        var banner = new StringBuilder()
                            .AppendLine(String.Format("Seanox Inventory [{0} {1}]", version, build))
                            .AppendLine($"{copyright.Replace("©", "(C)")}")
                            .ToString();
                        Console.WriteLine(banner);

                        if (Messages.Type.Exit == message.Type)
                            Console.WriteLine(Convert.ToString(message.Data));
                        return;
                    }
                    
                    _continue = true;

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var lines = message.ToString()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !String.IsNullOrWhiteSpace(line))
                        .ToArray();
                    
                    Action<string, bool> consoleWriteLine = (line, followup) =>
                    {
                        line = followup ? $" ...  {line}" : line;
                        Console.WriteLine($"{timestamp} {line}");
                    };

                    if (lines.Length > 0)
                    {
                        if (lines[0] != _context)
                            consoleWriteLine(lines[0], false);
                        _context = lines[0];
                        for (var index = 1; index < lines.Length; index++)    
                            consoleWriteLine(lines[index], true);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}