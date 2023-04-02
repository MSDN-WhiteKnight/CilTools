# Ensure "obj" folder exists and is empty
New-Item -Path "." -Name "obj" -ItemType "directory" -ErrorAction:SilentlyContinue
Remove-Item ".\obj\*"

# Copy binaries to output dir

echo ""
echo "Copying files..."

$inputDir = "..\CilView\bin\Release\"
$outputDir = ".\obj\"

$fileList = "CilTools.BytecodeAnalysis.dll",
            "CilTools.BytecodeAnalysis.xml",
            "CilTools.Metadata.dll",
            "CilTools.Metadata.xml",
            "CilTools.Runtime.dll",
            "CilTools.Runtime.xml",
            "CilTools.SourceCode.dll",
            "CilTools.SourceCode.xml",
            "CilView.exe",
            "CilView.exe.config",
            "CilView.Core.dll",
            "Microsoft.Diagnostics.Runtime.dll",
            "Microsoft.Diagnostics.Runtime.xml",
            "Microsoft.Diagnostics.Runtime.pdb",
            "System.Reflection.Metadata.dll",
            "System.Collections.Immutable.dll",
            "Newtonsoft.Json.dll",
            "license.txt",
            "ReadMe.txt",
            "credits.txt"

foreach ($file in $fileList)
{
    Copy-Item ($inputDir+$file) -Destination $outputDir
}

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
