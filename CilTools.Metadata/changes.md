# CilTools.Metadata changes

2.6

- Add support for getting inherited members in TypeDef and TypeRef. Now APIs like GetMembers return both declared and inherited members by default, and only declared ones when DeclaredOnly flag is specified.
- Implement Type.StructLayoutAttribute property on TypeDef
- Fix Type.IsValueType and Type.IsEnum returning incorrect values for .NET Core assemblies
- Fix token resolution to throw ArgumentOutOfRangeException instead of BadImageFormatException on out-of-range tokens
