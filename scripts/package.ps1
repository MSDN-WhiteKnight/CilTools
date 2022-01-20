# Ensure "obj" folder exists and is empty
New-Item -Path "." -Name "obj" -ItemType "directory" -ErrorAction:SilentlyContinue
Remove-Item ".\obj\*"

echo ""
echo "Copying files..."

# Copy binaries to output dir
Copy-Item "..\CilView\bin\Release\CilTools.BytecodeAnalysis.dll" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilTools.BytecodeAnalysis.xml" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilTools.Metadata.dll" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilTools.Metadata.xml" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilTools.Runtime.dll" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilTools.Runtime.xml" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\CilView.exe" -Destination ".\obj"
Copy-Item "..\CilView\bin\Release\CilView.exe.config" -Destination ".\obj"
Copy-Item "..\CilView\bin\Release\CilView.Core.dll" -Destination ".\obj"
Copy-Item "..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.dll" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.xml" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\Microsoft.Diagnostics.Runtime.pdb" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\System.Reflection.Metadata.dll" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\System.Collections.Immutable.dll" -Destination ".\obj\"

Copy-Item "..\CilView\bin\Release\license.txt" -Destination ".\obj\"
Copy-Item "..\CilView\bin\Release\ReadMe.txt" -Destination ".\obj\"

# Copy PDF docs to output dir
Copy-Item "..\docfx_project\_site_pdf\docfx_project_pdf.pdf" -Destination ".\obj\"
Rename-Item -Path ".\obj\docfx_project_pdf.pdf" -NewName "docs.pdf"

echo "Creating package..."

# Create empty ZIP archive
Remove-Item "..\_Distr\CilTools-bin.zip" -ErrorAction:SilentlyContinue
Copy-Item ".\empty.zip" -Destination "..\_Distr\"
Rename-Item -Path "..\_Distr\empty.zip" -NewName "CilTools-bin.zip"

# Add files into ZIP archive
$filePath = [System.IO.Path]::GetFullPath("..\_Distr\CilTools-bin.zip")
$shell = New-Object -ComObject Shell.Application
$zipFile = $shell.NameSpace($filePath)
$dir = $shell.NameSpace([System.IO.Path]::GetFullPath(".\obj"))
$zipFile.CopyHere($dir.Items())

echo "Finished"
[void][System.Console]::ReadKey($true)
