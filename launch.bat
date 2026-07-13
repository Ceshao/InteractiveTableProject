@echo off
rem POC dual-process launcher: table UI on monitor 1, projection on monitor 2.
rem Put this next to the built exe (or edit EXE path below), then double-click.
cd /d %~dp0
set EXE=InteractiveTable.exe
if not exist logs mkdir logs

start "TableUI" %EXE% -mode table -monitor 1 -screen-fullscreen 1 -logFile logs\table.log
start "Projection" %EXE% -mode projection -monitor 2 -screen-fullscreen 1 -logFile logs\projection.log
