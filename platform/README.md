# Platform
Platform is a tool for the initial creation, use and management of the virtual
environment and modules that then automatically download tools and programs
from the Internet, configure them and integrated them in the virtual
environment.

__In the upcoming release 3.x the modules are not yet supported.__


# Usage
- Rename __platform.exe__ to the name that will be used for the environment and
  drive

When the virtual environment is created in the form of the VHDX file, it can be
used as follows.

`usage: platform.exe A-Z: [create|attach|detach|compact|shortcuts]  `

Example
- `seanox.exe B: create` to create the initial environment as VHDX
- `seanox.exe B: shortcuts` to create the usual calls as shortcuts
- `seanox.exe B: attach` to attach the environment

Configure __Startup.cmd__ in the root directory of the virtual environment and
add the desired programs and services. It is recommended to use a launcher so
that the environment variables are available to the called programs. Detach
should also be started via the launcher if programs and services are terminated
when detaching and the environment variables are needed for this.

- `seanox.exe B: detach` to detach the environment
- `seanox.exe B: compact` to compact the virtual disk

__Module integration will come later, but will be similar.__


# System Requirement
- Windows 10 or higher
- .NET 4.7.x or higher


# Download
Platform is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/tree/main/platform/Releases


# Changes (Change Log)
## 3.2.0 20220625 (summary of the current version)  
BF: Build: Correction of the release info process  
BF: Launcher: Correction of the behavior when the screen resolution changes  
BF: Platform: Existing shortcuts are now overwritten  
CR: Platform: Optimization when detaching / process termination  
CR: Platform: Integrated settings as a core component  
CR: Launcher: Scaling of icons depending on screen resolution (aesthetic reasons)  
CR: Launcher: Increase from the default value of OPACITY (95)  
CR: Launcher: Added option AutoScale (default true)  
CR: ShiftDown: Change the location to /Program Portables/ShiftDown  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/platform/CHANGES)
