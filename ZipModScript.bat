@echo off
setlocal

:: Define directories and filenames
set "scriptDirInterkarmaWindows=D:\Downloads\daggerfall-unity-1.1.1\StandaloneWindows"
set "scriptDirInterkarmaLinux=D:\Downloads\daggerfall-unity-1.1.1\StandaloneLinux64"
set "scriptDirInterkarmaOsx=D:\Downloads\daggerfall-unity-1.1.1\StandaloneOSX"
set "scriptDirVwing=D:\Downloads\daggerfall-unity-android-android\Android"

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

:: Set file and zip paths
set "filePath=%scriptDir%\%fileName%"
set "zipFilePath=%scriptDir%\%zipFileName%"
set "tempDir=%scriptDir%\temp"

:: Create temporary directory if it doesn't exist
if not exist "%tempDir%" mkdir "%tempDir%"

:: Copy the file to the temporary directory
copy "%filePath%" "%tempDir%\%fileName%"

:: Delete existing zip file
del /q "%zipFilePath%"

:: Create the zip file
powershell -command "Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::CreateFromDirectory('%tempDir%', '%zipFilePath%')"

:: Clean up temporary directory
rd /s /q "%tempDir%"

endlocal
exit /b
