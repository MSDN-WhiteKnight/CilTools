# Publish nuget packages
$ver = "2.8.0"
$src = "nuget.org2"

$Env:Path += ";C:\Distr\Microsoft\nuget 4.1\"

nuget push "..\CilTools.BytecodeAnalysis\bin\Release\CilTools.BytecodeAnalysis.$ver.nupkg" -src $src
nuget push "..\CilTools.Metadata\bin\Release\CilTools.Metadata.$ver.nupkg" -src $src
nuget push "..\CilTools.Runtime\pkg\CilTools.Runtime.$ver.nupkg" -src $src
nuget push "..\CilTools.SourceCode\bin\Release\CilTools.SourceCode.$ver.nupkg" -src $src
nuget push "..\CilTools.Visualization\bin\Release\CilTools.Visualization.$ver.nupkg" -src $src
nuget push "..\CilTools.CommandLine\bin\Release\CilTools.CommandLine.$ver.nupkg" -src $src

echo "Finished"
[void][System.Console]::ReadKey($true)
