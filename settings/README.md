# Settings
Settings is a command-line program that replaces placeholders in files with
stored values. The program expects as argument the path to an ini file which
contains two sections (settings and files).

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
[settings]
  account       #[USERNAME]
  password      You do not want to know.
  gitlab-user   #[account]
  gitlab-token  git_123_456_789
  jira-user     #[account] 
  jira-tokem    jir_123_456_789

[files]
  - \Program Portables\Maven\conf\settings.xml
  - \Settings\.m2\settings.xml
```

Basically, sections, keys and placeholders are case-insensitive.

The values of the placeholders come from the settings or from the environment
variable. The notation of the placeholders is the same in both cases.

The settings are implemented in such a way that the program can be started with
new values at any time. To ensure that the change still works after the first
replacement, the program creates reusable copies of the files as templates (end
with #), where the placeholders still remain included. If there is no template
it will be created from the current file.

Only known placeholders are replaced, unknown ones remain.
