# Using CilTools.Metadata

CilTools.Metadata library enables you to inspect the contents of .NET assemblies without loading them to the execution context. The assemlies are inspected using the same API surface as standard reflection, but operating on artificial informational-only `System.Reflection.Assembly` instances, though some reflection API are not available on them. These instances can be unloaded from memory using `Dispose` method when they are no longer needed.

With CilTools.Metadata you can load assemblies for another target framework, or assemblies with unresolved dependencies, but if some member references an external member which is not available, you might be unable to read information about that missing member. For example, if the field type is declared in the missing assembly, you will be able to read the name of that type, but not the list of its members.

## Prerequisites

To use CilTools.Metadata, install [CilTools.Metadata](https://www.nuget.org/packages/CilTools.Metadata/) NuGet package. You can also build the library from sources or download released binaries in [CIL Tools repository](https://github.com/MSDN-WhiteKnight/CilTools). The minimum target framework is .NET Framework 4.5 or .NET Standard 2.0.

## Enumerating methods in an assembly

To read assembly information, use <xref:CilTools.Metadata.AssemblyReader> class. It provides `Load` and `LoadFrom` methods that returns `Assembly` instances for a given assembly name or file path, respectively. Then you can work with that assemblies using regular reflection methods, such as `Assembly.GetTypes`. When you no longer need loaded assemblies, release them with `Dispose` method or `using` block.

To enumerate methods in the given type, use `Type.GetMembers` method and filter out methods from the returned list. The returned method instances derive from <xref:CilTools.Reflection.CustomMethod>, which in turn is derived from `MethodBase` class. `Type.GetMethods` and `Type.GetConstructors` are not supported.

The following example shows how to display methods from the given assembly:

```csharp
using System;
using System.Reflection;
using CilTools.Metadata;

//...

static void PrintMethods(string assemblyPath)
{
    AssemblyReader reader = new AssemblyReader();

    using (reader)
    {
        Assembly ass = reader.LoadFrom(assemblyPath);
        Type[] types = ass.GetTypes();

        for(int n=0;n<types.Length;n++)
        {
            Type t = types[n];
            MemberInfo[] members = t.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
            );

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] is MethodBase)
                {
                    Console.WriteLine(t.Name+"."+members[i].Name);
                }
            }
        }
    }
}

```

## Getting P/Invoke parameters for the unmanaged method

To get P/Invoke parameters for the method imported from unmanaged library, such as a DLL name or calling convention, use the <xref:CustomMethod.GetPInvokeParams%2A> method. The following example displays P/Invoke information for all unmanaged methods in the given assembly:

```csharp
using System;
using System.Reflection;
using CilTools.Metadata;
using CilTools.Reflection;

//...

static void PrintPInvokeParams(string assemblyPath)
{
    AssemblyReader reader = new AssemblyReader();

    using (reader)
    {
        Assembly ass = reader.LoadFrom(assemblyPath);
        Type[] types = ass.GetTypes();

        for (int n = 0; n < types.Length; n++)
        {
            Type t = types[n];
            MemberInfo[] members = t.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] is CustomMethod &&
                    ((CustomMethod)members[i]).Attributes.HasFlag(MethodAttributes.PinvokeImpl))
                {
                    PInvokeParams pars = ((CustomMethod)members[i]).GetPInvokeParams();
                    Console.WriteLine(t.Name + "." + members[i].Name);
                    Console.WriteLine("Module: "+pars.ModuleName);
                    Console.WriteLine("Calling convention: " + pars.CallingConvention.ToString());        
                }
            }
        }
    }
}
```

## See also

<xref:CilTools.Metadata> API reference

[Assemblies in .NET](https://docs.microsoft.com/en-us/dotnet/standard/assembly/) on Microsoft Docs
