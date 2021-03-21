/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Runtime
{
    internal struct MethodId
    {
        ulong moduleAddr;
        int token;

        public MethodId(ulong module, int tok)
        {
            this.moduleAddr = module;
            this.token = tok;
        }

        public ulong ModuleAddr
        {
            get { return this.moduleAddr; }
        }

        public int MetadataToken
        {
            get { return this.token; }
        }

        public override int GetHashCode()
        {
            int ret = 0;

            unchecked
            {
                ret += (int)(moduleAddr);
                ret += token * 76543;
            }

            return ret;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is MethodId)) return false;

            MethodId mid = (MethodId)obj;

            return this.ModuleAddr == mid.ModuleAddr && 
                this.MetadataToken == mid.MetadataToken;
        }
    }
}
