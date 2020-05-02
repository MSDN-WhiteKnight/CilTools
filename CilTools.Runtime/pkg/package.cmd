@echo off
cd %~dp0
copy /Y /V ..\bin\Release\CilTools.Runtime.dll /B lib\net45\
copy /Y /V ..\bin\Release\CilTools.Runtime.pdb /B lib\net45\
copy /Y /V ..\bin\Release\CilTools.Runtime.xml lib\net45\
copy /Y /V ..\ReadMe.txt .\
"C:\Distr\Microsoft\nuget 2.8.6\nuget.exe" pack CilTools.Runtime.dll.nuspec
