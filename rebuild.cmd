@echo off
setlocal

set "COMPOSE=docker compose"

echo === Building images (pull latest base) ===
%COMPOSE% build --pull
if errorlevel 1 goto :error

echo === Recreating containers with new image ===
%COMPOSE% up -d --force-recreate
if errorlevel 1 goto :error

echo === Cleaning dangling images ===
docker image prune -f

echo Done.
exit /b 0

:error
echo FAILED with errorlevel %errorlevel%
exit /b %errorlevel%

