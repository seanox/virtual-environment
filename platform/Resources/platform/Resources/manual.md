# Introduction

Since about 2010, there has been a project for a virtual environment with a
modular structure targeting developers and users, enabling them to work in a
fully pre-configured environment with all programs, tools, and services—without
modifying the host environment or requiring additional dedicated virtualization
resources.

Short setup times, uniform tools with standardized configurations, consistent
file system paths, centralized maintenance, and easy distribution and updates
are just some of the benefits. The environment is highly customizable, can be
quickly adapted for different projects, and can be seamlessly transferred to
other machines to continue ongoing work.

The project includes a platform—a tool for the initial creation, use, and
management of the virtual environment—and a module concept for the automatic
integration and configuration of tools and programs from any source on the
Internet. The module concept is a successful proof of concept envisioned as a
possible future extension of the platform, but it is not currently the focus of
development.



# Usage

The program is initially started via the command line and serves as a central
tool for creating, managing, and starting the platform. Although the platform is
primarily used as a Windows application, initial control is provided through
command line commands.



## 1. Creating a New Environment

- Use a directory where the virtual environment will be created.
- Copy the `platform.exe` file into this directory.
- Rename `platform.exe` to the name of the virtual environment you want to create  
  e.g. `ren platform.exe seanox.exe`
- Create the virtual environment for drive B:  
  e.g., `seanox.exe B: create`

A VHDX file is created in the current directory with the name derived from the
platform program. The virtual drive also uses this name.

The drive can be mounted temporarily—either manually or using the platform
program. When you open the virtual drive, a recommended directory structure is
already present; however, it can be rearranged individually.

It is important to note that the platform program will run the `Startup.cmd`
located in the root directory when the virtual environment is started. Other
configurations may be modified as needed.



## 2. Creating Shortcuts for Common Tasks

This step is optional and convenient.

- Create shortcuts, for example:  
  `seanox.exe B: shortcuts`

This command creates shortcuts in the current directory for attaching,
detaching, and compacting the virtual environment. The file name of the platform
application is also used _seanox.attach.lnk_, _seanox.detach.lnk_ an
_seanox.compact.lnk_.



## 3. Attaching and Starting the Virtual Environment

- Start the virtual environment  
  e.g. `seanox.exe B: attach`

When attaching the virtual environment, `Startup.cmd` is executed. This process
installs, configures, and starts the environment along with its contained
programs. It is controlled exclusively via the environment variables of the
platform.



## 4. Installation of Programs and Services

The virtual environment is presented as a real drive with its own drive letter
and file system. By default, the user profile and application data are
redirected to the Settings directory in the root.

Programs can be installed in the usual manner; however, using portable versions
is recommended and generally simpler.

Good sources are:
- https://portableapps.com
- https://portapps.io

Many manufacturers also offer portable versions or portable usage of their
software.

With both attachment and detachment of the virtual environment, `Startup.cmd` is
invoked. This enables the environment to be configured and allows programs to be
installed, configured, started, stopped, or uninstalled— all driven by the
environment variables of the virtual platform.



## 5. Stopping and Detaching the Virtual Environment

If programs and services rely on the virtual platform’s environment variables,
they should also be started within the virtual environment. Detachment should be
performed in a manner that ensures all programs are properly terminated and,
if necessary, uninstalled.

__How does it work?__

When the virtual environment is attached, a child process is created that
inherits the specific environment variables of the virtual environment as its
context. This child process automatically executes `Startup.cmd` and launches a
dedicated launcher, which keeps the child process continuously active. All
programs within the virtual environment are then started via this launcher,
ensuring they run with the defined environment context.

Detachment can be initiated either via environment variables:

`%PLATFORM_APP% %HOMEDRIVE% detach`

Or by using the platform program

e.g. `seanox.exe B: detach`

When detaching, _Startup.cmd_ is called with the parameter 'exit' and then all
programs launched from the virtual environment are first terminated gracefully;
if necessary, a forced termination follows.

__About security__

__The platform is started with administrator privileges, although this
potentially carries an increased risk, it is quite common in development and
testing environments to run applications with administrative or elevated user
rights. Many tools and programs require these rights in order to fully utilize
all the necessary system functionalities. Nevertheless, the use of
administrative privileges should always be applied consciously—with appropriate
security measures in place within controlled environments—to minimize potential
risks as much as possible without unnecessarily hindering operations.__



## 6. Personalization of the Virtual Environment

Even though a virtual environment can be extensively configured, there are
scenarios where additional user, system, and environment data is required. For
this purpose, the virtual environment incorporates an INI-based settings
component. The INI file, located in the working directory of the virtual
environment, defines settings for environment variables, file system access,
registry management, and placeholder replacement within the environment. Each
section outlines structured rules to ensure consistent behavior when attaching
or detaching the environment.

More details and descriptions can be found in the INI file in the working
directory of the virtual environment.



## 7. Compacting the Virtual Environment

The virtual environment utilizes a virtual disk with a dynamic size. During use,
this file will grow, but it will not be automatically compacted. However, the
size of the virtual hard disk can be optimized manually.

- Stop and detach the virtual environment.
- Initiate the optimization process  
  e.g. `seanox.exe B: compact`

For optimization, the virtual disk is attached without executing _Startup.cmd_.
If a temp directory exists in the root directory, it is emptied. Subsequently,
the virtual disk is compacted by eliminating unused space.



## 8. Standard and Error Output

The user interface is minimalistic, reflecting a novel approach. Due to limited
space for extended explanations, a log file is generated. This log file contains
both error outputs and extended trace outputs, which assist in diagnostics.


## 9. Tips

Deactivate automatic playback for removable media in Windows. This prevents
erroneous error messages from Windows File Explorer when mounting virtual disks.

- Settings - Bluetooth & devices
  - Use AutoPlay for all media and devices: __Off__
  - Choose AutoPlay defaults
    - Removable drive: __Take no action__


__That's it -- have fun with it!__
