# CilTools.Metadata changes

2.6

- Add support for getting inherited members in TypeDef and TypeRef. Now APIs like GetMembers return both declared and inherited members by default, and only declared ones when DeclaredOnly flag is specified.
- Implement Type.StructLayoutAttribute property on TypeDef
- Fix Type.IsValueType and Type.IsEnum returning incorrect values for .NET Core assemblies
- Fix token resolution to throw ArgumentOutOfRangeException instead of BadImageFormatException on out-of-range tokens

2.8

- Support additional assembly resolution directories in AssemblyReader
- Support inherit parameter in Type.GetCustomAttributes methods

2.9

- Assembly resolution logic in AssemblyReader now tries to search in assemblies loaded by path before calling AssemblyResolve handler
