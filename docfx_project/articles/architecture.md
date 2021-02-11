# CIL Tools architecture

## Terms

[Common Intermediate Language (CIL)](https://docs.microsoft.com/en-us/dotnet/standard/managed-code#intermediate-language--execution) - a language that represents a program as a set of instructions for the architecture-independent Virtual Execution Engine (.NET Runtime) defined in [ECMA-335: Common Language Infrastructure](https://www.ecma-international.org/publications-and-standards/standards/ecma-335/) specification. Also known as MSIL or simply IL. CIL code can exist in two forms, bytecode and assembler.

**CIL bytecode** - a binary form of CIL, a compact machine readable byte representation consumed by runtime. CIL bytecode is produced by compilers of high-level .NET languages or by CIL assembler program.

**CIL assembler** - a textual form of CIL, represented as a language with the formal grammar similar to architecture-specific assembly language grammars. Also the program that produces the binary form of CIL from textual sources. CIL assembler text is produced by CIL disassemblers.

**.NET Metadata** - a set of structures that describe the contents of .NET asssemblies defined in ECMA-335 specification. Metadata is a concept related to CIL; some CIL assembler directives represent corresponding metadata structures. A .NET assembly consists of metadata and CIL bytecode.

**Reflection** - a set of APIs that support programmatic inspection of .NET assemblies using types derived from base types in standard reflection library, such as System.Type, System.Reflection.MethodBase etc. These base types could be called a *reflection contract*, then a set of actual concrete types implementing them is a *reflection implementation*.

## Overview

CIL Tools is a set of software to work with CIL. CIL Tools aims to help make CIL both accessible for the programmatical analysis and readable by human (for example, for debugging or studying purposes). However, decompiling CIL to high-level .NET languages is beyond the scope of CIL Tools.

## CilTools.BytecodeAnalysis

CilTools.BytecodeAnalysis library reads CIL bytecode and converts it into high-level objects or textual CIL assembler representation ("disassembly"). The data source of this library is a reflection implementation; this could be either the standard reflection implementation provided by .NET, a custom implementaion shipped in CIL Tools or a third-party custom implementation. Classes in custom reflection implementation can be derived either directly from the reflection contract, or from classes in CilTools.Reflection namespace. In the second case, they could provide extra information, such as P/Invoke parameters ([CustomMethod.GetPInvokeParams](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Reflection.CustomMethod.html#CilTools_Reflection_CustomMethod_GetPInvokeParams)). Besides parsing the bytecode itself, the bytecode analysis library reads some metadata structures not handled well by standard reflection, such as signatures (reflection can parse method signatures, but not standalone signatures for indirect calls). However, reading metadata in general is beyond the scope of this library; it relies on the reflection implementation to read it when it's needed (such as for token resolution or to emit CIL assembler directives corresponding to metadata).

**Design principles.** The CilTools.BytecodeAnalysis is the core library for the CIL Tools suite. It should not have dependencies on other CIL Tools projects, but other CIL Tools projects can depend on it. If the type must be shared by multiple projects, it can be placed here even if does not directly align with purposes outlined above. The CilTools.BytecodeAnalysis should not have dependencies besides BCL, and should be compatible with .NET Framework 3.5 and .NET Standard 2.0. If it needs to consume something from newer frameworks, it must be accessed via late binding or via abstractions implemented in other projects. Other projects could depend on external libraries or target newer frameworks.

Namespaces in CilTools.BytecodeAnalysis:

- [CilTools.BytecodeAnalysis](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.html) - code related to bytecode and signatures parsing
- [CilTools.BytecodeAnalysis.Extensions](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.Extensions.html) - extension methods that simplify using a library by extending standard reflection classes
- [CilTools.Reflection](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Reflection.html) - base classes for custom reflection implementations and code related to inspecting metadata
- [CilTools.Syntax](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Syntax.html) - classes that represent the CIL assembler syntax tree, needed to support syntax highlighting

Key types in CilTools.BytecodeAnalysis:

- [CilTools.BytecodeAnalysis.CilInstruction](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilInstruction.html) - a main structural element of CIL bytecode. Everything that processes bytecode works in terms of instructions.
- [CilTools.BytecodeAnalysis.CilReader](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilReader.html) - a low-level class that sequentially reads CIL bytes and returns instructions. It only understands individual instructions and does not deal with control flow, such as branches or exception handler blocks.
- [CilTools.BytecodeAnalysis.CilGraph](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilGraph.html) - takes instructions and returns a graph that represents the method's control flow. Unlike the previous one, takes into account branches and exception blocks. CilGraph.Create is likely an entry point for the consumer of the API.
- [CilTools.Reflection.CustomMethod](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Reflection.CustomMethod.html) - a base class for custom MethodBase implementations that need to provide more info then standard reflection supports
- [CilTools.Syntax.SyntaxNode](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Syntax.SyntaxNode.html) - a base class for syntax nodes in CIL assembler syntax tree

## CilTools.Runtime

CilTools.Runtime is a reflection implementation that reads information about assemblies loaded into the external .NET process via ClrMD. Besides regular assemblies loaded from files, it could read dynamic methods not belonging to the specific assembly. The key type in this library is the [ClrAssemblyReader](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Runtime.ClrAssemblyReader.html) that returns assemblies from the specified CLR instance. For more information about inspecting external managed processes see [ClrMD repository](https://github.com/microsoft/clrmd).

## CilTools.Metadata

CilTools.Metadata is a reflection implementation that reads information about assembly files without loading them into the current execution context. It enables inspecting assemblies for another target framework (such as .NET Standard assemblies when your application is on .NET Framework) or when some dependencies could not be resolved, using the same API shape as the standard reflection. It also implements fetching information not supported by standard reflection: P/Invoke parameters, custom modifiers, function pointer signatures etc. CilTools.Metadata is build upon the [System.Reflection.Metadata](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.metadata) library shipped by .NET Team. 

The key type in this library is [CilTools.Metadata.AssemblyReader](https://msdn-whiteknight.github.io/CilTools/api/CilTools.Metadata.AssemblyReader.html) that exposes methods to load assemblies.

## CIL View

[CIL View](https://github.com/MSDN-WhiteKnight/CilTools/tree/master/CilView) is a windows application to display CIL code of methods in the given assembly file or process. It supports extra functionality such as syntax highlighting, navigating to method code by click on its name and searching types and methods in assemblies. CIL View relies on other CIL Tools libraries to read bytecode and metadata. It uses Windows Presentation Foundation as a GUI toolkit.
