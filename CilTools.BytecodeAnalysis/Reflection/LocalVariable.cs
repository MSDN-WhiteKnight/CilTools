/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents local variable declaration in the method body
    /// </summary>
    public struct LocalVariable
    {
        MethodBase method;
        TypeSpec type;
        int index;

        /// <summary>
        /// Initializes a new LocalVariable instance
        /// </summary>
        /// <param name="m">Method in which this local variable is declared</param>
        /// <param name="ptype">TypeSpec object representing the variable type</param>
        /// <param name="pindex">Index of local variable within method body</param>
        internal LocalVariable(MethodBase m, TypeSpec ptype, int pindex)
        {
            this.type = ptype;
            this.index = pindex;
            this.method = m;
        }

        /// <summary>
        /// Gets the TypeSpec object representing the variable type
        /// </summary>
        public TypeSpec LocalTypeSpec { get { return this.type; } }

        /// <summary>
        /// Gets the variable type
        /// </summary>
        public Type LocalType { get { return this.type.Type; } }

        /// <summary>
        /// Gets the index of local variable within the containing method body
        /// </summary>
        public int LocalIndex { get { return this.index; } }

        /// <summary>
        /// Gets the value indicating whether the object pointed by local variable is pinned in memory
        /// </summary>
        public bool IsPinned { get { return this.type.IsPinned; } }

        /// <summary>
        /// Gets the method in which this local variable is declared
        /// </summary>
        public MethodBase Method { get { return this.method; } }

        static LocalVariable[] ReadSignatureImpl(MethodBase m, byte[] data, SignatureContext ctx)
        {
            MemoryStream ms = new MemoryStream(data);
            LocalVariable[] ret;

            using (ms)
            {
                byte b = MetadataReader.ReadByte(ms);

                if (b != 0x07) throw new InvalidDataException("Invalid local variables signature");

                uint paramcount = MetadataReader.ReadCompressed(ms);
                ret = new LocalVariable[paramcount];

                for (int i = 0; i < paramcount; i++)
                {
                    TypeSpec t = TypeSpec.ReadFromStream(ms, ctx, null);
                    ret[i] = new LocalVariable(m, t, i);
                }

                return ret;
            }
        }

        /// <summary>
        /// Reads local variables from the specified signature, resolving tokens using the specified ITokenResolver
        /// </summary>
        /// <param name="data">Local variable signature as byte array</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <returns>An array of local variables read from the signature</returns>
        /// <exception cref="System.ArgumentNullException">Input array is null</exception>
        /// <exception cref="NotSupportedException">Signature contains unsupported types</exception>
        public static LocalVariable[] ReadSignature(byte[] data, ITokenResolver resolver)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) return new LocalVariable[0];

            return ReadSignatureImpl(null, data, SignatureContext.FromResolver(resolver));
        }

        /// <summary>
        /// Reads local variables from the specified method signature
        /// </summary>
        /// <param name="m">Method which local variables are read</param>
        /// <param name="data">Local variable signature as byte array</param>
        /// <param name="ctx">Signature context information</param>
        /// <returns>An array of local variables read from the signature</returns>
        /// <exception cref="ArgumentNullException">Method, input array or signature context is null</exception>
        /// <exception cref="NotSupportedException">Signature contains unsupported types</exception>
        public static LocalVariable[] ReadMethodSignature(MethodBase m, byte[] data, SignatureContext ctx)
        {
            if (m == null) throw new ArgumentNullException("m");
            if (data == null) throw new ArgumentNullException("data");
            if (ctx == null) throw new ArgumentNullException("ctx");

            if (data.Length == 0) return new LocalVariable[0];

            return ReadSignatureImpl(m, data, ctx);
        }

        /// <summary>
        /// Reads local variables from the specified signature, resolving tokens using the specified 
        /// <see cref="ITokenResolver"/> in a generic context identified by the specified member reference.
        /// </summary>
        /// <param name="data">Local variable signature as byte array</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <param name="member">Method that identifies generic context for generic method params, or null if 
        /// this signature does not belong to a generic method</param>
        /// <returns>An array of local variables read from the signature</returns>
        /// <exception cref="System.ArgumentNullException">Input array is null</exception>
        /// <exception cref="NotSupportedException">Signature contains unsupported types</exception>
        public static LocalVariable[] ReadSignature(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) return new LocalVariable[0];

            GenericContext gctx = GenericContext.FromMember(member);
            SignatureContext ctx = new SignatureContext(resolver, gctx, null);
            return ReadSignatureImpl(null, data, ctx);
        }

        /// <summary>
        /// Reads local variables from the specified signature, resolving tokens within the scope of the specified module
        /// </summary>
        /// <param name="data">Local variable signature as byte array</param>
        /// <param name="module">Module in which to resolve metadata tokens</param>
        /// <returns>An array of local variables read from the signature</returns>
        /// <exception cref="System.ArgumentNullException">Input array is null</exception>
        /// <exception cref="NotSupportedException">Signature contains unsupported types</exception>
        public static LocalVariable[] ReadSignature(byte[] data, Module module)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) return new LocalVariable[0];

            ModuleWrapper mwr = new ModuleWrapper(module);
            return ReadSignatureImpl(null, data, SignatureContext.FromResolver(mwr));
        }

        /// <summary>
        /// Reads local variables from the specified method's body
        /// </summary>
        /// <param name="mb">Source method</param>
        /// <returns>An array of local variables read from the method body</returns>
        internal static LocalVariable[] FromReflection(MethodBase mb)
        {
            Debug.Assert(mb != null, "Source method cannot be null");

            MethodBody body = mb.GetMethodBody();

            if (body == null) return new LocalVariable[0];

            LocalVariable[] ret = new LocalVariable[body.LocalVariables.Count];

            for (int i = 0; i < body.LocalVariables.Count; i++)
            {
                ret[i] = new LocalVariable(mb, 
                    TypeSpec.FromType(body.LocalVariables[i].LocalType, body.LocalVariables[i].IsPinned),
                    i);
            }

            return ret;
        }
    }
}
