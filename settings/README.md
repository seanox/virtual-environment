# Settings
Settings is a command-line program that replaces placeholders in files with
stored values. The program expects as argument the path to an ini file which
contains two sections. settings and files.

The section `settings` provides the keys and values for the placeholder
replacement. The values can also already contain placeholders from preceding
keys or environment variables. Keys start with a letter or underscore, then can
be followed by alphanumeric characters, minus, underscore and dot, and end
alphanumerically or with an underscore. Key and values are separated by a
space, colon or equal sign.

The section `files` is a list of files in which the placeholders are to be
replaced. The paths must start with a slash or backslash. Drives at the
beginning of the path are not supported, because the program always uses the
drive where it is executed.

Example of the ini file:

```ini
[SETTINGS]
  account       #[USERNAME]
  password      You do not want to know.
  gitlab-user   #[account]
  gitlab-token  git_123_456_789
  jira-user     #[account] 
  jira-tokem    jir_123_456_789

[FILES]
  - \Program Portables\Maven\conf\settings.xml
  - \Settings\.m2\settings.xml
```

Basically, sections, keys and placeholders are case-insensitive.

The values of the placeholders come from the settings or from the environment
variable. The notation of the placeholders is the same in both cases.

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

Example from `Startup.cmd`

`"%APPSSETTINGS%\settings.exe" "%PLATFORM_HOME%\%PLATFORM_NAME%.ini"`
