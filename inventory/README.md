# Inventory
Scans and extracts changes in the file system and registry.

The program analyzes changes in the file system of the system disk and the
Windows registry through comparative status snapshots. It creates hash-based
scan files that either contain full paths or aggregated hash values—depending on
the user-defined scan depth.

Up to the specified scan depth, paths are fully recorded in the scan files. For
deeper structures beyond this depth, a hash value is calculated that represents
their entire content. The maximum scan depth is limited by the file system of
the operating system.

A second scan again creates scan files for the file system and the registry. By
comparing the two scans, only modified and added data is extracted and stored as
an exact copy of the affected files in an inventory directory. Deleted data is
not recorded.

For the copy of the file system, standard environment variables are used for
paths, making them independent of drive letters and user accounts. Registry
changes are saved in plain text in registry.data.

The tool facilitates the creation of portable applications, as the recorded
changes make it clear which files and registry entries an application requires
and which may need to be abstracted. Additionally, it improves the traceability
of changes after a software installation.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
Inventory is [part of the virtual environment](
    https://github.com/seanox/virtual-environment/tree/main/platform/Resources/platform/Programs/Platform)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/releases

# Changes 
## 0.0.0 00000000  
NT: Coming soon

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/inventory/CHANGES)
