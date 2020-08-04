/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    class MetadataCustomAttribute : ICustomAttribute
    {
        //ICustomAttribute is recognized by CilTools.BytecodeAnalysis when printing attributes
        MethodBase _owner;
        MethodBase _constr;
        byte[] _data;

        public MetadataCustomAttribute(MethodBase owner, MethodBase constr, byte[] data)
        {
            if (data == null) data = new byte[0];

            this._owner = owner;
            this._constr = constr;
            this._data = data;
        }

        public MethodBase Owner
        {
            get
            {
                return this._owner;
            }
        }

        public MethodBase Constructor
        {
            get
            {
                return this._constr;
            }
        }

        public byte[] Data
        {
            get
            {
                return this._data;
            }
        }
    }
}
