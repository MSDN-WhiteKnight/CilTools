@echo off
cd %~dp0
copy /Y /V ..\bin\Release\CilTools.BytecodeAnalysis.dll /B lib\net35\
copy /Y /V ..\bin\Release\CilTools.BytecodeAnalysis.pdb /B lib\net35\
copy /Y /V ..\bin\Release\CilTools.BytecodeAnalysis.xml lib\net35\
copy /Y /V ..\ReadMe.txt .\
"C:\Distr\Microsoft\nuget 2.8.6\nuget.exe" pack CilTools.BytecodeAnalysis.dll.nuspec
