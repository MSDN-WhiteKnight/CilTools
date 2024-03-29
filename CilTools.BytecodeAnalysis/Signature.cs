﻿/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Syntax;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Encapsulates function's return type, calling convention and parameter types
    /// </summary>
    public class Signature
    {
        const int CALLCONV_MASK = 0x0F; //bitmask to extract calling convention from first byte of signature
        const int MFLAG_HASTHIS = 0x20; //instance
        const int MFLAG_EXPLICITTHIS = 0x40; //explicit
        const int MFLAG_GENERIC = 0x10; //method has generic parameters
        const int MFLAG_GENERICINST = 0x0A; //generic method instantiation

        CallingConvention _conv;
        bool _HasThis;
        bool _ExplicitThis;
        int _GenParamCount;
        bool _GenInst;
        TypeSpec _ReturnType;
        TypeSpec[] _ParamTypes;
        SignatureContext _ctx;
        int _sentinelPos;

        /// <summary>
        /// Initializes a new Signature object representing a stand-alone method signature
        /// </summary>
        /// <param name="data">The byte array containing StandAloneMethodSig data (ECMA-335 II.23.2.3)</param>
        /// <param name="module">Module containing the passed signature</param>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of input data</exception>
        /// <exception cref="CilParserException">Input data is invalid</exception>
        /// <exception cref="System.NotSupportedException">Signature contains unsupported elements</exception>
        public Signature(byte[] data, Module module) //ECMA-335 II.23.2.3: StandAloneMethodSig
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            ModuleWrapper mwr = new ModuleWrapper(module);
            Initialize(data, SignatureContext.FromResolver(mwr));
        }

        /// <summary>
        /// Initializes a new signature object, resolving metadata tokens using the specified resolver
        /// </summary>
        /// <param name="data">The byte array containing the signature data</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of input data</exception>
        /// <exception cref="CilParserException">Input data is invalid</exception>
        /// <exception cref="System.NotSupportedException">Signature contains unsupported elements</exception>
        /// <remarks>The signature could be the method signature or the standalone signature 
        /// (ECMA-335 II.23.2.3: StandAloneMethodSig)</remarks>
        public Signature(byte[] data, ITokenResolver resolver)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            Initialize(data, SignatureContext.FromResolver(resolver));
        }

        /// <summary>
        /// Initializes a new signature object, resolving metadata tokens using the specified resolver in 
        /// the specified generic context
        /// </summary>
        /// <param name="data">The byte array containing the signature data</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <param name="member">Method that identifies generic context for generic method params, or null 
        /// if this signature does not belong to a generic method</param>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of input data</exception>
        /// <exception cref="CilParserException">Input data is invalid</exception>
        /// <exception cref="System.NotSupportedException">Signature contains unsupported elements</exception>
        /// <remarks>The signature could be the method signature or the standalone signature 
        /// (ECMA-335 II.23.2.3: StandAloneMethodSig)</remarks>
        public Signature(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            GenericContext gctx = GenericContext.FromMember(member);
            SignatureContext ctx = new SignatureContext(resolver, gctx, genericDefinition: null);
            this.Initialize(data, ctx);
        }

        /// <summary>
        /// Initializes a new signature object from the stream, resolving metadata tokens using the specified 
        /// resolver in the specified generic context
        /// </summary>
        /// <param name="src">The stream to read signature data from</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <param name="member">Method that identifies generic context for generic method params, or null 
        /// if this signature does not belong to a generic method</param>
        /// <exception cref="System.ArgumentNullException">Source stream is null</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of input data</exception>
        /// <exception cref="CilParserException">Input data is invalid</exception>
        /// <exception cref="System.NotSupportedException">Signature contains unsupported elements</exception>
        /// <remarks>The signature could be the method signature or the standalone signature 
        /// (ECMA-335 II.23.2.3: StandAloneMethodSig)</remarks>
        public Signature(Stream src, ITokenResolver resolver, MemberInfo member)
        {
            if (src == null) throw new ArgumentNullException("src", "Source stream cannot be null");

            GenericContext gctx = GenericContext.FromMember(member);
            SignatureContext ctx = new SignatureContext(resolver, gctx, genericDefinition: null);
            this.Initialize(src, ctx);
        }

        Signature(byte[] data, SignatureContext ctx)
        {
            this.Initialize(data, ctx);
        }

        /// <summary>
        /// Reads a signature from the specified byte array using the specified signature context
        /// </summary>
        /// <param name="data">Byte array to read data from</param>
        /// <param name="ctx">The signature context</param>
        /// <remarks><para>The signature could be the method signature or the standalone signature 
        /// (ECMA-335 II.23.2.3: StandAloneMethodSig)</para>
        /// <para>A signature context encapsulates a token resolver and a generic context. Generic context is only used 
        /// when the signature contains generic parameters.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Source array or signature context is null</exception>
        /// <exception cref="ArgumentException">Source array is empty</exception>
        public static Signature ReadFromArray(byte[] data, SignatureContext ctx)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");
            if (ctx == null) throw new ArgumentNullException("ctx");

            return new Signature(data, ctx);
        }

        internal Signature(Stream src, SignatureContext ctx)
        {
            this.Initialize(src, ctx);
        }

        void Initialize(Stream src, SignatureContext ctx)
        {
            this._ctx = ctx;
            byte b = MetadataReader.ReadByte(src); //calling convention & method flags
            int conv = b & CALLCONV_MASK;
            this._conv = (CallingConvention)conv;

            if ((b & MFLAG_HASTHIS) == MFLAG_HASTHIS) this._HasThis = true;

            if ((b & MFLAG_EXPLICITTHIS) == MFLAG_EXPLICITTHIS) this._ExplicitThis = true;

            if ((b & MFLAG_GENERICINST) == MFLAG_GENERICINST)
            {
                //generic method instantiation
                this._GenInst = true;
                int genparams = (int)MetadataReader.ReadCompressed(src);
                this._ParamTypes = new TypeSpec[genparams];
                this._GenParamCount = genparams;

                for (int i = 0; i < genparams; i++)
                {
                    this._ParamTypes[i] = TypeSpec.ReadFromStream(src, ctx, parentGenericDefinition: null);
                }
            }
            else
            {
                if ((b & MFLAG_GENERIC) == MFLAG_GENERIC)
                {
                    //generic method definition
                    this._GenParamCount = (int)MetadataReader.ReadCompressed(src);
                }

                uint paramcount = MetadataReader.ReadCompressed(src);
                this._ParamTypes = new TypeSpec[paramcount];
                this._ReturnType = TypeSpec.ReadFromStream(src, ctx, parentGenericDefinition: null);
                int sentinelPos = -1;

                for (int i = 0; i < paramcount; i++)
                {
                    TypeSpec paramTypeSpec = TypeSpec.ReadFromStream(src, ctx, parentGenericDefinition: null);

                    if (paramTypeSpec.ElementType == ElementType.Sentinel)
                    {
                        //skip vararg sentinel
                        sentinelPos = i;
                        paramTypeSpec = TypeSpec.ReadFromStream(src, ctx, parentGenericDefinition: null);
                    }

                    this._ParamTypes[i] = paramTypeSpec;
                }

                if (this._conv == CallingConvention.Vararg && sentinelPos == -1)
                {
                    sentinelPos = (int)paramcount; //vararg sentinel is after last parameter 
                }

                this._sentinelPos = sentinelPos;
            }
        }

        void Initialize(byte[] data, SignatureContext ctx)
        {
            MemoryStream ms = new MemoryStream(data);

            using (ms)
            {
                this.Initialize(ms, ctx);
            }
        }

        /// <summary>
        /// Reads the field signature from the specified byte array
        /// </summary>
        /// <param name="data">The byte array containing the field signature data</param>
        /// <param name="resolver">The object used to resolve metadata tokens</param>
        /// <param name="member">Method that identifies generic context for generic method params, or null 
        /// if this signature does not belong to a generic method</param>
        /// <returns>The <c>TypeSpec</c> representing the field type</returns>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="InvalidDataException">
        /// The signature data does not represent the field signature
        /// </exception>
        /// <remarks>
        /// The field signature in .NET assembly consists of only the single <see cref="TypeSpec"/> that 
        /// represents the field type. The signature data passed to this method should not contain 
        /// data for signature types other then the field signature (such as the method signature), or 
        /// an exception will be thrown.
        /// </remarks>
        public static TypeSpec ReadFieldSignature(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            MemoryStream ms = new MemoryStream(data);
            GenericContext gctx = GenericContext.FromMember(member);
            SignatureContext ctx = new SignatureContext(resolver, gctx, genericDefinition: null);

            using (ms)
            {
                byte b = MetadataReader.ReadByte(ms); //signature kind

                if (b != 0x6) throw new InvalidDataException("Invalid field signature");

                return TypeSpec.ReadFromStream(ms, ctx, parentGenericDefinition: null);
            }
        }

        /// <summary>
        /// Returns calling convention of the function described by this signature
        /// </summary>
        public CallingConvention CallingConvention { get { return this._conv; } }

        /// <summary>
        /// Gets the value indicating whether the function described by this signature uses an instance pointer
        /// </summary>
        public bool HasThis { get { return this._HasThis; } }

        /// <summary>
        /// Gets the value indicating whether the instance pointer is included explicitly in this signature
        /// </summary>
        public bool ExplicitThis { get { return this._ExplicitThis; } }

        /// <summary>
        /// Gets the value indicating whether this signature represents the generic method instantiation
        /// </summary>
        public bool GenericInst { get { return this._GenInst; } }

        /// <summary>
        /// Gets the return type of the function described by this signature
        /// </summary>
        public TypeSpec ReturnType { get { return this._ReturnType; } }

        /// <summary>
        /// Gets the amount of fixed parameters that the function described by this signature takes
        /// </summary>
        public int ParamsCount { get { return this._ParamTypes.Length; } }

        /// <summary>
        /// Gets the generic arguments or parameters count for the generic method, or zero if this 
        /// signature does not represent the generic method.
        /// </summary>
        public int GenericArgsCount { get { return this._GenParamCount; } }

        internal SignatureContext Context { get { return this._ctx; } }

        internal int SentinelPosition { get { return this._sentinelPos; } }

        /// <summary>
        /// Gets the type of parameter with the specified index
        /// </summary>
        /// <param name="index">Index of the requested parameter</param>
        /// <returns>The type of requested parameter</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Index is negative or outside the bounds of collection</exception>
        public TypeSpec GetParamType(int index)
        {
            if (index < 0 || index >= this._ParamTypes.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index must be non-negative and within the size of collection");
            }

            return this._ParamTypes[index];
        }

        /// <summary>
        /// Enumerates types of fixed parameters that the function described by this signature takes
        /// </summary>
        public IEnumerable<TypeSpec> ParamTypes
        {
            get
            {
                for (int i = 0; i < this._ParamTypes.Length; i++)
                {
                    yield return this._ParamTypes[i];
                }
            }
        }

        /// <summary>
        /// Gets the array of fixed parameter types that the function described by this signature takes
        /// </summary>
        /// <returns></returns>
        public TypeSpec[] GetParamTypes()
        {
            TypeSpec[] res = new TypeSpec[this._ParamTypes.Length];
            this._ParamTypes.CopyTo(res, 0);
            return res;
        }

        /// <summary>
        /// Gets the textual representation of this signature as CIL code
        /// </summary>        
        public override string ToString()
        {
            return this.ToString(pointer: false, containingAssembly: null);
        }

        internal string ToString(bool pointer, Assembly containingAssembly)
        {
            StringBuilder sb_sig = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb_sig);

            foreach (SyntaxNode node in this.ToSyntax(pointer, containingAssembly))
            {
                node.ToText(wr);
            }

            wr.Flush();
            return sb_sig.ToString();
        }

        internal IEnumerable<SyntaxNode> ToSyntax(bool pointer, Assembly containingAssembly)
        {
            switch (this._conv)
            {
                case CallingConvention.CDecl:
                    yield return new KeywordSyntax("unmanaged", " ");
                    yield return new KeywordSyntax("cdecl", " ");
                    break;
                case CallingConvention.StdCall:
                    yield return new KeywordSyntax("unmanaged", " ");
                    yield return new KeywordSyntax("stdcall", " ");
                    break;
                case CallingConvention.ThisCall:
                    yield return new KeywordSyntax("unmanaged", " ");
                    yield return new KeywordSyntax("thiscall", " ");
                    break;
                case CallingConvention.FastCall:
                    yield return new KeywordSyntax("unmanaged", " ");
                    yield return new KeywordSyntax("fastcall", " ");
                    break;
                case CallingConvention.Vararg:
                    yield return new KeywordSyntax("vararg", " ");
                    break;
            }

            if (this._HasThis) yield return new KeywordSyntax("instance", " ");

            if (this._ExplicitThis) yield return new KeywordSyntax("explicit", " ");

            yield return this._ReturnType.ToSyntax(containingAssembly);

            if (pointer)
            {
                yield return new PunctuationSyntax(" ", "*", string.Empty);
                yield return new PunctuationSyntax(string.Empty, "(", string.Empty);
            }
            else yield return new PunctuationSyntax(" ", "(", string.Empty);

            for (int i = 0; i < this._ParamTypes.Length; i++)
            {
                if (i >= 1) yield return new PunctuationSyntax(string.Empty, ",", " ");

                yield return this._ParamTypes[i].ToSyntax(containingAssembly);
            }

            yield return new PunctuationSyntax(string.Empty, ")", string.Empty);
        }
    }
}
