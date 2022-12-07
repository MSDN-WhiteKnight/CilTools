*** CilTools.Runtime library ***
Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
License: BSD 2.0

CilTools.Runtime reads Common Intermediate Language (CIL) bytecode of methods in external process's CLR instance using ClrMD. This enables processing bytecode from external process with CilTools.BytecodeAnalysis library.

Requirements: .NET Framework 4.5+

* Changelog: *

2.1
- Add exception block support for dynamic methods
- Add method token resolution for dynamic methods

2.2
- Add support for dynamic assemblies
- Implement IsAssignableFrom on ClrTypeInfo
- Improve performance of some ClrTypeInfo methods

2.3
- Add support for Assembly.GetName() on dynamic assemblies (.NET Framework only)

2.4
- Update custom method implementations to derive from MethodInfo/ConstructorInfo and implement ICustomMethod

2.5
- Implement GetReferencedAssemblies and ManifestModule on DynamicMethodsAssembly
