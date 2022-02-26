# This script should be executed from Visual Studio Developer command prompt (or with MSBuild on %PATH%)

echo "Building CIL Tools..."
$exitCode = 0

msbuild "-t:Restore;Build"

if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}       

echo "Build finished"
exit $exitCode
