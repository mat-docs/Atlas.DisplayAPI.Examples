if exist "C:\Program Files\McLaren Applied Technologies\ATLAS 10\" (
  xcopy /r /y %1 "C:\Program Files\McLaren Applied Technologies\ATLAS 10"
)else (
  echo "No Atlas installation detected - skipping deploying"
)

rem xcopy /r /y %1 "C:\dev\MAT.OCS.Atlas10\bin\Debug"
