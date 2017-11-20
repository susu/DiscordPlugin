:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

REM @echo off
REM set /p path="Please enter the folder location of your SpaceEngineersDedicated.exe: "
REM cd %~dp0
REM mklink /J GameBinaries "%path%"
REM if errorlevel 1 goto Error
REM echo Done!
REM goto End
REM :Error
REM echo An error occured creating the symlink.
REM goto EndFinal
REM :End

set /p path="Please enter the folder location of your Torch.Server.exe: "
cd %~dp0
mklink /J TorchBinaries "%path%"
if errorlevel 1 goto Error
echo Done! You can now open the Torch solution without issue.
goto EndFinal
:Error2
echo An error occured creating the symlink.
:EndFinal
pause
