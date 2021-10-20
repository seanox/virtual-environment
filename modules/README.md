# About Modules
Modules are the technique with which programs, services and tools are
integrated in the portable development environment.

They are primarily instructions with optional extensions for automated
integration of programs, services and tools from the Internet. But they can
also be complete programs, services and tools.

Modules have a defined directory structure. The `module.yaml` file is required,
everything else is optional.

```
+ <module>
    + install            Optional file structure to be copied to the install directory
    + module             Optional file structure to be copied to the destination directory
    - module.yaml        Description of the assembly of the module                 
```

A central _Install_ directory with sub-directories for the different modules is
the idea that configurations, scripts and templates for a program, services and
tools have a defined place outside the application directory. This makes it
easier to remove the application directory, e.g. for an update, and simply add
the contents of the _Install_ directory to a new version.

The module directory is an optional addition to the download or can replace it.
Complete modules or only specific files and file structures can be stored here,
which are copied to the destination directory after the download. This step is
executed after the download, but is independent of whether a download has been
defined.

The `module.yaml` file describes how a module is assembled, the required
dependencies, where the data comes from, how it is configured and integrated
into the environment.

```yaml
module:           # Configuration of the module
    description:  # Description is used only for understanding the module

    depends:      # Value or list of dependent modules which are loaded before
    download:     # URL for download 
    source:       # Directory different from the download, whose content is copied to the destination
    destination:  # Destination directory of the module, default is /Modules/<module>
    script:       # JavaScript that is executed after downloading and copying
    prepare:      # Path or list of paths of destination files where placeholders are replaced

    commons:      # Batch script with common commands 
    attach:       # Batch script for initialization, will be executed after common
    detach:       # Batch script when exiting the environment
    
    control:      # Launcher configuration for this module

monitoring:       # Configuration of version monitoring 
    url:          # URL of the website where the version should be checked
    pattern:      # RegExp to check the version
```

The module integration uses the following automations and assumptions:
- If a module has already been integrated, it will be ignored when requested
  again. This can happen due to dependencies.
- If dependencies have been defined, they will be processed first.
- If there is a _module_ directory, the content will be copied to the
  destination directory after the download.
- If there is an _install_ directory it will be copied to `/Install/<Module>`.

The integration has a defined sequence:
- Integration of dependencies
- Downloading the program files
- Copying files from the download directory to the destination directory
- Copying additional files from the _module_ directory to the destination directory
- Filling the placeholders in the files specified as _prepare_   
If steps are not present or not configured, they are skipped.

The following placeholders are supported and replaced if the files are declared in _prepare_:

| Placeholder                          | Description                                                                        |
| ------------------------------------ | ---------------------------------------------------------------------------------- |
| `#[workspace.destination.directory]` | Temporary root path of the final environment during creation                       |
| `#[workspace.directory]`             | Root path of the workspace                                                         |
| `#[workspace.drive]`                 | Drive letter of the workspace                                                      |
| `#[workspace.drive.file]`            | Path of the virtual disk from the workspace                                        |
| `#[workspace.drive.number]`          | Number of the workspace drive in the drive list                                    |
| `#[workspace.drive.directory]`       | drive-directory of the workspace                                                   |
| `#[workspace.modules.directory]`     | modules-directory of the workspace                                                 |
| `#[workspace.platform.directory]`    | platform-directory of the workspace                                                |
| `#[workspace.startup.directory]`     | startup-directory of the workspace                                                 |
| `#[workspace.temp.directory]`        | temp-directory of the workspace                                                    |
| `#[workspace.proxy]`                 | Proxy, if one has been defined                                                     |
|  &nbsp;                              | &nbsp;                                                                             |
| `#[release.name]`                    | Name of final environment, is also used as the label from the drive                |
| `#[release.version]`                 | Version of final environment, is used as the label from the drive                  |
| `#[release.drive.bitlocker]`         | Virtual Disk: Flag for using BitLocker                                             |
| `#[release.drive.compress]`          | Virtual Disk: Flag for file system compression                                     |
| `#[release.drive.size]`              | Virtual Disk: Size in megabytes                                                    |
| `#[release.drive.style]`             | Virtual Disk: Partition style `MBR` or `GPT`                                       |
| `#[release.drive.type]`              | Virtual Disk: Type `expandable` or `fixed`                                         |
| `#[release.drive.letter]`            | Drive letter of the final environment                                              |
|  &nbsp;                              | &nbsp;                                                                             |
| `#[module.name]`                     | Name of the module                                                                 |
| `#[module.directory]`                | Directory of the module                                                            |
| `#[module.destination]`              | Destination directory of the module with the drive letter of the workspace         |
| `#[module.release.destination]`      | Destination directory of the module with the drive letter of the final environment |

            Workspace.removeVariable("module.directory")


More can be added in `creator.yaml`.