/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CilTools.Reflection
{
    public class PInvokeParams
    {
        string _module;
        string _funcname;
        CharSet _charset;
        bool _exactspell;
        bool _setlasterr;
        bool? _bestfit;
        System.Runtime.InteropServices.CallingConvention _callconv;

        public PInvokeParams(
            string module,
            string func,
            CharSet charSet, 
            bool exactSpelling, 
            bool setLastError,
            bool? bestFitMapping,
            System.Runtime.InteropServices.CallingConvention callConv
            )
        {
            this._module = module;
            this._funcname = func;
            this._charset = charSet;
            this._exactspell = exactSpelling;
            this._setlasterr = setLastError;
            this._callconv = callConv;
            this._bestfit = bestFitMapping;
        }

        public string ModuleName { get { return this._module; } }

        public string FunctionName { get { return this._funcname; } }

        public CharSet CharSet { get { return this._charset; } }

        public bool ExactSpelling { get { return this._exactspell; } }

        public bool SetLastError { get { return this._setlasterr; } }

        public bool? BestFitMapping { get { return this._bestfit; } }

        public System.Runtime.InteropServices.CallingConvention CallingConvention 
        { 
            get { return this._callconv; } 
        }
    }
}
