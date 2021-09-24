## CIL Tools example - Working with instructions

This example shows how to programmatically parse individual instructions of the .NET method and display their CIL code and byte offsets using `GetInstructions` extension method. 

The example is a .NET Core application that uses .NET Standard build of `CilTools.BytecodeAnalysis`. It requires .NET Core SDK version 2.1 or above. To build it, open the command line in the sample directory and enter `dotnet build`.
