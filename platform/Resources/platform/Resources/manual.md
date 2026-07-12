# Description

The workspace project provides a portable, file-based working environment with a
modular structure for developers and users. It allows tools, applications and
services to run within a workspace using predefined paths and configurations,
while attempting to avoid changes to the host system.

Stored on a virtual disk, the workspace can be attached, detached and
transferred between Windows installations on different machines. A Windows-based
abstraction layer handles the mounting and management of this disk, allowing all
operations to take place within the logically isolated workspace. Path,
configuration and environment separation are used to maintain consistent runtime
conditions and reduce unintended interaction with the host file system and
registry.

__The Workspace project consists of the platform, launcher, startup and
inventory components. The platform manages the workspace lifecycle, including
virtual disk creation, attachment, detachment and maintenance. It creates a
configured, ready-to-use workspace containing the launcher, startup and
inventory tools, including the required directory structure and configuration.
The launcher provides keyboard-optimized access to workspace applications, the
startup tool executes workspace initialization scripts, and the inventory tool
analyzes file system and Windows registry changes to support the creation of
portable applications. All three tools are standalone applications and can also
be used independently.

BitLocker-encrypted virtual disks are supported for environments where
encryption requirements apply.

# Usage

The workspace is controlled through the platform command line application. The
same tool is used for creating, managing and using workspaces.

The command syntax is:

```text
workspace.exe <drive>: [create|attach|detach|compact|shortcuts]
```

After renaming the application, the workspace name is used for the executable,
configuration and virtual disk files.

## 1. Creating a new workspace

Create or select a directory for the workspace files.

Copy `workspace.exe` into this directory and rename it to the desired
workspace name.

Example:

```text
ren workspace.exe example.exe
```

Create the workspace named _example_ for drive __B:__:

```text
example.exe B: create
```

A VHD/VHDX virtual disk is created in the current directory using the name of
the platform application. The virtual disk uses the same name.

After creation, the workspace contains a predefined directory structure. This
structure can be extended or modified according to the requirements of the
workspace.

When the workspace is attached, `Startup.cmd` in the root directory of the
workspace is executed. This file initializes the workspace environment and can
start applications or services.

## 2. Creating shortcuts

This step is optional.

Create shortcuts for the workspace on drive __B:__:

```text
example.exe B: shortcuts
```

The command creates shortcuts for attaching, detaching and compacting the
workspace. The application name is used for the shortcut names.

Examples:

```text
example.attach.lnk
example.detach.lnk
example.compact.lnk
```

## 3. Attaching the workspace

Attach the workspace as drive __B:__:

```text
example.exe B: attach
```

When the workspace is attached, `Startup.cmd` is executed. It can configure
the workspace environment and start required applications or services.

Applications started through the workspace inherit the configured environment
variables.

## 4. Using applications and services

The workspace is a virtual disk with its own drive letter and file system.
Application data and user-specific files can be stored inside the workspace.

Applications can be installed normally inside the workspace. Portable versions
can also be used if available.

Possible sources for portable applications include:

- [PortableApps.com](https://portableapps.com)
- [portapps.io](https://portapps.io)

The launcher integrated into the workspace provides keyboard-based access to
applications. Applications started from the launcher inherit the environment
variables of the workspace.

Additional applications and services can be started and configured using
`Startup.cmd`.

## 5. Stopping and detaching the workspace

Before detaching the workspace, applications and services started from the
workspace should be closed.

The launcher can be used to initiate the detach operation. It keeps the
workspace environment available while applications started from it are being
terminated.

The workspace can also be detached using the platform command:

```text
example.exe B: detach
```

It can also be called from inside the workspace:

```text
%PLATFORM_APP% %HOMEDRIVE% detach
```

During detachment, `Startup.cmd` is called with the parameter `exit`.
Applications started from the workspace are requested to terminate before the
virtual disk is detached.

## 6. Workspace configuration

Additional workspace-specific settings can be defined using the settings
component.

The settings component supports:
- key-value definitions
- environment variables
- placeholders
- file replacement based on placeholders

The configuration file is stored outside the workspace and is processed each
time the workspace is attached.

Details are described in the workspace INI file.

## 7. Compacting the workspace

The workspace uses a dynamically sized virtual disk. The file grows during use
but does not automatically release unused space.

To compact the virtual disk:

1. Detach the workspace.
2. Run:

   ```text
   example.exe B: compact
   ```

During this process, the virtual disk is attached without executing
`Startup.cmd`.

If a temporary directory exists in the workspace root, its contents are removed
before the virtual disk is compacted.

## 8. Logging

The user interface provides only basic output. Additional information is written
to log files for troubleshooting and diagnostics.

## 9. Tips

Deactivate automatic playback for removable media in Windows. This prevents
erroneous error messages from Windows File Explorer when mounting virtual disks.

- Settings - Bluetooth & devices
  - Use AutoPlay for all media and devices: __Off__
  - Choose AutoPlay defaults
    - Removable drive: __Take no action__
