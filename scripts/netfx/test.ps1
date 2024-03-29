# This script should be executed from Visual Studio Developer command prompt (or with vstest.console on %PATH%)
Param($config = "Debug")

$toTest = "tests\CilTools.BytecodeAnalysis.Tests\bin\$config\net45\CilTools.BytecodeAnalysis.Tests.dll",
          "tests\CilTools.Metadata.Tests\bin\$config\net45\CilTools.Metadata.Tests.dll"

$exitCode = 0           

foreach ($item in $toTest)
{
    echo ("Testing "+$item)
    Invoke-Expression ("vstest.console.exe "+$item)
    if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}
    echo ""
}

echo "Long-running tests..."

# Test CilTools.Runtime
Set-Location -Path "tests\CilTools.Runtime.Tests\bin\$config"
.\CilTools.Runtime.Tests.exe
if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}

echo "Finished testing"
exit $exitCode
