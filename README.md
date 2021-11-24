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

__The project includes with [platform](platform), a tool for the initial
creation, use and management of the virtual environment and [modules](modules)
that then automatically download tools and programs from the Internet,
configure them and integrated them in the virtual environment.__

__In the upcoming release 3.0.0 the modules are not yet supported. The software
in the virtual environment must be set up manually, but this is easy, which is
a normal drive.__

Complete environments can be several gigabytes in size and not all tools,
programs and services are always needed and so everyone can decide for
themselves and customize the environment.

From my own experience from large companies with strict use of BitLocker, this
is also supported :-)

__What is the difference with PortableApps.com or portapps.io?__

The virtual environment focuses on the virtual drive as a platform. It is about
the advantages that the platform can be used as a single file and programs and
services can be used in it with a complete configuration and with reliable
absolute paths.

The integration and distribution of portable applications are not the ambition
of this project.

The use of modules for the integration of programs and services is planned, but
is more an exemplification of the possibilities for the integration of programs
and services. However, it is not the intention of the project to establish a
corresponding eco-system or repository.

PortableApps.com and portapps.io complement the virtual environment perfectly
and both release very good portable versions of programs that can be used in
the virtual environment.


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

When the virtual environment is created in the form of the vhdx file, it can be used as follows.

`usage: platform.exe A-Z: [create|attach|detach|compact|shortcuts]  `

Example
- `seanox.exe B: create` to create the initial environment as vhdx
- `seanox.exe B: shortcuts` to create the usual calls as shortcuts
- `seanox.exe B: attach` to attach the environment
- Configure __Startup.cmd__ and the desired programs
- `seanox.exe B: detach` to detach the environment
- `seanox.exe B: compact` to shrink the environment

__Module integration will come later, but will be similar.__


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