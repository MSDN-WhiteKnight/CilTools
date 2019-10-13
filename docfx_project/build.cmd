rem echo %~dp0
C:\Distr\Microsoft\docfx\docfx.exe .\docfx.json
copy /Y logo.svg ..\docs
C:\Distr\Microsoft\docfx\docfx.exe serve ..\docs
pause