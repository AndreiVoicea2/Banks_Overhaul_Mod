@echo off
setlocal

:: Define directories and filenames
set "scriptDirInterkarmaWindows=C:\Users\Admin\Downloads\daggerfall-unity-1.1.1\daggerfall-unity-1.1.1\StandaloneWindows"
set "scriptDirInterkarmaLinux=C:\Users\Admin\Downloads\daggerfall-unity-1.1.1\daggerfall-unity-1.1.1\StandaloneLinux64"
set "scriptDirInterkarmaOsx=C:\Users\Admin\Downloads\daggerfall-unity-1.1.1\daggerfall-unity-1.1.1\StandaloneOSX"
set "scriptDirVwing=C:\Users\Admin\Downloads\daggerfall-unity-android-android\daggerfall-unity-android-android\Android"

set "fileName=banks remaked.dfmod"
set "zipFileNameWindows=banks_remakedWIN.zip"
set "zipFileNameLinux=banks_remakedLINUX.zip"
set "zipFileNameOSX=banks_remakedOSX.zip"
set "zipFileNameAndroid=banks_remakedANDROID.zip"

:: Call the function for each platform
call :ProcessFiles "%scriptDirInterkarmaWindows%" "%fileName%" "%zipFileNameWindows%"
call :ProcessFiles "%scriptDirInterkarmaLinux%" "%fileName%" "%zipFileNameLinux%"
call :ProcessFiles "%scriptDirInterkarmaOsx%" "%fileName%" "%zipFileNameOSX%"
call :ProcessFiles "%scriptDirVwing%" "%fileName%" "%zipFileNameAndroid%"

endlocal
exit /b 0

:: Function to process files
:ProcessFiles
setlocal
set "scriptDir=%~1"
set "fileName=%~2"
set "zipFileName=%~3"

:: Set paths for file, Mods folder, and zip file
set "filePath=%scriptDir%\%fileName%"
set "modsDir=%scriptDir%\Mods"
set "zipFilePath=%scriptDir%\%zipFileName%"

:: Create Mods directory if it doesn't exist
if not exist "%modsDir%" mkdir "%modsDir%"

:: Move the file to the Mods directory
move /y "%filePath%" "%modsDir%\%fileName%"

:: Delete existing zip file if it exists
if exist "%zipFilePath%" del /q "%zipFilePath%"

:: Create the zip file including the Mods directory
powershell -command "Compress-Archive -Path '%modsDir%' -DestinationPath '%zipFilePath%'"

:: Clean up Mods directory (if desired, comment this out if you want to keep the Mods directory)
rd /s /q "%modsDir%"

endlocal
exit /b 0
