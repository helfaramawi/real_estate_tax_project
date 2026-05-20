@echo off
rem ============================================================================
rem  Quick shortcut to stop all services.
rem  Just delegates to retax.bat --down with any extra flags forwarded.
rem  Examples:
rem    retax-down.bat              Stop services, keep DB data
rem    retax-down.bat --clean      Stop services and wipe volumes (DB data lost)
rem ============================================================================
"%~dp0retax.bat" --down %*
