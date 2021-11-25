# Platform
Platform is a tool for the initial creation, use and management of the virtual
environment and modules that then automatically download tools and programs
from the Internet, configure them and integrated them in the virtual
environment.

__In the upcoming release 3.0.0 the modules are not yet supported.__

# Usage
- Rename __platform.exe__ to the name that will be used for the environment and
  drive

When the virtual environment is created in the form of the vhdx file, it can be
used as follows.

`usage: platform.exe A-Z: [create|attach|detach|compact|shortcuts]  `

Example
- `seanox.exe B: create` to create the initial environment as vhdx
- `seanox.exe B: shortcuts` to create the usual calls as shortcuts
- `seanox.exe B: attach` to attach the environment

Configure __Startup.cmd__ in the root directory of the virtual environment and
add the desired programs and services. It is recommended to use a launcher so
that the environment variables are available to the called programs. Detach
should also be started via the launcher if programs and services are terminated
when detaching and the environment variables are needed for this.

- `seanox.exe B: detach` to detach the environment
- `seanox.exe B: compact` to shrink the environment

__Module integration will come later, but will be similar.__
