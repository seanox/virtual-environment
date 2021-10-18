# About Modules
Modules are the technique with which programs, services and tools are
integrated in the portable development environment.

They are primarily instructions with optional extensions for automated
integration of programs, services and tools from the Internet. But they can
also be complete programs, services and tools.

Modules have a defined directory structure. The `module.yaml` file is required,
everything else is optional.

```
- Module
    + Install
    + Module
    - module.yaml      
```

The `module.yaml` file describes how a module is assembled, the required
dependencies, where the data comes from, how it is configured and integrated
into the environment.

```yaml
module:           # Configuration of the module
    description:  # Description is used only for understanding the module

    depends:      # Value or list of dependent modules which are loaded before
    download:     # URL for download 
    source:       # Directory different from the download, whose content is copied to the destination
    destination:  # Destination directory of the module
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
