@echo off
mkdir .\obj
del /Q .\obj\*

echo Copying files to output dir...

rem Copy binaries to output dir
copy /Y /V ..\CilView\bin\Release\CilTools.BytecodeAnalysis.dll /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilTools.BytecodeAnalysis.xml /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilTools.Metadata.dll /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilTools.Metadata.xml /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilTools.Runtime.dll /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilTools.Runtime.xml /B .\obj\
copy /Y /V ..\CilView\bin\Release\CilView.exe /B .\obj
copy /Y /V ..\CilView\bin\Release\CilView.exe.config /B .\obj
copy /Y /V ..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.dll /B .\obj\
copy /Y /V ..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.xml /B .\obj\
copy /Y /V ..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.pdb /B .\obj\
copy /Y /V ..\CilView\bin\Release\System.Reflection.Metadata.dll /B .\obj\
copy /Y /V ..\CilView\bin\Release\System.Collections.Immutable.dll /B .\obj\

copy /Y /V ..\CilView\bin\Release\license.txt .\obj
copy /Y /V ..\CilView\bin\Release\ReadMe.txt .\obj

rem copy PDF docs to output dir
copy /Y /V ..\docfx_project\_site_pdf\docfx_project_pdf.pdf /B .\obj
ren .\obj\docfx_project_pdf.pdf docs.pdf

PAUSE
