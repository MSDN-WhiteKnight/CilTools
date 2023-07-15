# Inspecting function pointer types with CilTools.Metadata

> [!NOTE]
> .NET Runtime intoduced the function pointers inspection support in .NET 8 Preview 2 (both for runtime reflection and MetadataLoadContext). You might not need APIs described in this article if you are using modern .NET versions. For more information, see [System.Reflection: introspection support for function pointers](https://github.com/dotnet/core/issues/8134#issuecomment-1444103530).

The .NET platform from the start had supported function types that enable developers to pass around and store addresses of functions (mostly useful for interop with native C++ code). Initially only Visual C++ compiler emitted function pointer types, but starting from the version 9.0 C# compiler takes advantage of them as well. However, the runtime reflection functionality is severely lacking in its ability to handle function pointers (now as of .NET 5). The standard reflection implementation substitutes IntPtr type when encountering them, MetadataLoadContext just dies with NotSupportedException. The CIL Tools suite added the API to support inspecting function pointers in the version 2.3 to help fill this gap.

## Prerequisites

To inspect function pointers, install the [CilTools.Metadata](https://www.nuget.org/packages/CilTools.Metadata/) NuGet package version 2.3 or above. You can also build the library from sources or download released binaries in [CIL Tools repository](https://github.com/MSDN-WhiteKnight/CilTools). The minimum target framework is .NET Framework 4.5 or .NET Standard 2.0.

## Overview

The function pointer support is provided via the <xref:CilTools.Reflection.ITypeInfo> interface. Some APIs in CilTools.Metadata, such as `ParameterInfo.ParameterType` property on objects loaded using this library, could return `System.Type` instances that implements this interface. Cast them to the interface using `is`/`as` C# operators and use <xref:CilTools.Reflection.ITypeInfo.IsFunctionPointer> property to determine whether a type is a function pointer type. If it is the case, you could use <xref:CilTools.Reflection.ITypeInfo.TargetSignature> property to get information about the signature of the function this type refers to.

## Example

The example below shows how to inspect function pointer types on the example of the `bsearch` function from the WPF's *PresentationCore.dll* library. WPF is one of the most famous C++/CLI consumers, and this *PresentationCore.dll* is a module partially written in C++/CLI to help with DirectWrite interop. The `bsearch` function that was statically embedded into this module is a standard C library function which performs binary search using the supplied comparer callback. Its signature looks as follows in CIL:

```
.method  assembly static void* bsearch(
    void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* key, 
    void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* base, 
    uint32 num, 
    uint32 width, 
    method int32 *( void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*, 
        void modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*
        ) compare
    ) cil managed
```

You can see that the fifth parameter is a function pointer type. Here's the example how to read infomation about this function's params, including the function pointer signature, and print it into the console:

```csharp
using System;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Metadata;

namespace ExampleFunctionPointers
{
    class Program
    {
        static void PrintType(Type t) 
        {
            if (t is ITypeInfo)
            {
                ITypeInfo ti = (ITypeInfo)t;

                if (ti.IsFunctionPointer)
                {
                    Console.WriteLine("Function pointer type.");
                    Signature sig = ti.TargetSignature;
                    Console.WriteLine("Return type: " + sig.ReturnType.FullName);
                    Console.WriteLine("Calling convention: " + sig.CallingConvention.ToString());
                    Console.WriteLine("Parameters: " + sig.ParamsCount.ToString());

                    for (int i = 0; i < sig.ParamsCount; i++)
                    {
                        TypeSpec paramType = sig.GetParamType(i);
                        Console.WriteLine(" Parameter {0} type: {1}", i, paramType.FullName);
                    }
                }
                else 
                {
                    Console.WriteLine(t.FullName);
                }
            }
        }

        static void Main()
        {
            // Path to .NET Framework's PresentationCore
            string path = "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\WPF\\PresentationCore.dll";
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                // Load function from the assembly
                // (the function is exposed via the special <Module> type used to host global functions)
                Assembly ass = reader.LoadFrom(path);
                Type mt = ass.GetType("<Module>");
                MethodBase mi = mt.GetMember(
                    "bsearch",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                    )[0] as MethodBase;
                
                ParameterInfo[] pars = mi.GetParameters();

                // Print information about function parameters

                for (int i = 0; i < pars.Length; i++) 
                {
                    Console.WriteLine("Parameter "+i.ToString());
                    PrintType(pars[i].ParameterType);
                    Console.WriteLine();
                }
            }

            Console.ReadLine();
        }
    }
}

/* Output:
Parameter 0
System.Void*

Parameter 1
System.Void*

Parameter 2
System.UInt32

Parameter 3
System.UInt32

Parameter 4
Function pointer type.
Return type: System.Int32
Calling convention: Default
Parameters: 2
 Parameter 0 type: System.Void*
 Parameter 1 type: System.Void*
*/
```
