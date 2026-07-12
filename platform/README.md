# Platform

Platform creates and manages Workspace virtual disks based on the VHD and VHDX
formats. It provides commands to create, attach, detach and compact workspaces.

When creating a workspace, Platform generates the directory structure,
configuration and the included Workspace components (Launcher, Startup and
Inventory).

A module mechanism for integrating additional tools is implemented. No official
modules are currently available.

# Features
- Create VHD and VHDX workspaces
- Attach and detach workspaces
- Compact virtual disks
- Optional BitLocker encryption
- Generate the Workspace directory structure
- Create shortcuts for a workspace
- Configuration via INI files

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for
  development only)

# Download
Platform is [part of the workspace](..) but can also be downloaded and used
separately.

https://github.com/seanox/workspace/releases/latest

# Usage
1. Download the latest release of [seanox-workspace.zip](
       https://github.com/seanox/workspace/releases/latest)
2. Extract the archive to any location in the local file system.
3. Rename __`workspace.exe`__ to the name that will be used for the workspace
   and drive

__Then the program can be used as follows:__

```
usage: workspace.exe <drive>: [create|attach|detach|compact|shortcuts]
```

## Examples

```text
workspace.exe B: create
```
Creates a new workspace as a VHD/VHDX virtual disk to be used as drive __B:__.


```text
workspace.exe B: shortcuts
```
Creates shortcuts for the workspace on drive __B:__.

```text
workspace.exe B: attach
```
Attaches the workspace as drive __B:__.

Configure __`Startup.cmd`__ in the root directory of the workspace and add the
desired programs and services. It is recommended to use a launcher so that the
environment variables are available to the called programs. Detach should also
be started via the launcher if programs and services are terminated when
detaching and the environment variables are needed for this.

```text
workspace.exe B: detach
```
Detaches the workspace from drive __B:__.

```text
workspace.exe B: compact
```
Compacts the workspace virtual disk.

<img src="../assets/usage.gif"/>

__Module integration will follow later and will be similar.__

# Changes 
## 3.6.0 20241230  
BF: Review: Optimization and corrections  
BF: Settings: Correction to ignore invalid paths in section [FILES]  
BF: Platform: Names of environment variables are case-sensitive  
CR: Platform: Omission of the steps recorder (deprecated by Microsoft)  
CR: Platform: Simplification of the terminal action  

[Read more](https://raw.githubusercontent.com/seanox/workspace/master/platform/CHANGES)
