/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Metadata;
using CilTools.Metadata.Constructors;
using CilTools.Metadata.Methods;
using CilTools.Reflection;

namespace CilTools.Internal
{
    internal static class Utils
    {
        static bool IsCorelibName(string assemblyName)
        {
            return StrEquals(assemblyName, "mscorlib") || StrEquals(assemblyName, "System.Runtime") ||
                StrEquals(assemblyName, "System.Private.CoreLib");
        }

        public static bool IsValueTypeBase(EntityHandle eh, MetadataAssembly context)
        {
            string assname = string.Empty;
            string ns = string.Empty;
            string name = string.Empty;

            if (eh.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinitionHandle tdh = (TypeDefinitionHandle)eh;
                Type t = context.GetTypeDefinition(tdh);
                assname = context.GetName().Name;
                ns = t.Namespace;
                name = t.Name;
            }
            else if (eh.Kind == HandleKind.TypeReference)
            {
                TypeReferenceHandle trh = (TypeReferenceHandle)eh;
                TypeReference tref = context.MetadataReader.GetTypeReference(trh);
                name = context.MetadataReader.GetString(tref.Name);
                ns = context.MetadataReader.GetString(tref.Namespace);

                if (tref.ResolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    AssemblyReferenceHandle arh = (AssemblyReferenceHandle)tref.ResolutionScope;
                    AssemblyReference aref = context.MetadataReader.GetAssemblyReference(arh);
                    assname = context.MetadataReader.GetString(aref.Name);
                }
            }

            return IsCorelibName(assname) && StrEquals(ns, "System") &&
                (StrEquals(name, "ValueType") || StrEquals(name, "Enum"));
        }
    }
}
