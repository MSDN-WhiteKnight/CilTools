dotnet build -f netcoreapp3.1
.\bin\Debug\netcoreapp3.1\CilTools.CommandLine.exe view "..\tests\CilTools.BytecodeAnalysis.Tests\bin\Debug\netcoreapp2.1\CilTools.Tests.Common.dll" CilTools.Tests.Common.SampleMethods PrintHelloWorld
pause
