<p>
  <a href="https://github.com/seanox/virtual-environment-creator/pulls">
    <img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge">
  </a>  
  <a href="https://github.com/seanox/virtual-environment-creator/issues">
    <img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge">
  </a>
  <a href="http://seanox.de/contact">
    <img src="https://img.shields.io/badge/support-active-green?style=for-the-badge">
  </a>
</p>


# Description
Since about 2010, the project exists for a modular platform for Windows to
create virtual environments. Based on a virtual drive, the environment can be
started easily and provides developers with a completely preconfigured
development environment with numerous tools, services and  programs and
standardizes the toolset in a development team. Short setup times, uniform
tools with uniform configuration, uniform paths in the file system, centralized
maintenance and easy distribution and updating are some of the benefits. The
platform is easily customizable, can be quickly switched to use for different
projects, and the environment can be easily transferred to other machines where
work started can be easily continued.

__The project includes [Platform](platform), a tool for the initial creation,
use and management of the virtual environment and modules that then
automatically download tools and programs from the Internet, configure them and
integrated them in the virtual environment.__

Complete environments can be several gigabytes in size and not all tools,
programs and services are always needed and so everyone can decide for
themselves and customize the environment.

From my own experience from large companies with strict use of BitLocker, this
is also supported :-)

__What is the project status?  
The platform tool is ready and is already being used productively. The
implementation of the modules is still in progress.__


## Advantages
- A virtual drive is used, which contains all data in one file.
- The drives can also be supplied and used via the network.
- Only one large file can be copied faster and also shared.
- Snapshots and versioning are possible.
- Multiple drives with different environments can be used in parallel on one computer. 
- Fast switching between different drives and environments is possible.
- Fixed drive letters and paths are used.
- The use of the file system and registry from the host is avoided.
- Environments can be maintained and distributed centrally.
- A team use the same environment with the same paths and configurations, which facilitates automation.


## List of available modules 
https://github.com/seanox/virtual-environment/tree/main/modules

More applications can be easily added for the creator or even later in the
final virtual environment.

Good sources for portable apps are:
- https://portableapps.com/apps
- https://portapps.io/apps

Often there is also software directly from the manufacturer/vendor as a
portable version.


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
- .NET 4.7.x or higher


# Downloads
Coming soon


# Usage
- Download the release or [platform.exe](https://github.com/seanox/virtual-environment/raw/main/platform/Platform.exe)
- Rename __platform.exe__ to the name that will be used for the environment and drive
- Call e.g. `platform.exe B: create` to create the initial environment as vhdx
- Call e.g. `platform.exe B: attach` to attach the environment
- Configure __Startup.cmd__ and the desired programs
- Call e.g. `platform.exe B: detach` to detach the environment
- Call e.g. `platform.exe B: compact` to shrink the environment

Note: Module integration will come later, but will be similar.


# Changes (Change Log)
[Read more](https://raw.githubusercontent.com/seanox/virtual-environment-creator/master/CHANGES)


# Contact
[Issues](https://github.com/seanox/virtual-environment-creator/issues)  
[Requests](https://github.com/seanox/virtual-environment-creator/pulls)  
[Mail](http://seanox.de/contact)


# Thanks!
<img src="https://raw.githubusercontent.com/seanox/seanox/master/sources/resources/images/thanks.png">

[cantaa GmbH](https://cantaa.de/)  
[JetBrains](https://www.jetbrains.com/?from=seanox)  
Sven Lorenz  
Andreas Mitterhofer  
[novaObjects GmbH](https://www.novaobjects.de)  
Leo Pelillo  
Gunter Pfannm&uuml;ller  
Annette und Steffen Pokel  
Edgar R&ouml;stle  
Michael S&auml;mann  
Markus Schlosneck  
[T-Systems International GmbH](https://www.t-systems.com)