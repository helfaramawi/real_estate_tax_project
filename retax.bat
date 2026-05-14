@echo off
setlocal EnableDelayedExpansion EnableExtensions

rem ============================================================================
rem  Real Estate Tax Intelligence Platform — Build ^& Restart
rem  Paradise Integrated Solutions
rem  Run "%~nx0 --help" for full usage.
rem ============================================================================

rem ---------- ANSI color setup (Windows 10+) ----------------------------------
for /f %%a in ('echo prompt $E ^| cmd') do set "ESC=%%a"
set "RST=%ESC%[0m"
set "INFO=%ESC%[36m"
set "OK=%ESC%[32m"
set "WARN=%ESC%[33m"
set "ERR=%ESC%[91m"
set "BOLD=%ESC%[1m"
set "DIM=%ESC%[90m"

rem ---------- Project configuration -------------------------------------------
set "PROJECT_NAME=retax"
set "COMPOSE_FILE=docker-compose.yml"
set "API_CONTAINER=retax_api"
set "FRONTEND_CONTAINER=retax_frontend"
set "POSTGRES_CONTAINER=retax_postgres"
set "FRONTEND_URL=http://localhost"
set "WAIT_TIMEOUT_SEC=180"

rem ---------- Defaults --------------------------------------------------------
set "DO_PULL=0"
set "DO_NO_CACHE=0"
set "DO_LOGS=0"
set "DO_CLEAN=0"
set "DO_DEV=0"
set "DO_NO_BUILD=0"
set "DO_STATUS_ONLY=0"
set "DO_DOWN=0"
set "TARGET="
set "PROFILE_FLAG="

rem ---------- Argument parsing ------------------------------------------------
:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="--help"     goto :show_help
if /i "%~1"=="-h"         goto :show_help
if /i "%~1"=="/?"         goto :show_help
if /i "%~1"=="--pull"      ( set "DO_PULL=1"        & shift & goto :parse_args )
if /i "%~1"=="--no-cache"  ( set "DO_NO_CACHE=1"    & shift & goto :parse_args )
if /i "%~1"=="--logs"      ( set "DO_LOGS=1"        & shift & goto :parse_args )
if /i "%~1"=="--clean"     ( set "DO_CLEAN=1"       & shift & goto :parse_args )
if /i "%~1"=="--dev"       ( set "DO_DEV=1"         & shift & goto :parse_args )
if /i "%~1"=="--no-build"  ( set "DO_NO_BUILD=1"    & shift & goto :parse_args )
if /i "%~1"=="--status"    ( set "DO_STATUS_ONLY=1" & shift & goto :parse_args )
if /i "%~1"=="--down"      ( set "DO_DOWN=1"        & shift & goto :parse_args )
if /i "%~1"=="--stop"      ( set "DO_DOWN=1"        & shift & goto :parse_args )
if /i "%~1"=="--api"       ( set "TARGET=api"       & shift & goto :parse_args )
if /i "%~1"=="--frontend"  ( set "TARGET=frontend"  & shift & goto :parse_args )
echo %ERR%[X] Unknown option: %~1%RST%
echo     Run "%~nx0 --help" to see available options.
exit /b 64
:args_done

if "%DO_DEV%"=="1" set "PROFILE_FLAG=--profile dev"

rem ---------- Banner ----------------------------------------------------------
echo.
echo %BOLD%%INFO%================================================================%RST%
echo %BOLD%%INFO%  Real Estate Tax Intelligence Platform                         %RST%
if "%DO_DOWN%"=="1" (
    echo %BOLD%%INFO%  Shutdown Pipeline                                              %RST%
) else (
    echo %BOLD%%INFO%  Build ^& Restart Pipeline                                      %RST%
)
echo %BOLD%%INFO%================================================================%RST%
echo %DIM%  Compose file : %COMPOSE_FILE%%RST%
echo %DIM%  Project name : %PROJECT_NAME%%RST%
if "%DO_DEV%"=="1"      echo %DIM%  Profile      : dev -- pgAdmin enabled%RST%
if not "%TARGET%"==""   echo %DIM%  Target       : %TARGET% only%RST%
if "%DO_NO_CACHE%"=="1" echo %DIM%  Cache        : disabled%RST%
if "%DO_CLEAN%"=="1"    echo %WARN%  DANGER       : --clean will delete database volumes%RST%
echo.

set "START_TIME=%TIME%"

rem ============================================================================
rem  STEP 0 — Preflight checks
rem ============================================================================
call :step "Preflight checks"

if not exist "%COMPOSE_FILE%" (
    call :fail "%COMPOSE_FILE% not found. Run this script from the project root."
    exit /b 1
)

docker info >nul 2>&1
if errorlevel 1 (
    call :fail "Docker daemon is not running. Start Docker Desktop and retry."
    exit /b 1
)

rem Detect Docker Compose v2 vs legacy v1
docker compose version >nul 2>&1
if not errorlevel 1 goto :compose_v2
docker-compose version >nul 2>&1
if errorlevel 1 (
    call :fail "Neither Docker Compose v2 nor v1 is installed."
    exit /b 1
)
set "DC=docker-compose"
echo %WARN%[!] Using legacy docker-compose v1. Consider upgrading to v2.%RST%
goto :compose_done
:compose_v2
set "DC=docker compose"
:compose_done

rem .env check (only needed when bringing services up)
if "%DO_DOWN%"=="1" goto :skip_env_check
if "%DO_STATUS_ONLY%"=="1" goto :skip_env_check
if exist ".env" goto :skip_env_check
if not exist ".env.example" (
    call :fail ".env file is missing and no .env.example to copy from."
    exit /b 1
)
echo %WARN%[!] .env not found, copying from .env.example%RST%
copy /Y ".env.example" ".env" >nul
echo %WARN%    Edit .env now and re-run, or proceed with example values.%RST%
:skip_env_check

call :ok "All checks passed"

rem ============================================================================
rem  --status mode: just show ps and exit
rem ============================================================================
if not "%DO_STATUS_ONLY%"=="1" goto :after_status_check
echo.
call :step "Service status"
%DC% -p %PROJECT_NAME% ps
exit /b 0
:after_status_check

rem ============================================================================
rem  --down mode: just stop services and exit
rem ============================================================================
if not "%DO_DOWN%"=="1" goto :after_down_only

if "%DO_CLEAN%"=="1" goto :down_only_clean

call :step "Stopping all services"
%DC% -p %PROJECT_NAME% --profile dev down --remove-orphans
if errorlevel 1 goto :down_only_failed
call :ok "All services stopped"
goto :down_only_summary

:down_only_clean
call :step "Stopping services and removing volumes"
echo %WARN%[!] --clean specified: removing volumes -- DB data will be lost%RST%
%DC% -p %PROJECT_NAME% --profile dev down --volumes --remove-orphans
if errorlevel 1 goto :down_only_failed
call :ok "All services stopped, volumes removed"
goto :down_only_summary

:down_only_failed
call :fail "Failed to stop services"
exit /b 1

:down_only_summary
echo.
echo %BOLD%%OK%================================================================%RST%
echo %BOLD%%OK%  Shutdown complete                                              %RST%
echo %BOLD%%OK%================================================================%RST%
call :elapsed "%START_TIME%"
exit /b 0

:after_down_only

rem ============================================================================
rem  STEP 1 — Stop existing services
rem ============================================================================
call :step "Stopping existing services"

if "%DO_CLEAN%"=="1" goto :clean_down
%DC% -p %PROJECT_NAME% down --remove-orphans
if errorlevel 1 goto :stop_failed
goto :down_ok

:clean_down
echo %WARN%[!] --clean specified: removing volumes -- DB data will be lost%RST%
%DC% -p %PROJECT_NAME% down --volumes --remove-orphans
if errorlevel 1 goto :stop_failed

:down_ok
call :ok "Services stopped"
goto :after_down

:stop_failed
call :fail "Failed to stop services"
exit /b 1

:after_down

rem ============================================================================
rem  STEP 2 — Pull latest base images (optional)
rem ============================================================================
if not "%DO_PULL%"=="1" goto :after_pull
call :step "Pulling latest base images"
%DC% -p %PROJECT_NAME% pull
if errorlevel 1 goto :pull_warn
call :ok "Images up to date"
goto :after_pull
:pull_warn
echo %WARN%[!] Pull had issues, continuing -- some images build locally%RST%
:after_pull

rem ============================================================================
rem  STEP 3 — Build
rem ============================================================================
if not "%DO_NO_BUILD%"=="1" goto :do_build
echo %DIM%[~] Skipping build per --no-build flag%RST%
goto :start_services

:do_build
call :step "Building images"

set "BUILD_FLAGS="
if "%DO_NO_CACHE%"=="1" set "BUILD_FLAGS=--no-cache --pull"

%DC% -p %PROJECT_NAME% -f %COMPOSE_FILE% %PROFILE_FLAG% build %BUILD_FLAGS% %TARGET%
if errorlevel 1 (
    call :fail "Build failed. Check the output above for the failing service."
    exit /b 1
)
call :ok "Build complete"

rem ============================================================================
rem  STEP 4 — Start services
rem ============================================================================
:start_services
call :step "Starting services"

%DC% -p %PROJECT_NAME% -f %COMPOSE_FILE% %PROFILE_FLAG% up -d --remove-orphans
if errorlevel 1 (
    call :fail "Failed to start services"
    %DC% -p %PROJECT_NAME% logs --tail=50
    exit /b 1
)
call :ok "Services started"

rem ============================================================================
rem  STEP 5 — Wait for API health (via Docker health status)
rem ============================================================================
call :step "Waiting for API container to become healthy. Timeout: %WAIT_TIMEOUT_SEC%s"

set /a "ELAPSED=0"

:wait_loop
set "HSTATUS=unknown"
for /f "usebackq delims=" %%h in (`docker inspect --format "{{.State.Health.Status}}" %API_CONTAINER% 2^>nul`) do set "HSTATUS=%%h"

if /i "!HSTATUS!"=="healthy" goto :api_ready
if /i "!HSTATUS!"=="unhealthy" (
    echo.
    call :fail "API container reports UNHEALTHY status"
    echo.
    echo %DIM%--- Last 30 lines of API logs ---%RST%
    %DC% -p %PROJECT_NAME% logs --tail=30 api
    exit /b 1
)

if !ELAPSED! geq %WAIT_TIMEOUT_SEC% (
    echo.
    call :fail "API did not become healthy within %WAIT_TIMEOUT_SEC% seconds. Last status: !HSTATUS!"
    echo.
    echo %DIM%--- Last 30 lines of API logs ---%RST%
    %DC% -p %PROJECT_NAME% logs --tail=30 api
    exit /b 1
)

<nul set /p "=."
timeout /t 3 /nobreak >nul
set /a "ELAPSED+=3"
goto :wait_loop

:api_ready
echo.
call :ok "API container is healthy"

rem Frontend reachability check on the host
curl --silent --fail --max-time 3 -o nul "%FRONTEND_URL%" 2>nul
if not errorlevel 1 goto :fe_ok
echo %WARN%[!] Frontend not yet reachable on %FRONTEND_URL% -- may still be starting%RST%
goto :fe_done
:fe_ok
call :ok "Frontend is reachable on %FRONTEND_URL%"
:fe_done

rem ============================================================================
rem  STEP 6 — Final status
rem ============================================================================
echo.
call :step "Service status"
%DC% -p %PROJECT_NAME% ps

echo.
echo %BOLD%%OK%================================================================%RST%
echo %BOLD%%OK%  Deployment successful                                          %RST%
echo %BOLD%%OK%================================================================%RST%
echo   %INFO%Frontend:%RST%   %FRONTEND_URL%
echo   %INFO%API ^(via nginx^):%RST%   %FRONTEND_URL%/api
echo   %INFO%Swagger ^(via nginx^):%RST%   %FRONTEND_URL%/swagger
if "%DO_DEV%"=="1" echo   %INFO%pgAdmin:%RST%   http://localhost:5050
echo.

call :elapsed "%START_TIME%"

rem ============================================================================
rem  STEP 7 — Tail logs (optional)
rem ============================================================================
if not "%DO_LOGS%"=="1" goto :end_ok
echo %DIM%[~] Tailing logs -- press Ctrl+C to exit...%RST%
echo.
%DC% -p %PROJECT_NAME% logs -f --tail=50

:end_ok
exit /b 0

rem ============================================================================
rem  Helper subroutines
rem ============================================================================

:step
echo.
echo %BOLD%%INFO%[*] %~1%RST%
exit /b 0

:ok
echo %OK%[v] %~1%RST%
exit /b 0

:fail
echo %ERR%[X] %~1%RST%
exit /b 0

:elapsed
for /f "tokens=1-4 delims=:.," %%h in ("%~1") do set /a "S1=(%%h*3600)+(%%i*60)+%%j"
for /f "tokens=1-4 delims=:.," %%h in ("%TIME%") do set /a "S2=(%%h*3600)+(%%i*60)+%%j"
set /a "DUR=S2-S1"
if !DUR! lss 0 set /a "DUR+=86400"
set /a "MIN=DUR/60"
set /a "SEC=DUR%%60"
echo %DIM%  Total elapsed: !MIN!m !SEC!s%RST%
echo.
exit /b 0

:show_help
echo.
echo %BOLD%Real Estate Tax Intelligence Platform — Build ^& Restart%RST%
echo.
echo %BOLD%USAGE:%RST%
echo   %~nx0 [options]
echo.
echo %BOLD%OPTIONS:%RST%
echo   --help, -h      Show this help message and exit
echo   --status        Show service status and exit
echo   --down, --stop  Stop all services and exit
echo   --pull          Pull latest base images before building
echo   --no-cache      Build without Docker cache -- slow, full rebuild
echo   --no-build      Skip build, only restart existing containers
echo   --clean         DESTRUCTIVE -- removes DB volumes
echo   --dev           Enable dev profile -- pgAdmin on :5050
echo   --api           Rebuild only the API service
echo   --frontend      Rebuild only the frontend service
echo   --logs          Tail logs after services are healthy
echo.
echo %BOLD%COMMON RECIPES:%RST%
echo   %~nx0                       Standard build + restart
echo   %~nx0 --down                Stop all services
echo   %~nx0 --down --clean        Stop and wipe volumes
echo   %~nx0 --no-build            Quick restart without rebuild
echo   %~nx0 --api --logs          Rebuild API only, then tail logs
echo   %~nx0 --no-cache            Full rebuild after dependency changes
echo   %~nx0 --clean --no-cache    Nuclear -- wipe volumes + rebuild all
echo   %~nx0 --dev                 Bring up with pgAdmin
echo   %~nx0 --status              Show what's currently running
echo.
echo %BOLD%EXIT CODES:%RST%
echo    0    Success
echo    1    Build/runtime failure
echo   64    Invalid arguments
echo.
exit /b 0
