# Startup
Startup is a simple background launcher for batch scripts.

The program expects in the current working directory or in the program directory
a batch file of the same name with the file extension cmd that it starts with a
hidden console window and then waits until the end of the batch script.

Renaming the startup program also changes the expected name of the batch script.

Command-line arguments are supported and passed to the batch script.

# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8.x or higher (for runtime)
- [Microsoft .NET 4.8.x Developer Pack or higher](
      https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (for development only)

# Download
Startup is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform/Resources/platform/Programs/Platform)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/releases

# Changes 
## 1.2.3 20240302  
CR: Project: Updated TargetFrameworkVersion to v4.8  
CR: Platform: Refactoring of the standard directory structure  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment/master/startup/CHANGES)
