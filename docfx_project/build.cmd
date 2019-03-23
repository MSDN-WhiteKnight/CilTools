rem echo %~dp0
D:\Distr\Microsoft\docfx\docfx.exe .\docfx.json
copy /Y logo.svg ..\docs
D:\Distr\Microsoft\docfx\docfx.exe serve ..\docs
pause