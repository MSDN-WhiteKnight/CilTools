/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CilBytecodeParser
{
    public enum CallingConvention //ECMA-335 II.15.3: Calling convention 
    {
        Default  = 0x00,
        CDecl    = 0x01,
        StdCall  = 0x02,
        ThisCall = 0x03,
        FastCall = 0x04,
        Vararg   = 0x05,
    }

    public class Signature //ECMA-335 II.23.2.3: StandAloneMethodSig
    {
        const int CALLCONV_MASK = 0x0F;
        const int MFLAG_HASTHIS = 0x20; //instance
        const int MFLAG_EXPLICITTHIS = 0x40; //explicit

        CallingConvention _conv;
        bool _HasThis;
        bool _ExplicitThis;
        TypeSpec _ReturnType;
        TypeSpec[] _ParamTypes;

        public Signature(byte[] data, Module module)
        {            
            MemoryStream ms = new MemoryStream(data);
            using (ms)
            {
                byte b = MetadataReader.ReadByte(ms); //calling convention & method flags
                int conv = b & CALLCONV_MASK;
                this._conv = (CallingConvention)conv;

                if ((b & MFLAG_HASTHIS) == MFLAG_HASTHIS) this._HasThis = true;

                if ((b & MFLAG_EXPLICITTHIS) == MFLAG_EXPLICITTHIS) this._ExplicitThis = true;

                uint paramcount = MetadataReader.ReadCompressed(ms);
                this._ParamTypes = new TypeSpec[paramcount];
                this._ReturnType = TypeSpec.ReadFromStream(ms, module);                

                for (int i = 0; i < paramcount; i++)
                {
                    this._ParamTypes[i] = TypeSpec.ReadFromStream(ms, module);                    
                } 
            }
        }

        public CallingConvention CallingConvention { get { return this._conv; } }

        public bool HasThis { get { return this._HasThis; } }

        public bool ExplicitThis { get { return this._ExplicitThis; } }

        public TypeSpec ReturnType { get { return this._ReturnType; } }

        public int ParamsCount { get { return this._ParamTypes.Length; } }

        public TypeSpec[] GetParamTypes()
        {
            TypeSpec[] res = new TypeSpec[this._ParamTypes.Length];
            this._ParamTypes.CopyTo(res, 0);
            return res;
        }

        public override string ToString()
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
            sb_sig.Append(" (");

            for (int i = 0; i < this._ParamTypes.Length; i++)
            {
                if (i >= 1) sb_sig.Append(", ");
                sb_sig.Append(this._ParamTypes[i].ToString());
            }

            sb_sig.Append(')');
            return sb_sig.ToString();
        }        
    }
}
