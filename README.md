<p>
  <a href="pulls" title="Open for contributions, ideas and enhancements"
    ><img src="https://img.shields.io/badge/development-active-green?style=for-the-badge"
  ></a>
  <!--
  <a href="pulls" title="Development is waiting for contributions, ideas and enhancements"
    ><img src="https://img.shields.io/badge/development-passive-blue?style=for-the-badge"
  ></a>
  -->
  <a href="issues" title="Open for issues and feature requests"
    ><img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge"
  ></a>
  <a href="http://seanox.de/contact" title="Contact for questions and support"
    ><img src="https://img.shields.io/badge/support-active-green?style=for-the-badge"
  ></a>
</p>

# Description
This project provides a virtual environment based on a modular structure. It
enables developers and users to work within a pre-configured environment that
contains tools, programs and services without modifying the host system or
requiring additional virtualization software.

The environment is stored on a virtual drive that can be attached, detached and
moved between systems. It operates on an abstraction layer on top of Windows
that mounts this virtual drive. All operations take place inside this file-based
environment, which is logically separated through path and configuration
isolation and is designed to avoid interaction with the host file system and
registry.

__The project consists of the [platform](platform), the [launcher](launcher) and
the [startup tool](startup). The platform manages virtual drives (creation,
attachment, detachment, maintenance). The launcher provides keyboard-based
access to programs inside the environment, and the startup tool initializes
services and applications. A [module concept](modules) for integrating external
tools exists as a proof of concept but is not the current focus of
development.__

BitLocker-encrypted virtual drives are supported for environments where
encryption requirements apply.

__Difference to [PortableApps.com](https://portableapps.com) and [portapps.io](
    https://portapps.io)__

This project focuses on the virtual drive as the execution platform.
Applications and services run inside the virtual drive with defined paths and
configurations. The goal is not to provide portable applications, but to offer a
complete, isolated environment that can be used as a single file.

[PortableApps.com](https://portableapps.com) and [portapps.io](
    https://portapps.io) can be used within the virtual environment, but the
project does not aim to provide an application repository or an ecosystem for
portable software.

## Advantages
- Virtual drive as a single file containing the entire environment
- Usable from local storage, external drives or network locations
- Single-file structure simplifies copying, transferring and sharing
- Snapshots and versioning supported by Windows VHD/VHDX mechanisms
- Multiple environments attachable in parallel on the same system
- Switching between environments by attaching/detaching virtual drives
- Consistent paths with freely selectable drive letters
- Host file system and registry usage avoided by operating inside the virtual
  drive
- Centralized maintenance by distributing updated virtual drive files
- Identical paths and configurations for teams, enabling reproducible automation
  workflows

# Features
- Support for VHD and VHDX virtual drives, including optional BitLocker
  encryption
- Commands to create, attach, detach, manage and compact the virtual environment
- Predefined structure that makes the environment usable immediately after
  creation
- Integrated launcher with keyboard-based navigation for accessing programs
- Configuration of environment and applications through a separate key-value
  file
- Platform implemented with a small footprint and minimal resource usage
- Operation inside the virtual drive to avoid using the host file system and
  registry
- Customization through configuration files and startup scripts
- Centralized distribution by providing updated virtual drive files
- Consistent paths and configurations enabling reproducible automation workflows

# License Terms
Seanox Software Solutions is an open-source project, hereinafter referred to as
__Seanox__.

This software is licensed under the __Apache License, Version 2.0__.

__Copyright (C) 2025 Seanox Software Solutions__

Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at

https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
CONDITIONS OF ANY KIND, either express or implied. See the License for the
specific language governing permissions and limitations under the License.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for
  development only)

# Downloads
- [Seanox Virtual Environment 3.7.0](
      releases/download/3.7.0/seanox-platform-3.7.0.zip
)  
- [Seanox Virtual Environment 3.7.0 Update](
      releases/download/3.7.0/seanox-platform-3.7.0-update.zip) for an existing
  environment

# Usage
1. Download the last release of [seanox-platform.zip](releases/latest)
2. Extract the archive to any location in the local file system.
3. Rename __`platform.exe`__ to the name that will be used for the environment
   and drive

__Then the program can be used as follows:__

```
usage: platform.exe A-Z: [create|attach|detach|compact|shortcuts]
```

## Example
- `platform.exe B: create`  
  Creates the initial environment as a VHDX virtual drive.
- `platform.exe B: shortcuts`  
  Creates shortcut files for common actions.
- `platform.exe B: attach` to attach the environment
  Attaches the environment and makes it available as a virtual drive.

Configure __`Startup.cmd`__ in the root directory of the virtual environment and
add the desired programs and services. It is recommended to use a launcher so
that the environment variables are available to the called programs. Detach
should also be started via the launcher if programs and services are terminated
when detaching and the environment variables are needed for this.

- `platform.exe B: detach`  
  Detaches the environment.
- `platform.exe B: compact`  
  Detaches the environment.

<img src="assets/usage.gif"/>

__Module integration will follow later and will be similar.__

# Example Environment (Ready-to-Use Template)
A complete, ready-to-use virtual environment is provided as a template. It
contains a fully configured development setup with tools for AWS, Kubernetes,
Terraform, Java, Python, Node.js, a customized Eclipse installation, a
PostgreSQL database including pgvector, and additional utilities.

## Download (approx. 5 GB, last update 2025-12-21)
- https://seanox.com/storage/master-3.7.0.7z  
- https://seanox.com/storage/master-proxy-3.7.0.7z

### Starting the environment
1. Extract the archive.
2. Start __`master.exe B: attach`__ to mount the environment.
3. The launcher can be opened with the host key combination __Win + ESC__.
4. To exit the environment, use the **Detach** button in the launcher.

The environment is immediately usable after attaching. All tools and
configurations are already included inside the virtual drive.

<img src="assets/example.gif"/>

### Using the template as a base for your own environment (optional)

If you want to create your own environment based on the template:

1. Rename __`master.exe`__, __`master.ini`__ and __`master.vhdx`__ to the
   desired environment name.
2. (Optional) Change the virtual disk label (disk properties).
3. (Optional) Adjust the volume name in __`AutoRun.inf`__.
4. Modify __`Startup.cmd`__ inside the virtual drive to start your own tools and
   services.

# Changes
## 3.7.0 20251207  
__Version 3.6.0 contained outdated components due to a merge issue.__    
__The release was withdrawn and replaced by version 3.7.0.__  

BF: Review: Optimization and corrections  
BF: Settings: Correction to ignore invalid paths in section [FILES]  
BF: Platform: Optimization and corrections  
BF: Platform: Names of environment variables are case-sensitive  
BF: StartUp: Optimization and corrections  
BF: Launcher: Correction/optimization  
BF: Launcher: Correction when using custom scaling  
CR: Platform: Omission of the steps recorder (deprecated by Microsoft)  
CR: Platform: Simplification of the terminal action  
CR: Platform: Optimization and corrections  
CR: StartUp: Optimization  
CR: Launcher: Optimization and corrections  
CR: Launcher: Added additional environment variables for the tile action  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/main/CHANGES)

# Contact
[Issues](issues)  
[Requests](https://github.com/seanox/virtual-environment/pulls)  
[Mail](https://seanox.com/contact)
