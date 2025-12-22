# Platform
Platform is a tool for the initial creation, use and management of the virtual
environment. It also provides a module concept that can automatically download,
configure and integrate external tools and programs into the virtual
environment.

__The module concept is implemented and usable, but no modules have been
released yet due to limited maintenance capacity. Contributions are welcome.__

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for
  development only)

# Download
Platform is [part of the virtual environment](../platform) but can also be
downloaded and used separately.

https://github.com/seanox/virtual-environment/releases/latest

# Usage
1. Download the last release of [seanox-platform.zip](
       https://github.com/seanox/virtual-environment/releases/latest)
2. Extract the archive to any location in the local file system.
3. Rename __`platform.exe`__ to the name that will be used for the environment
   and drive

__Then the program can be used as follows:__

```
usage: platform.exe A-Z: [create|attach|detach|compact|shortcuts]  `
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

<img src="../assets/usage.gif"/>

__Module integration will follow later and will be similar.__

# Changes 
## 3.6.0 20241230  
BF: Review: Optimization and corrections  
BF: Settings: Correction to ignore invalid paths in section [FILES]  
BF: Platform: Names of environment variables are case-sensitive  
CR: Platform: Omission of the steps recorder (deprecated by Microsoft)  
CR: Platform: Simplification of the terminal action  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/platform/CHANGES)
