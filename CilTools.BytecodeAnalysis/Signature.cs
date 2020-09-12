/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.Reflection;
using CilTools.Syntax;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents calling convention, a set of rules defining how the function interacts with its caller, as defined by ECMA-335
    /// </summary>
    public enum CallingConvention //ECMA-335 II.15.3: Calling convention 
    {
        /// <summary>
        /// Default managed calling convention (fixed amount of arguments)
        /// </summary>
        Default  = 0x00,

        /// <summary>
        /// C language calling convention
        /// </summary>
        CDecl    = 0x01,

        /// <summary>
        /// Standard x86 calling convention
        /// </summary>
        StdCall  = 0x02,

        /// <summary>
        /// Calling convention for C++ class member functions
        /// </summary>
        ThisCall = 0x03,

        /// <summary>
        /// Optimized x86 calling convention
        /// </summary>
        FastCall = 0x04,

        /// <summary>
        /// Managed calling convention with variable amount of arguments
        /// </summary>
        Vararg   = 0x05,
    }

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

        /// <summary>
        /// Initializes a new Signature object representing a stand-alone method signature
        /// </summary>
        /// <param name="data">The byte array containing StandAloneMethodSig data (ECMA-335 II.23.2.3)</param>
        /// <param name="module">Module containing the passed signature</param>
        /// <exception cref="System.ArgumentNullException">Source array is null</exception>
        /// <exception cref="System.ArgumentException">Source array is empty</exception>
        /// <exception cref="System.IO.EndOfStreamException">Unexpected end of input data</exception>
        /// <exception cref="CilParserException">Input data is invalid</exception>
        /// <exception cref="System.NotSupportedException">Signature contains generic types, function pointers or variable-bound arrays</exception>
        public Signature(byte[] data, Module module) //ECMA-335 II.23.2.3: StandAloneMethodSig
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");
                        
            ModuleWrapper mwr = new ModuleWrapper(module);
            Initialize(data, mwr, null);
        }

        public Signature(byte[] data, ITokenResolver resolver)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            Initialize(data, resolver,null);
        }

        public Signature(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");

            Initialize(data, resolver, member);
        }

        public Signature(Stream src, ITokenResolver resolver, MemberInfo member)
        {
            if (src == null) throw new ArgumentNullException("src", "Source stream cannot be null");            

            this.Initialize(src, resolver, member);
        }

        void Initialize(Stream src, ITokenResolver resolver, MemberInfo member)
        {
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
                    this._ParamTypes[i] = TypeSpec.ReadFromStream(src, resolver, member);
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
                this._ReturnType = TypeSpec.ReadFromStream(src, resolver, member);

                for (int i = 0; i < paramcount; i++)
                {
                    this._ParamTypes[i] = TypeSpec.ReadFromStream(src, resolver, member);
                }
            }
        }

        void Initialize(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            MemoryStream ms = new MemoryStream(data);

            using (ms)
            {
                this.Initialize(ms, resolver, member);
            }
        }

        public static TypeSpec ReadFieldSignature(byte[] data, ITokenResolver resolver, MemberInfo member)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) throw new ArgumentException("Source array cannot be empty", "data");
            MemoryStream ms = new MemoryStream(data);

            using (ms)
            {
                byte b = MetadataReader.ReadByte(ms); //signature kind

                if (b != 0x6) throw new InvalidDataException("Invalid field signature");

                return TypeSpec.ReadFromStream(ms, resolver,member);
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

        public bool GenericInst { get { return this._GenInst; } }

        /// <summary>
        /// Gets the return type of the function described by this signature
        /// </summary>
        public TypeSpec ReturnType { get { return this._ReturnType; } }

        /// <summary>
        /// Gets the amount of fixed parameters that the function described by this signature takes
        /// </summary>
        public int ParamsCount { get { return this._ParamTypes.Length; } }

        public int GenericArgsCount { get { return this._GenParamCount; } }

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
            return this.ToString(false);
        }

        internal string ToString(bool pointer)
        {
            StringBuilder sb_sig = new StringBuilder(100);

            switch (this._conv)
            {
                case CallingConvention.CDecl: sb_sig.Append("unmanaged cdecl "); break;
                case CallingConvention.StdCall: sb_sig.Append("unmanaged stdcall "); break;
                case CallingConvention.ThisCall: sb_sig.Append("unmanaged thiscall "); break;
                case CallingConvention.FastCall: sb_sig.Append("unmanaged fastcall "); break;
                case CallingConvention.Vararg: sb_sig.Append("vararg "); break;
            }

            if (this._HasThis) sb_sig.Append("instance ");

            if (this._ExplicitThis) sb_sig.Append("explicit ");

            sb_sig.Append(this._ReturnType.ToString());
            sb_sig.Append(' ');
            if(pointer) sb_sig.Append('*');
            sb_sig.Append('(');

            for (int i = 0; i < this._ParamTypes.Length; i++)
            {
                if (i >= 1) sb_sig.Append(", ");
                sb_sig.Append(this._ParamTypes[i].ToString());
            }

            sb_sig.Append(')');
            return sb_sig.ToString();
        }

        internal IEnumerable<SyntaxNode> ToSyntax(bool pointer)
        {
            switch (this._conv)
            {
                case CallingConvention.CDecl:
                    yield return new KeywordSyntax(String.Empty, "unmanaged"," ", KeywordKind.Other);
                    yield return new KeywordSyntax(String.Empty, "cdecl", " ", KeywordKind.Other);
                    break;
                case CallingConvention.StdCall:
                    yield return new KeywordSyntax(String.Empty, "unmanaged", " ", KeywordKind.Other);
                    yield return new KeywordSyntax(String.Empty, "stdcall", " ", KeywordKind.Other);
                    break;
                case CallingConvention.ThisCall:
                    yield return new KeywordSyntax(String.Empty, "unmanaged", " ", KeywordKind.Other);
                    yield return new KeywordSyntax(String.Empty, "thiscall", " ", KeywordKind.Other);
                    break;
                case CallingConvention.FastCall:
                    yield return new KeywordSyntax(String.Empty, "unmanaged", " ", KeywordKind.Other);
                    yield return new KeywordSyntax(String.Empty, "fastcall", " ", KeywordKind.Other);
                    break;
                case CallingConvention.Vararg:
                    yield return new KeywordSyntax(String.Empty, "vararg", " ", KeywordKind.Other);
                    break;
            }

            if (this._HasThis) yield return new KeywordSyntax(String.Empty, "instance", " ", KeywordKind.Other);

            if (this._ExplicitThis) yield return new KeywordSyntax(String.Empty, "explicit", " ", KeywordKind.Other);

            yield return this._ReturnType.ToSyntax();

            if (pointer)
            {
                yield return new PunctuationSyntax(" ", "*", String.Empty);
                yield return new PunctuationSyntax(String.Empty, "(", String.Empty);
            }
            else yield return new PunctuationSyntax(" ", "(", String.Empty);
            
            for (int i = 0; i < this._ParamTypes.Length; i++)
            {
                if (i >= 1) yield return new PunctuationSyntax(String.Empty, ",", " ");

                yield return this._ParamTypes[i].ToSyntax();
            }

            yield return new PunctuationSyntax(String.Empty, ")", String.Empty);
        }
    }
}
