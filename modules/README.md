# About Modules
Modules are the technique with which programs, services and tools are
integrated in the virtual environment.

They are primarily instructions with optional extensions for automated
integration of programs, services and tools from the Internet. But they can
also be complete programs, services and tools.

Modules have a defined directory structure. The `module.yaml` file is required,
everything else is optional.

```
+ <module>
    + install            Optional content that is copied to the central installation directory
    + data               Optional content to be copied to the destination directory
    - module.yaml        Assembly instructions for program, service or tool     
```

A central install directory `/Install` with sub-directories for the different
modules is the idea that configurations, scripts and templates for a program,
services and tools have a defined place outside the application directory. This
makes it easier to remove the application directory, e.g. for an update, and
simply add the contents of the install directory to a new version.

The _data_ directory is an optional addition to the download or can replace it.
Complete program or only specific files and file structures can be stored here,
which are copied to the destination directory after the download. This step is
executed after the download, but is independent of whether a download has been
defined.

The `module.yaml` file describes how a program, service or tool is assembled,
the required dependencies, where the data comes from, how it is configured and
integrated into the environment.

```yaml
module:           # Configuration of the module
    description:  # Description is used only for understanding the module

    depends:      # Value or list of dependent modules which are loaded before
    download:     # URL for download; 7z, zip, msi are automatically unpacked into the destination
    source:       # Directory different from the download, whose content is copied to the destination
    destination:  # Destination directory of the module, default is /Program Portable/<module>
    script:       # JavaScript that is executed after downloading and copying
                  # Script can be classified: initial (before all), immediate (with module), final (after all)
                  # Script can be code or function that is passed a meta-object of the module.
    configure:    # Path or list of paths of destination files where placeholders are replaced

    commons:      # Batch script with common commands 
    attach:       # Batch script for initialization, will be executed after common
    startup:      # Batch script after initialization for program starts
    detach:       # Batch script when exiting the environment

    meta:         # Additional attributes
                  # Attributes are available with #[workspace.modules] and #[module.meta].
                  # Among other things, they are used for the configuration of the launcher.

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
- Copying files from the source directory to the destination directory, if no download has been defined
- Copying additional content from the _data_ directory to the destination directory
- Copying additional content from the _install_ directory to the central install directory
- Execution of the assembly script
- Filling the placeholders in the files specified as _prepare_   
If steps are not present or not configured, they are skipped.

The following placeholders are supported and replaced if the files are declared in _prepare_:

| Placeholder                                    | Description                                                                        |
| ---------------------------------------------- | ---------------------------------------------------------------------------------- |
| `#[workspace.drive]`                           | Drive letter of the workspace                                                      |
| `#[workspace.drive.number]`                    | Number of the workspace drive in the drive list                                    |
| `#[workspace.drive.file]`                      | Path of the virtual disk from the workspace                                        |
| `#[workspace.proxy]`                           | Proxy, if one has been defined                                                     |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[workspace.drive.directory]`                 | Path of the drive-directory in the workspace                                       |
| `#[workspace.modules.directory]`               | Path of the modules-directory in the workspace                                     |
| `#[workspace.platform.directory]`              | Path of the platform-directory in the workspace                                    |
| `#[workspace.startup.directory]`               | Path of the startup-directory in the workspace                                     |
| `#[workspace.temp.directory]`                  | Path of the temp-directory in the workspace                                        |
| `#[workspace.workspace.directory]`             | Path of the workspace-directory in the workspace                                   |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[workspace.environment.drive]`               | Drive letter of the temporary environment from the workspace                       |
| `#[workspace.environment.directory]`           | Root path of the temporary environment from the workspace                          |
| `#[workspace.environment.database.directory]`  | Path of the database-directory of the temporary environment from the workspace     |
| `#[workspace.environment.documents.directory]` | Path of the documents-directory of the temporary environment from the workspace    |
| `#[workspace.environment.install.directory]`   | Path of the install-directory of the temporary environment from the workspace      |
| `#[workspace.environment.programs.directory]`  | Path of the programs-directory of the temporary environment from the workspace     |
| `#[workspace.environment.resources.directory]` | Path of the resources-directory of the temporary environment from the workspace    |
| `#[workspace.environment.temp.directory]`      | Path of the temp-directory of the temporary environment from the workspace         |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[module.name]`                               | Name of the module                                                                 |
| `#[module.directory]`                          | Directory of the module                                                            |
| `#[module.destination]`                        | Destination directory of the module with the drive letter of the workspace         |
| `#[module.environment]`                        | Destination directory of the module with the drive letter of the final environment |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[environment.name]`                          | Name of final environment, is also used as the label from the drive                |
| `#[environment.version]`                       | Version of final environment, is used as the label from the drive                  |
| `#[environment.drive]`                         | Drive letter of the final environment                                              |
| `#[environment.size]`                          | Virtual Disk: Size in megabytes                                                    |
| `#[environment.type]`                          | Virtual Disk: Type `expandable` or `fixed`                                         |
| `#[environment.style]`                         | Virtual Disk: Partition style `MBR` or `GPT`                                       |
| `#[environment.bitlocker]`                     | Virtual Disk: Flag for using BitLocker                                             |
| `#[environment.compress]`                      | Virtual Disk: Flag for file system compression                                     |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[environment.database]`                      | Name of the  database-directory                                                    |
| `#[environment.documents]`                     | Name of the  document-directory                                                    |
| `#[environment.install]`                       | Name of the  install-directory                                                     |
| `#[environment.programs]`                      | Name of the  programs-directory                                                    |
| `#[environment.resources]`                     | Name of the  resources-directory                                                   |
| `#[environment.temp]`                          | Name of the  temp-directory                                                        |
| &nbsp;                                         | &nbsp;                                                                             |
| `#[environment.directory]`                     | Root path of the final environment                                                 |
| `#[environment.database.directory]`            | Path of the database-directory of the final environment                            |
| `#[environment.documents.directory]`           | Path of the documents-directory of the final environment                           |
| `#[environment.install.directory]`             | Path of the install-directory of the final environment                             |
| `#[environment.programs.directory]`            | Path of the programs-directory of the final environment                            |
| `#[environment.resources.directory]`           | Path of the resources-directory of the final environment                           |
| `#[environment.temp.directory]`                | Path of the temp-directory of the final environment                                |

More can be added in `creator.yaml`.
