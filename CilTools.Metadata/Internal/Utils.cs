/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
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
        public static readonly Type[] EmptyTypeArray = new Type[0];

        /// <summary>
        /// Checks two string for equality independently of current culture
        /// </summary>
        public static bool StrEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.InvariantCulture);
        }

        public static bool IsConstructorName(string name) 
        {
            return Utils.StrEquals(name, ".ctor") || Utils.StrEquals(name, ".cctor");
        }

        /// <summary>
        /// Checks two types for equality. Types are equal if they are in assemblies with the same name 
        /// and have equal full type names.
        /// </summary>
        public static bool TypeEquals(Type left, Type right)
        {
            if (ReferenceEquals(left, right)) return true;

            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            string left_assname = string.Empty;
            string right_assname = string.Empty;

            if (left.Assembly != null) left_assname = left.Assembly.GetName().Name;
            if (right.Assembly != null) right_assname = right.Assembly.GetName().Name;

            return StrEquals(left_assname, right_assname) && StrEquals(left.FullName, right.FullName);
        }

        static bool TypeEquals_GenericParameters(Type left, Type right)
        {
            bool left_isMethod = (left.DeclaringMethod != null);
            bool right_isMethod = (right.DeclaringMethod != null);

            if (left_isMethod != right_isMethod) return false;

            int left_pos = left.GenericParameterPosition;
            int right_pos = right.GenericParameterPosition;
            return left_pos == right_pos;
        }

        /// <summary>
        /// Compares two types from signatures for equality
        /// </summary>
        public static bool TypeEqualsSignature(Type left, Type right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null) return false;
            if (right == null) return false;
            
            if (left.IsByRef)
            {
                if (!right.IsByRef) return false;
                else return TypeEqualsSignature(left.GetElementType(), right.GetElementType());
            }

            if (right.IsByRef)
            {
                return false; //left is not byref at this point
            }

            //Generic parameters are special-cased so we won't hit Type.Name which might be not defined for them.
            //This prevents issues like https://github.com/MSDN-WhiteKnight/CilTools/issues/92

            if (left.IsGenericParameter)
            {
                if (!right.IsGenericParameter) return false;
                else return TypeEquals_GenericParameters(left, right);
            }

            if (right.IsGenericParameter)
            {
                return false; //left is not generic parameter at this point
            }

            //other types are compared by metadata name
            return StrEquals(left.Name, right.Name);
        }

        public static bool ParamsMatchSignature(ParameterInfo[] pars, Type[] sig) 
        {
            if (pars.Length != sig.Length) return false;

            for (int i = 0; i < pars.Length; i++) 
            {
                Type pt = pars[i].ParameterType;

                if (!TypeEqualsSignature(pt, sig[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Reads custom attributes from the specified collection and returns them as an array of 
        /// <see cref="ICustomAttribute"/> objects
        /// </summary>
        /// <param name="coll">Collection to read from</param>
        /// <param name="owner">Assembly or member that attributes are attached to</param>
        /// <param name="ass">
        /// Assembly from which attributes are read (if owner is assembly, the value of this parameter should be 
        /// the same assembly)
        /// </param>
        public static object[] ReadCustomAttributes(
            CustomAttributeHandleCollection coll, object owner, MetadataAssembly ass
            )
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            object[] ret = new object[coll.Count];
            int i = 0;

            foreach (CustomAttributeHandle h in coll)
            {
                CustomAttribute ca = ass.MetadataReader.GetCustomAttribute(h);
                EntityHandle eh = ca.Constructor;
                MethodBase constr = null;

                if (eh.Kind == HandleKind.MethodDefinition)
                {
                    constr = ass.GetMethodDefinition((MethodDefinitionHandle)eh);
                }
                else if (eh.Kind == HandleKind.MemberReference)
                {
                    MemberReference mref = ass.MetadataReader.GetMemberReference((MemberReferenceHandle)eh);

                    if (mref.GetKind() == MemberReferenceKind.Method)
                        constr = CreateMethodFromReference(mref, (MemberReferenceHandle)eh, ass);
                }

                ret[i] = new MetadataCustomAttribute(
                    owner, constr, ass.MetadataReader.GetBlobBytes(ca.Value)
                    );
                i++;
            }

            return ret;
        }

        public static ParameterInfo[] GetParametersFromSignature(Signature sig, MemberInfo owner)
        {
            ParameterInfo[] pars = new ParameterInfo[sig.ParamsCount];

            for (int i = 0; i < pars.Length; i++)
            {
                pars[i] = new ParameterSpec(sig.GetParamType(i), i, owner);
            }

            return pars;
        }

        public static Signature DecodeSignature(MetadataAssembly owner, BlobHandle bh, Type declaringType) 
        {
            byte[] sig = owner.MetadataReader.GetBlobBytes(bh);
            GenericContext gctx = GenericContext.Create(declaringType, null);
            SignatureContext ctx = SignatureContext.Create(owner, gctx, null);
            return Signature.ReadFromArray(sig, ctx);
        }

        /// <summary>
        /// Creates a new method reference object (<see cref="MethodRef"/> or <see cref="ConstructorRef"/>).
        /// </summary>
        public static MethodBase CreateMethodFromReference(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            string name = owner.MetadataReader.GetString(m.Name);

            if (Utils.IsConstructorName(name)) return new ConstructorRef(m, mh, owner);
            else return new MethodRef(m, mh, owner);
        }

        /// <summary>
        /// Creates a new method definition object (<see cref="MethodDef"/> or <see cref="ConstructorDef"/>).
        /// </summary>
        public static MethodBase CreateMethodFromDefinition(MethodDefinition m, MethodDefinitionHandle mh, MetadataAssembly owner)
        {
            string name = owner.MetadataReader.GetString(m.Name);

            if (Utils.IsConstructorName(name)) return new ConstructorDef(m, mh, owner);
            else return new MethodDef(m, mh, owner);
        }

        /// <summary>
        /// Creates an array of parameters for the specified method definition
        /// </summary>
        public static ParameterInfo[] GetMethodParameters(MetadataReader reader, MethodBase method, MethodDefinition mdef, Signature sig)
        {
            ParameterInfo[] pars = new ParameterInfo[sig.ParamsCount];
            ParameterHandleCollection hcoll = mdef.GetParameters();

            foreach (ParameterHandle h in hcoll)
            {
                Parameter par = reader.GetParameter(h);
                int index = par.SequenceNumber - 1;
                if (index >= pars.Length) continue;
                if (index < 0) continue;

                pars[index] = new ParameterSpec(sig.GetParamType(index), par, method, reader);
            }

            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i] == null) pars[i] = new ParameterSpec(sig.GetParamType(i), i, method);
            }

            return pars;
        }

        /// <summary>
        /// Reads exception handling blocks from the specified method body
        /// </summary>
        public static ExceptionBlock[] GetMethodExceptionBlocks(MethodBodyBlock mb, MetadataAssembly ownerAssembly)
        {
            ExceptionBlock[] ret = new ExceptionBlock[mb.ExceptionRegions.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ExceptionHandlingClauseOptions opt = (ExceptionHandlingClauseOptions)0;
                Type t = null;

                switch (mb.ExceptionRegions[i].Kind)
                {
                    case ExceptionRegionKind.Catch:
                        opt = ExceptionHandlingClauseOptions.Clause;
                        EntityHandle eh = mb.ExceptionRegions[i].CatchType;

                        if (eh.Kind == HandleKind.TypeDefinition)
                        {
                            t = ownerAssembly.GetTypeDefinition((TypeDefinitionHandle)eh);
                        }
                        else if (eh.Kind == HandleKind.TypeReference)
                        {
                            t = new TypeRef(
                                ownerAssembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh),
                                (TypeReferenceHandle)eh,
                                ownerAssembly);
                        }

                        break;
                    case ExceptionRegionKind.Finally: opt = ExceptionHandlingClauseOptions.Finally; break;
                    case ExceptionRegionKind.Filter: opt = ExceptionHandlingClauseOptions.Filter; break;
                    case ExceptionRegionKind.Fault: opt = ExceptionHandlingClauseOptions.Fault; break;
                }

                ret[i] = new ExceptionBlock(
                    opt, mb.ExceptionRegions[i].TryOffset, mb.ExceptionRegions[i].TryLength, t,
                    mb.ExceptionRegions[i].HandlerOffset, mb.ExceptionRegions[i].HandlerLength,
                    mb.ExceptionRegions[i].FilterOffset);
            }

            return ret;
        }

        /// <summary>
        /// Get declaring type for method reference based on parent <see cref="EntityHandle"/>
        /// </summary>        
        public static Type GetRefDeclaringType(MetadataAssembly ass, MethodBase methodRef, EntityHandle eh)
        {
            if (!eh.IsNil && eh.Kind == HandleKind.TypeReference)
            {
                return new TypeRef(
                    ass.MetadataReader.GetTypeReference((TypeReferenceHandle)eh), (TypeReferenceHandle)eh, ass
                    );
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.TypeSpecification)
            {
                //TypeSpec is either complex type (array etc.) or generic instantiation

                TypeSpecification ts = ass.MetadataReader.GetTypeSpecification(
                    (TypeSpecificationHandle)eh
                    );

                TypeSpec encoded = TypeSpec.ReadFromArray(ass.MetadataReader.GetBlobBytes(ts.Signature),
                    ass, methodRef);

                if (encoded != null) return encoded.Type;
                else return UnknownType.Value;
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = ass.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)eh);
                TypeDefinitionHandle tdefh = mdef.GetDeclaringType();

                if (!tdefh.IsNil) return ass.GetTypeDefinition(tdefh);
                else return UnknownType.Value;
            }
            else return UnknownType.Value;
        }

        /// <summary>
        /// Loads method definition corresponding to the specified method reference (MethodRef or ConstructorRef). 
        /// This method attempts to resolve passed external type and find matching method on it.
        /// </summary>
        /// <returns>
        /// Resolved method, or null if type could not be resolved or does not contain the matching method
        /// </returns>
        public static MethodBase ResolveMethodRef(MetadataAssembly ass, Type et, MethodBase methodRef, Signature sig)
        {
            Type t = ass.AssemblyReader.LoadType(et);

            if (t == null) return null;

            MemberInfo[] members = t.GetMember(methodRef.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                );

            //if there's only one method, pick it
            if (members.Length == 1 && members[0] is MethodBase)
            {
                return (MethodBase)members[0];
            }

            //if there are multiple methods with the same name, match by signature
            ParameterInfo[] pars_match;
            bool isstatic_match = false;
            int genargs_match = 0;

            if (sig != null)
            {
                pars_match = Utils.GetParametersFromSignature(sig, methodRef);
                isstatic_match = !sig.HasThis;
                genargs_match = sig.GenericArgsCount;
            }
            else
            {
                pars_match = new ParameterInfo[0];
            }

            bool match;

            for (int i = 0; i < members.Length; i++)
            {
                if (!(members[i] is MethodBase)) continue;

                MethodBase m = (MethodBase)members[i];
                ParameterInfo[] pars_i = m.GetParameters();

                if (m.IsStatic != isstatic_match) continue;

                if (pars_i.Length != pars_match.Length) continue;

                //compare generic args count
                Type[] ga = m.GetGenericArguments();
                int genargs = 0;

                if (ga != null) genargs = ga.Length;
                else genargs = 0;

                if (genargs != genargs_match) continue;

                //compare parameter types
                match = true;

                for (int j = 0; j < pars_i.Length; j++)
                {
                    Type pt1 = pars_i[j].ParameterType;
                    Type pt2 = pars_match[j].ParameterType;

                    if (!Utils.TypeEqualsSignature(pt1, pt2))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return m;
                }
            }//end for

            return null;
        }

        public static Type[] GetGenericParameters(MetadataAssembly assembly, MemberInfo declMember,
            GenericParameterHandleCollection hcoll)
        {
            Type[] ret = new Type[hcoll.Count];

            for (int i = 0; i < ret.Length; i++)
            {
                GenericParameter gp = assembly.MetadataReader.GetGenericParameter(hcoll[i]);
                StringHandle sh = gp.Name;
                GenericParameterConstraintHandleCollection gpchc = gp.GetConstraints();
                Type[] constrains = new Type[gpchc.Count];
                string name;

                if (!sh.IsNil) name = assembly.MetadataReader.GetString(sh);
                else name = string.Empty;

                for (int j = 0; j < gpchc.Count; j++)
                {
                    GenericParameterConstraint cons = assembly.MetadataReader.GetGenericParameterConstraint(gpchc[j]);
                    Type tCons = assembly.ResolveType(MetadataTokens.GetToken(assembly.MetadataReader, cons.Type));
                    if (tCons == null) tCons = UnknownType.Value;
                    constrains[j] = tCons;
                }

                ret[i] = GenericParamType.Create(declMember, gp.Index, name, gp.Attributes, constrains);
            }

            return ret;
        }
    }
}
