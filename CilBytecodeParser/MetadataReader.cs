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
    internal static class MetadataReader //Internal helper methods for parsing ECMA-335 binary metadata
    {
        public static byte ReadByte(Stream source)
        {
            int res = source.ReadByte();
            if (res < 0) throw new EndOfStreamException();
            return (byte)res;
        }

        public static uint ReadCompressed(Stream source) //ECMA-335 II.23.2 Blobs and signatures
        {
            byte[] paramcount_bytes = new byte[4];
            byte b1, b2, b3, b4;
            b1 = ReadByte(source);

            if ((b1 & 0x80) == 0x80)
            {
                b2 = ReadByte(source);

                if ((b2 & 0x40) == 0x40) //4 bytes
                {
                    paramcount_bytes[0] = b1;
                    paramcount_bytes[1] = b2;

                    b3 = ReadByte(source);
                    b4 = ReadByte(source);

                    paramcount_bytes[2] = b3;
                    paramcount_bytes[3] = b4;
                }
                else //2 bytes
                {
                    paramcount_bytes[0] = b1;
                    paramcount_bytes[1] = b2;
                }
            }
            else //1 byte
            {
                paramcount_bytes[0] = b1;
            }

            return BitConverter.ToUInt32(paramcount_bytes, 0);
        }
    }
}
