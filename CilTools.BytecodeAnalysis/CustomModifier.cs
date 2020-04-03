﻿/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents custom modifier, an object used to associate a special information with the type specification, as defined by ECMA-335
    /// </summary>
    public struct CustomModifier //ECMA-335 II.23.2.7 CustomMod
    {
        bool _IsRequired;
        int _token;
        Type _Type;

        internal CustomModifier(bool required, int tok,Type t)
        {            
            this._IsRequired = required;
            this._token = tok;
            this._Type = t;
        }

        /// <summary>
        /// Gets the value indicating whether this modifier is required or optional
        /// </summary>
        /// <value>
        /// 'true' if type users are required to understand this modifier in order to correctly use it, 'false' if the modifier can be ignored
        /// </value>
        /// <remarks>
        /// See ECMA-335 I.9.7 (Metadata extensibility) for more information about required and optional modifiers
        /// </remarks>
        public bool IsRequired { get { return this._IsRequired; } }

        /// <summary>
        /// Gets the type of this modifier
        /// </summary>
        public Type ModifierType { get { return this._Type; } }

        /// <summary>
        /// Gets the textual representation of this modifier as CIL code
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string mod;
            string type;

            if (this._IsRequired) mod = "modreq(";
            else mod = "modopt(";

            if (this._Type != null) type = CilAnalysis.GetTypeNameInternal(this._Type);
            else type = "Type" + _token.ToString("X");

            return mod + type + ")";
        }
    }
}
