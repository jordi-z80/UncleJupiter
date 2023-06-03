@echo off

where /q curl
if %ERRORLEVEL% neq 0 (
    echo curl is not installed. Please use "choco install curl"
    exit /b
)

where /q 7z
if %ERRORLEVEL% neq 0 (
    echo 7zip is not installed. Please use "choco install 7zip"
    exit /b
)

where /q git
if %ERRORLEVEL% neq 0 (
    echo git is not installed. Please use "choco install git"
    exit /b
)

set ans=y
set /p ans=Do you want to install vosk-english (Y/n)?
if "%ans%" NEQ "n" (
    mkdir external
    curl -o ./external/vosk-model-small-en-us-0.15.zip https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip
    7z.exe e ./external/vosk-model-small-en-us-0.15.zip -o./external/vosk-model-small-en-us-0.15/
    del .\external\vosk-model-small-en-us-0.15.zip
) else (
    echo Skipping vosk-english installation.
)

set ans=y
set /p ans=Do you want to install FSCmd (Y/n)?
if "%ans%" NEQ "n" (
    git clone https://github.com/jordi-z80/FSCmd ./external/FSCmd
    cd ./external/FSCmd
    dotnet build --configuration Release
    cd ../..
) else (
    echo Skipping FSCmd installation.
)

set ans=y
set /p ans=Do you want to install external sounds (Y/n)?
if "%ans%" NEQ "n" (
    md external\audio
    curl -o external/audio/computerbeep_29.mp3 https://www.trekcore.com/audio/computer/computerbeep_29.mp3
    curl -o external/audio/computerbeep_41.mp3 https://www.trekcore.com/audio/computer/computerbeep_41.mp3
    curl -o external/audio/computerbeep_58.mp3 https://www.trekcore.com/audio/computer/computerbeep_58.mp3
) else (
    echo Skipping sounds installation.
)



