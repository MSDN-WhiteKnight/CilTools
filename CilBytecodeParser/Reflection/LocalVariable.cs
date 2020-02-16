/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilBytecodeParser.Reflection
{
    public class LocalVariable
    {
        TypeSpec type;
        int index;

        protected LocalVariable(TypeSpec ptype, int pindex)
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
    }
}
