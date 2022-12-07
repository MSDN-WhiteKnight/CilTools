*** CilTools.BytecodeAnalysis library ***
Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
License: BSD 2.0

CilTools.BytecodeAnalysis reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

Requirements: .NET Framework 3.5+ or .NET Standard 2.0+

Usage: Add reference to CilTools.BytecodeAnalysis.dll, import CilTools.BytecodeAnalysis namespace. Use CilReader.GetInstructions to get the collection of instructions from method, CilAnalysis.GetGraph to get a a graph that represents a flow of control between method's instructions, or CilAnalysis.MethodToText when you need to output method's CIL code as text. CilTools.BytecodeAnalysis.Extensions namespace provides an alternative syntax via extenstion methods.

Example:

using System;
using System.Collections.Generic;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

class Program
{
    public static void Hello()
    {
        int a = 1;
        int b = 2;
        Console.WriteLine("Hello, World");
        Console.WriteLine("{0} + {1} = {2}",a,b,a+b);
    }

    static void Main(string[] args)
    {
        IEnumerable<CilInstruction> instructions = typeof(Program).GetMethod("Hello").GetInstructions();

        foreach (CilInstruction instr in instructions)
        {
            Console.WriteLine(instr.ToString());
        }
        Console.ReadKey();
    }

}

/* Output:

nop
ldc.i4.1
stloc.0
ldc.i4.2
stloc.1
ldstr "Hello, World"
call void [mscorlib]System.Console::WriteLine(string)
nop
ldstr "{0} + {1} = {2}"
ldloc.0
box [mscorlib]System.Int32
ldloc.1
box [mscorlib]System.Int32
ldloc.0
ldloc.1
add
box [mscorlib]System.Int32
call void [mscorlib]System.Console::WriteLine(string, System.Object, System.Object, System.Object)
nop
ret
*/

v1.1
- Fixed types representation when converting instructions to text
- Fixed ldftn instruction representation when converting it to text
- Added CilAnalysis.GetReferencedMembers method

v1.2
- Added proper exception when CilReader encounters unsopprted opcode
- Documented exceptions for CilReader
- Added several new CilAnalysis.GetReferencedMembers overloads
- Added extension methods for GetReferencedMembers

v1.3
- Added support for vararg keyword
- Added support for ldtoken instruction
- Added typedref into short type names
- Fixed local variables and fields representation in generated CIL text
- Fixed types representation in generated CIL text
- Tweaked error handling
- Documented exceptions for most of the API

v1.4
- Added methods for enumerating graph nodes and instructions into CilGraph class
- Added CilInstruction.Parse method
- Added CilInstruction.EmitTo method and corresponding IlGenerator extension method

v1.5
- Moved CilInstruction.opcodes initialization from static constructor to first FindOpCode call, so it will be only loaded when needed.
- Added support for instructions referencing 32-bit target branch operands
- Added special characters escaping when displaying string literals
- Added CilGraph.EmitTo method with jumps support as well as the corresponding IlGenerator extension method

v1.6
- CilGraph.ToString now displays parameter names
- Added support for optional parameters and default values to CilGraph.ToString
- Converting constant values to string now uses InvariantCulture
- Fixed byte, sbyte, float and double type names representation in text output
- Improved representation of array types in text output

v1.7
- Fixed incorrect multidimensional array types representation in text output
- Improved representation of pointer types in text output
- CilGraph.ToString now displays method's attributes

v1.8
- Fixed handling of null arguments in some CilAnalysis methods
- Fixed handling of no-namespace types in text output
- Improved representation of char, IntPtr and UIntPtr types in text output
- Refactored and improved code related to metadata tokens resolution. Now handles generic methods and types better.
- Fixed access modifiers handling when outputting method signature
- Added support for argument-referencing instructions
- Added support for calli instruction's signature parsing
- Impoved parsing of instructions that reference local variables
- Fixed ldvirtfn opcode handling
- Added CilGraph.Print method for customizable conversion to text; refactored and improved CilGraph.ToString()

v1.9
- Fix CilGraph.EmitTo ble instructions handling
- Add filter and fault exception blocks support
- Add proper handling of duplicate try blocks
- Indent exception blocks when outtputting them as text
- Add switch instruction support

v2.0
- Rename CilBytecodeParser to CilTools.BytecodeAnalysis
- Add dynamic method parsing support
- Add syntax API
- Rework CilInstruction with generics
- Change the behaviour of CilGraph.ToString method to include only signature instead of full method code
- Add CilGraph.ToText method to return full code
- Add CilGraph.PrintSignature to output signature into target TextWriter
- Fix bug that prevented correct decoding of TypeDef or TypeSpec tokens during signature parsing
- Fix possible null refs in signature parser
- Avoid first-chance ArgumentException when resolving tokens
- Fix bug in MetadataReader.ReadCompressed
- Fix ldflda/ldsflda instructions handling

2.1
- Add .NET Standard 2.0+ support
- Add support for method implementation flags when outputting method signatures
- Add support for function pointer types. The standard reflection APIs don't support function pointers, so this only works with custom implementations; but CilTools.Metadata does implement this functionality.
- Add support for module-level ("global") functions and fields
- Add syntax API support for calli instruction's operand
- Add full custom modifiers support (they were previously only supported in standalone signatures). Only works with CilTools.Metadata or custom implementations.
- Add syntax API support for custom modifiers 
- Add pinvokeimpl support (only CilTools.Metadata or custom implementations)
- Add support for newslot and final keywords
- Add support for generic types in signature parser
- The TypeSpec class now inherits from System.Type, so it can be used in many contexts where reflection type construct is needed. The "Type" property of TypeSpec is now deprecated.
- Format char default values as hex
- Improve custom attribute support (raw attribute data now can be fetched - only CilTools.Metadata or custom implementations)
- Improve empty method body handling. Now empty body is ignored only when method is is abstract, P/Invoke or implemented by runtime. In other cases the exception is generated.
- Improve generic methods support. The CustomMethod class now has the GetDefinition method that inheritors can implement to enable fetching of method definition (and therefore parameter names)
- Place custom attributes before default parameter values in disassembled method code
- Fix ldloc.s/stloc.s instruction handling
- Fix ldtoken instruction handling with field operand
- Fix possible null reference when converting array/pointer of generic args to syntax
- Fix return type output for CustomMethod implementations

2.2
- Add support for dynamic methods on .NET Core (token resolution still does not work properly on .NET Core 3+ Linux)
- Add support for `constrained.` instruction prefix
- Add type definition disassembler
- Add CilGraphNode.GetExceptionBlocks
- Add CilGraph.GetHandlerNodes
- Implement IsAssignableFrom on TypeSpec
- Fix exception on TypeSpec.IsValueType

2.3
- Escape IL assembler keywords when used as identifiers
- Make ITypeInfo interface public to enable inspecting function pointer types with CilTools.Metadata
- Change TypeSpec.IsFunctionPointer from method to property (breaking change)
- Fix bug that prevented CilReader.GetInstructions from correctly enumerating instuctions more than once for the same iterator instance

2.4
- Add support for including bytecode size and source code lines in disassembler output
- Add support for .entrypoint directive
- Add support for generic constraints
- Add support for properties in disassembler
- Add ICustomMethod interface as a base for custom method implementations to replace CustomMethod base class. This means that custom method implementations can now be derived from MethodInfo or ConstructorInfo.
- Improve generics support. Generic context is now passed correctly to generic parameter types is more cases; this enables getting generic parameter names and their declaring methods/types.
- Fix TypeSpec.IsGenericParameter for byrefs 
- Fix ldtoken syntax for methods
- Fix string literal escaping in disassembler to use ECMA-335 rules

2.5
- Add IReflectionInfo interface to enable custom properties on reflection objects (implemented by classes in CilTools.Metadata)
- Add CilInstruction.ToSyntax()
- Add Disassembler.GetAssemblyManifestSyntaxNodes
- Add IParamsProvider interface (enables getting method parameters without resolving external assembly references, implemented by classes in CilTools.Metadata)
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
- Fix TypeSpec.IsValueType for byref target types and generics
- Fix extra whitepaces after directive names
- Fix constructors to have void return type in disassembled CIL
- Fix ldtoken syntax for types
- Fix CilParserException when exception handler block closes after the last instruction in the method body
- Fix literal syntax for enum and boolean types
- Fix `serializable` attribute handling
- Fix type name representation in syntax API (now namespace is handled as a separate identifier token)
- Fix detection of `<Module>` type (global fields and functions)
