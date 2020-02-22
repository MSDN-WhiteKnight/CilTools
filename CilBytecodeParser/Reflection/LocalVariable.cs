/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;

namespace CilBytecodeParser.Reflection
{
    public struct LocalVariable
    {
        TypeSpec type;
        int index;

        internal LocalVariable(TypeSpec ptype, int pindex)
        {
            this.type = ptype;
            this.index = pindex;
        }

        public TypeSpec LocalTypeSpec { get { return this.type; } }

        public Type LocalType { get { return this.type.Type; } }

        public int LocalIndex { get { return this.index; } }

        public bool IsPinned { get { return this.type.IsPinned; } }

        static LocalVariable[] ReadSignatureImpl(byte[] data, ITokenResolver resolver)
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
                    TypeSpec t = TypeSpec.ReadFromStream(ms, resolver);
                    ret[i] = new LocalVariable(t, i);
                }

                return ret;
            }
        }

        public static LocalVariable[] ReadSignature(byte[] data, ITokenResolver resolver)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) return new LocalVariable[0];

            return ReadSignatureImpl(data, resolver);
        }

        public static LocalVariable[] ReadSignature(byte[] data, Module module)
        {
            if (data == null) throw new ArgumentNullException("data", "Source array cannot be null");
            if (data.Length == 0) return new LocalVariable[0];

            ModuleWrapper mwr = new ModuleWrapper(module);
            return ReadSignatureImpl(data, mwr);
        }

        internal static LocalVariable[] FromReflection(MethodBase mb)
        {
            Debug.Assert(mb != null, "Source method cannot be null");

            MethodBody body = mb.GetMethodBody();

            if (body == null) return new LocalVariable[0];

            LocalVariable[] ret = new LocalVariable[body.LocalVariables.Count];

            for (int i = 0; i < body.LocalVariables.Count; i++)
            {
                ret[i] = new LocalVariable(
                    TypeSpec.FromType(body.LocalVariables[i].LocalType, body.LocalVariables[i].IsPinned),
                    i);
            }

            return ret;
        }
    }
}
