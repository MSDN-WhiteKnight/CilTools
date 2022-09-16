Param($config = "Debug")

echo ('Configuration: '+$config)

$toTest = "tests/CilTools.BytecodeAnalysis.Tests/CilTools.BytecodeAnalysis.Tests.csproj -f netcoreapp3.1",
          "tests/CilTools.CommandLine.Tests/CilTools.CommandLine.Tests.csproj",
          "tests/CilView.Tests/CilView.Tests.csproj"

$exitCode = 0           
           
foreach ($item in $toTest)
{
    echo ("Testing "+$item)
    Invoke-Expression ("dotnet test "+$item+" -c "+$config)
    if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}
    echo ""
}

echo "Finished testing"
exit $exitCode
