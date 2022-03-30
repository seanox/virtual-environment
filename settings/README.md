# Settings
Settings is a command-line program that replaces placeholders in files with
stored values. The program expects as argument the path to an ini file which
contains two sections: `SETTINGS` and `FILES`.

The section `SETTINGS` provides the keys and values for the placeholder
replacement. The values can also already contain placeholders from preceding
keys or environment variables. Keys start with a letter or underscore, then can
be followed by alphanumeric characters, minus, underscore and dot, and end
alphanumerically or with an underscore. Key and values are separated by a
space, colon or equal sign.

The section `FILES` is a list of files in which the placeholders are to be
replaced. The paths must start with a slash or backslash. Drives at the
beginning of the path are not supported, because the program always uses the
drive where it is executed.

Example of the ini file:

```ini
[SETTINGS]
  account       #[USERNAME]
  password      You do not want to know.
  gitlab-user   git_#[account]
  gitlab-token  git_123_456_789
  jira-user     jir_#[account] 
  jira-token    jir_123_456_789

[FILES]
  - \Program Portables\Maven\conf\settings.xml
```

Basically, sections, keys and placeholders are case-insensitive.

The values of the placeholders come from the settings or from the environment
variable. Settings values take precedence. The notation of the placeholders is
the same in both cases.

The settings are implemented in such a way that the program can be started with
new values at any time. To ensure that the change still works after the first
replacement, the program creates reusable copies of the files as templates (end
with settings), where the placeholders still remain included. If there is no
template or the template is older than the original, then a new template is
created.

Only known placeholders are replaced, unknown ones remain.


# Integration
Settings is primarily used to further personalize a virtual environment. The
idea is that the ini file with the values is stored in the working directory
and thus outside the virtual hard disk. The Settings program is included in the
virtual environment and is configured and started in Startup.cmd. For the
access to the outside ini file the environment variable xxx is used, which
contains the working directory.

Example of use in `Startup.cmd`

```
REM Environment will be prepared
REM ----

REM Programs and service are configured and initialized here, but not started.

REM ---- Settings
IF EXIST "%PLATFORM_HOME%\%PLATFORM_NAME%.ini" (
    "%APPSSETTINGS%\settings.exe" "%PLATFORM_HOME%\%PLATFORM_NAME%.ini"
)

REM Placeholder for automatic module integration
REM INSERT ATTACH
```


# System Requirement
- Windows 10 or higher
- .NET 4.7.x or higher


# Download
Settings is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform/Resources/platform/Settings)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/tree/main/settings/Releases


# Changes (Change Log)
## 1.1.0 20220401 (summary of the current version)  
BF: Settings: Correction of template synchronization  
BF: Settings: Optimization and corrections  
CR: Settings: Unification of namespace / platform icon / (sub) project structure  
CR: Settings: Added build script/process via Ant  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment-creator/master/settings/CHANGES)
