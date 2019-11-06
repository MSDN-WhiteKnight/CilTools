using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CilBytecodeParser
{
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

        public bool IsRequired { get { return this._IsRequired; } }

        public Type ModifierType { get { return this._Type; } }

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
