The file database.7z  contains the completely preconfigured database, which is
unpacked to #[environment.database.directory] during the creation of the virtual environment.



To install a new database use the following script:

    set db_root=#[environment.database.directory]
    set db_home=#[module.environment]
    set db_data=%db_root%
    set db_service_name=#[environment.name] PostgreSQL

    "%db_home%\bin\initdb" -E UTF8 -D "%db_data%" -U dba -A trust



The database must be configured.
The configuration files:

#[environment.database.directory]\postgresql.conf
configure: listen_addresses, port and logging

#[environment.database.directory]\pg_hba.conf
In this file the access/connection permissions are configured.
There is at least one permission for the database administration of the database: dba.
The database administrator should be used only locally on the DBMS.

    host    all    dba    samehost    trust



Configuration of the Windows service:
That does the virtual environment itself later.
The command line must be opened as administrator.

    set db_root=#[environment.database.directory]
    set db_home=#[module.environment]
    set db_data=%db_root%
    set db_service_name=#[environment.name] PostgreSQL

    "%db_home%\bin\pg_ctl" register -D "%db_data%" -N "%db_service_name%" -p "%db_home%\bin\pg_ctl.exe" -S auto -s

If necessary, the uninstall command:

    "%db_home%\bin\pg_ctl" unregister -N "%db_service_name%"



The service is started as follows:
That does the virtual environment itself later.

    net start "#[environment.name] PostgreSQL"

The service is stopped as follows:
That does the virtual environment itself later.

    net stop "#[environment.name] PostgreSQL"
