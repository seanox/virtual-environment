# Platform
Platform is a tool for the initial creation, use and management of the virtual
environment and modules that then automatically download tools and programs
from the Internet, configure them and integrated them in the virtual
environment.

__The module concept is implemented and usable, but no modules have been
released yet because there is currently not enough time to maintain the
packages -- your support is welcome :-)__

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
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
Platform is [part of the virtual environment](
   https://github.com/seanox/virtual-environment/tree/main/platform)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/releases

# Changes 
## 3.6.0 20241230  
BF: Review: Optimization and corrections  
BF: Settings: Correction to ignore invalid paths in section [FILES]  
BF: Platform: Names of environment variables are case-sensitive  
CR: Platform: Omission of the steps recorder (deprecated by Microsoft)  
CR: Platform: Simplification of the terminal action  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/platform/CHANGES)
