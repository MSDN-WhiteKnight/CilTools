dotnet build -c Release ..\CilTools.CommandLine\CilTools.CommandLine.csproj

# Ensure "obj" folder exists and is empty
New-Item -Path "." -Name "obj" -ItemType "directory" -ErrorAction:SilentlyContinue
Remove-Item ".\obj\*"

# Copy binaries to output dir

echo ""
echo "Copying files..."

$inputDir = "..\CilTools.CommandLine\bin\Release\netcoreapp3.1\"
$outputDir = ".\obj\"

$fileList = "CilTools.BytecodeAnalysis.dll",
            "CilTools.BytecodeAnalysis.xml",
            "CilTools.Metadata.dll",
            "CilTools.Metadata.xml",
            "CilTools.SourceCode.dll",
            "CilTools.SourceCode.xml",
            "CilTools.Visualization.dll",
            "CilTools.Visualization.xml",
            "CilView.Core.dll",
            "cil.dll",
            "cil.runtimeconfig.json",
            "cil.deps.json"

foreach ($file in $fileList)
{
    Copy-Item ($inputDir+$file) -Destination $outputDir
}

# Copy scripts to output dir
$inputDir = "..\CilTools.CommandLine\"
$outputDir = ".\obj\"
$fileList = "cil.sh", "cil.cmd", "license.txt"

foreach ($file in $fileList)
{
    Copy-Item ($inputDir+$file) -Destination $outputDir
}

echo "Creating package..."

# Create empty ZIP archive
Remove-Item "..\_Distr\CilTools.CommandLine-bin.zip" -ErrorAction:SilentlyContinue
Copy-Item ".\empty.zip" -Destination "..\_Distr\"
Rename-Item -Path "..\_Distr\empty.zip" -NewName "CilTools.CommandLine-bin.zip"

# Add files into ZIP archive
$filePath = [System.IO.Path]::GetFullPath("..\_Distr\CilTools.CommandLine-bin.zip")
$shell = New-Object -ComObject Shell.Application
$zipFile = $shell.NameSpace($filePath)
$dir = $shell.NameSpace([System.IO.Path]::GetFullPath(".\obj"))
$zipFile.CopyHere($dir.Items())

echo "Finished"
[void][System.Console]::ReadKey($true)
