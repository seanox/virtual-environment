# Inventory
Analyzes changes in the file system and the Windows registry.

The tool performs two comparative scans of the system drive and registry. Each
scan creates data files containing either explicit paths or aggregated hash
values, depending on the configured scan depth.

Within the configured depth, file and registry paths are stored directly. For
deeper structures, a hash value represents the content of the respective
subtree. The maximum scan depth depends on the operating system file system.

A second scan creates another set of scan files. The tool compares both scan
results and identifies added and modified files, directories, and registry
entries. Removed data is not recorded.

Files detected during the comparison are copied unchanged into an inventory
directory.

File system paths are normalized using standard environment variables to avoid
dependencies on drive letters and user-specific directories. Registry changes
are stored as plain text in registry.data.

The generated inventory data can be used to identify files and registry entries
created or modified by software installations and to determine required
components for portable application setups.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
Inventory is [part of the workspace](
    https://github.com/seanox/workspace/tree/master/platform/Resources/platform/Programs/Platform)
but can also be downloaded and used separately.

https://github.com/seanox/workspace/releases

# Changes 
## 0.0.0 00000000  
NT: Coming soon

[Read more](https://raw.githubusercontent.com/seanox/workspace/master/inventory/CHANGES)
