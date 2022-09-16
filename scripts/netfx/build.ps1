# This script should be executed from Visual Studio Developer command prompt (or with MSBuild on %PATH%)
Param($config = "Debug")

echo "Building CIL Tools..."
echo ('Configuration: '+$config)
$exitCode = 0

msbuild ('-t:Restore;Build -p:Configuration='+$config)

if ($LASTEXITCODE -ne 0) {$exitCode = $LASTEXITCODE}       

echo "Build finished"
exit $exitCode
