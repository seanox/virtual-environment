# Startup
Startup is a small background launcher for batch scripts.

The program looks for a batch file with the same name and the `.cmd` extension
in the current working directory or in the program directory. It starts this
batch script in a hidden console window and waits until the script has finished.

Renaming the Startup executable also changes the expected name of the batch
script.

Command-line arguments are supported and passed through to the batch script.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
Startup is [part of the virtual environment](../platform) but can also be
downloaded and used separately.

https://github.com/seanox/virtual-environment/releases/latest

# Changes 
## 1.3.0 20250701  
BF: StartUp: Optimization and corrections  
CR: StartUp: Optimization  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/startup/CHANGES)
