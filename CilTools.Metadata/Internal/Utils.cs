/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Metadata;
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
                        constr = MethodRef.CreateReference(mref, (MemberReferenceHandle)eh, ass);
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
            SignatureContext ctx = new SignatureContext(owner, gctx);
            return Signature.ReadFromArray(sig, ctx);
        }
    }
}
