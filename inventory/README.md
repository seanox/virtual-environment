# Inventory
Analyzes and extracts modifications in the file system and the Windows registry.

The tool performs comparative scans of the system drive and the registry.  
Each scan produces hash?based data files that contain either full paths or
aggregated hash values, depending on the configured scan depth.

Up to the defined scan depth, all paths are written explicitly to the scan
files. For deeper structures, a hash value is generated that represents the
complete content of the respective subtree. The maximum usable scan depth is
determined by the operating system’s file system.

A subsequent scan produces a second set of scan files for both the file system
and the registry. By comparing the two scans, the tool identifies added and
modified data. These files are copied verbatim into an inventory directory.
Removed data is not recorded.

For file system copies, standard environment variables are used to normalize
paths, ensuring independence from drive letters and user-specific directories.
Registry modifications are stored in plain text in the file `registry.data`.

The resulting data supports the creation of portable applications by making
visible which files and registry entries are required and which components may
need abstraction. It also improves the traceability of system changes following
software installation.

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
