NET SESSION >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    IF EXIST "C:\Program Files\McLaren Applied Technologies\ATLAS 10\" (
        xcopy /r /y /S /I /F %1 "C:\Program Files\McLaren Applied Technologies\ATLAS 10"
    ) ELSE (
        echo "No Atlas installation detected - skip deploying"
    )
    rem xcopy /r /y %1 "C:\dev\MAT.OCS.Atlas10\bin\Debug"
) ELSE (
    echo "This script requires admin privileges - skip deploying"
    exit /b 0
)