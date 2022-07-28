$toTest = "tests/CilTools.BytecodeAnalysis.Tests/CilTools.BytecodeAnalysis.Tests.csproj -f netcoreapp3.1",
          "tests/CilTools.Metadata.Tests/CilTools.Metadata.Tests.csproj -f netcoreapp3.1",
          "tests/CilTools.CommandLine.Tests/CilTools.CommandLine.Tests.csproj",
          "tests/CilView.Tests/CilView.Tests.csproj"

$exitCode = 0           
           
foreach ($item in $toTest)
{
    echo ("Testing "+$item)
    Invoke-Expression ("dotnet test "+$item)
    if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}
}

echo "Finished testing"
exit $exitCode
