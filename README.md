<p>
  <a href="https://github.com/seanox/portable-development-environment/pulls">
    <img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge">
  </a>  
  <a href="https://github.com/seanox/portable-development-environment/issues">
    <img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge">
  </a>
  <a href="http://seanox.de/contact">
    <img src="https://img.shields.io/badge/support-active-green?style=for-the-badge">
  </a>
</p>


# Description

Since about 2010, the project exists for a modular platform for Windows to
create portable development environments. Based on a virtual drive, the
environment can be started easily and provides developers with a completely
preconfigured development environment with numerous tools, services and
programs and standardizes the toolset in a development team. Short setup times,
uniform tools with uniform configuration, uniform paths in the file system,
centralized maintenance and easy distribution and updating are some of the
benefits. The platform is easily customizable, can be quickly switched to use
for different projects, and the environment can be easily transferred to other
machines where work started can be easily continued.

This project provides the environment with the necessary tools. The programs,
tools and services are not included but for many there are ready-made
configurations and automated helpers so that they can be easily loaded from the
Internet and integrated.

Complete environments can be several gigabytes in size and not all  tools,
programs and services are always needed and so everyone can decide for
themselves and customize the environment.

From my own experience from large companies with strict use of BitLocker, this
is also supported :-)

__The project provides a tool for initial creation of a pre-configured
environment with some tools and programs. Afterwards, the environment can be
maintained, configured and extended independently without this tool.__

## Advantages
- A virtual drive is used, which contains all data in one file.
- The drives can also be used over the network.
- The drives can also be supplied and used via the network.
- Only one large file can be copied faster and also shared.
- Snapshots and versioning are possible.
- Multiple drives with different environments can be used in parallel on one computer. 
- Fast switching between different drives and environments is possible.
- Fixed drive letters and paths are used.
- The use of the file system and registry from the host is avoided.
- Environments can be maintained and distributed centrally.
- Users all use the same environment with the same paths and configurations, which makes automation easier.

## List of available modules 
https://github.com/seanox/portable-development-environment/tree/main/modules

__Project is in development.__


# Licence Agreement
LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
Folgenden Seanox Software Solutions oder kurz Seanox genannt.

Diese Software unterliegt der Version 2 der Apache License.

Copyright (C) 2021 Seanox Software Solutions

Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
CONDITIONS OF ANY KIND, either express or implied. See the License for the
specific language governing permissions and limitations under the License.


# System Requirement
- Windows 7 or higher as operating system
- Node.js 14.x or higher


# Downloads

# Usage

- Download the release
- Configure the target environment in `creator.yaml`
- Use the command line and go to the root directory of the project
- Call `npm install` to initialize the runtime environment
- Call `node creator.js` to create a Portable Development Environment
- Target environment is created in the directory `./workspace`

# Changes (Change Log)

# Contact

# Thanks!