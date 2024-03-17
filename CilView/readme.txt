== CilView ==

**Requirements:** .NET Framework 4.5+  
**License:** BSD 3-Clause  
**Repository:** https://github.com/MSDN-WhiteKnight/CilTools  

CilView is a windows application to display CIL code of methods in .NET assemblies. The key features are:

- Displaying CIL code of methods in the specified assembly file or process
- Automatically building C#/VB sources and displaying the resulting CIL
- Displaying contents of CIL assembler source files (*.il)
- Syntax highlighting
- Navigation to the referenced method's code by clicking on the method reference
- Exporting CIL code into file
- Displaying process information and stack traces of managed threads (when process is opened)
- Displaying source code from which the method's CIL was compiled
- Executing methods interactively and inspecting the results

---------------------------------------------

Changelog

2.1

- Update assembly reading mechanism to use CilTools.Metadata instead of loading assemblies directly into the current process. This enables inspecting assemblies for another target framework (such as .NET Standard) or when some dependencies could not be resolved. This also means assemblies can be unloaded from memory when they are no longer needed.
- Add support for method implementation flags when outputting method signatures
- Add support for function pointer types
- Add support for module-level ("global") functions and fields
- Add syntax highlighting for calli instruction's operand
- Add full custom modifiers support (they were previously only supported in standalone signatures)
- Add custom modifier syntax highlighting
- Add pinvokeimpl support
- Add exception block support for dynamic methods
- Add method token resolution for dynamic methods
- Support auto-completion in assembly and type combo boxes
- Improve performance for type combo box when assembly has a lot of types
- Rework search. Search results are now displayed in context menu. Method search is added.
- Format char default values as hex
- Improve custom attribute support (now custom attribute data is properly shown for any attribute)
- Improve empty method body handling. Now empty body is ignored only when method is is abstract, P/Invoke or implemented by runtime. In other cases the error message is displayed.
- Place custom attributes before default parameter values in disassembled method code
- Fix ldloc.s/stloc.s instruction handling
- Fix ldtoken instruction handling with field operand
- Fix exception when method implementation is provided by runtime
- Fix possible null reference exception when reading array/pointer of generic args
- Fix BadImageFormatException on C++/CLI assemblies
- Fix signatures with function pointers being incorrectly displayed in left panel

2.2

- Add support for `constrained.` instruction prefix
- Add type definition disassembler
- Add **Open BCL assembly** dialog
- Add navigation history
- Add partial support for 64-bit processes
- Add support for dynamic assemblies
- Add exception analysis
- Disable wrapping in search textbox
- Method navigation hyperlink now spans only over the method name identifier, instead of the whole method reference syntax
- Method navigation hyperlink is no longer underlined (to fix cases where it was obscuring _ chars in name)
- Improve performance of "Open process" by preloading assemblies from files instead of reading target process memory, where it's possible
- Fix null reference on typedref parameter
- Fix unhandled exception when opening file on background thread
- Fix token resolution bug after navigating to generic method instantiation
- Fix crashes on access to disposed assemblies

2.3

- Escape IL assembler keywords when used as identifiers
- Make search in **Open process** window case-insensitive
- Add support for displaying dynamic assembly names when inspecting process (.NET Framework only)
- Show loaded modules in process info

2.4

- Add support for opening C#/VB code and MSBuild projects
- Add **Show source** support
- Add options to include bytecode size and source code lines (from PDB) in disassembler output
- Add support for opening IL source files
- Add support for .entrypoint directive
- Add **Export type to file** menu command
- Add support for generic constraints
- Add support for disassembling properties
- Add support for interactive method execution
- Use .NET Core runtime directory when resolving dependencies for .NET Core assemblies
- Use runtime directory of the inspected process instead of current runtime directory for assembly resolution when opening a process
- Load assembly images from memory instead of files when opening a process
- When an assembly contains a single type, automatically navigate to that type
- When the type contains only one non-constructor method and no other members (like fields), automatically navigate to that single method when type is selected
- Change member identifier color to more visible with lower brightness
- Fix `ldtoken` syntax for methods
- Fix string literal escaping in disassembler to use ECMA-335 rules

2.5

- Add Source Link support for Portable PDB symbols
- Add source code syntax highlighting
- Add instruction info
- Add IL syntax highlighting in SourceViewWindow
- Add support for viewing assembly manifest and **Export assembly to file** menu command
- Add **Recent files** menu
- Add HTML help
- Change default filter in Open File dialog to include all supported file types (instead of only .dll and .exe)
- Disable formatted view for .il files larger then 1 MB
- Support viewing separate types from .il files
- Load .il files in background
- Support parameters and return type custom attributes
- Support field custom attributes
- Support `.override` and `.vtentry` directives
- Support events in type disassembler
- Support vararg sentinel (...) in method signatures
- Support `specialname` and `rtspecialname` attributes on methods
- Skip assembly name for types in the same assembly
- Escape special characters in identifiers
- Escape slash in string literals 
- Fix base type syntax in GetTypeDefSyntax
- Fix extra whitepaces after directive names
- Fix constructors to have void return type in disassembled CIL
- Fix ldtoken syntax for types
- Fix CilParserException when exception handler block closes after the last instruction in the method body
- Fix literal syntax for enum and boolean types
- Fix `serializable` attribute handling

2.5.0.2

- Fix help when running from directory with non-latin characters in path

2.6

- Add support for .pack and .size directives
- Add support for C# verbatim strings in source viewer
- Use .NET Core runtime directory for .NET Standard 2.1 targeting assemblies when navigating to methods from BCL types
- Update disassembler to not escape math symbols in string literals
- Fix syntax highlighting for constant values

2.6.1
- Fix TypeLoadException when disassembling property on a type derived from external non-BCL assembly

2.8
- Add support for type forwards
- Add support for .vtfixup directives in assembly manifest
- Add syntax highlighting support for some non-standard IlAsm keywords
- Add navigation to labels
- Add support for field offsets for structs with explicit layout
- Add support for RVA fields
- No longer automatically select <Module> type when it's the only one in assembly (so user can see assembly manifest)
- Fix members counting to pick only declared members when deciding whether the only method in type should be auto-selected

2.9
- Add support for exporting disassembled CIL as HTML
- Fix disassembling pointer-to-pointer types when used as operand of instruction
- Fix disassembling array-of-arrays types when used as operand of instruction

---------------------------------------------

Copyright (c) 2023,  MSDN.WhiteKnight
