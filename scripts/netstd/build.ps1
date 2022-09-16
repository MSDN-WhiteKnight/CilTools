Param($config = "Debug")

echo ('Configuration: '+$config)

$toBuild = "CilTools.Metadata/CilTools.Metadata.csproj -f netstandard2.0",
           "tests/CilTools.BytecodeAnalysis.Tests/CilTools.BytecodeAnalysis.Tests.csproj -f netcoreapp3.1",
           "tests/CilTools.CommandLine.Tests/CilTools.CommandLine.Tests.csproj",
           "tests/CilView.Tests/CilView.Tests.csproj"

$exitCode = 0           
           
foreach ($item in $toBuild)
{
    echo ("Building "+$item)
    Invoke-Expression ("dotnet build "+$item+" -c "+$config)
    if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}
    echo ""
}

echo "Build finished"
exit $exitCode
