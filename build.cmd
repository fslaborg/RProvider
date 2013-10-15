@echo off
cls
if not exist "tools\FAKE\tools\Fake.exe" ( 
  ".\.nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
)
"tools\FAKE\tools\Fake.exe" build.fsx
pause