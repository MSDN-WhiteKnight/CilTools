/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.Internal;

namespace CilTools.Metadata
{
    class ModuleDef : Module
    {
        MetadataAssembly _owner;
        ModuleDefinition _m;
        ModuleDefinitionHandle _handle;
        string _name;

        internal ModuleDef(MetadataAssembly owner, ModuleDefinition m, ModuleDefinitionHandle h)
        {
            this._owner = owner;
            this._m = m;
            this._handle = h;
            this._name = owner.MetadataReader.GetString(m.Name);
        }

        public override string Name => this._name;

        public override string ScopeName => this._name;

        public override string FullyQualifiedName => this._owner.Location;

        public override Assembly Assembly => this._owner;

        public override int MetadataToken => this._owner.MetadataReader.GetToken(this._handle);

        public override Guid ModuleVersionId => this._owner.MetadataReader.GetGuid(this._m.Mvid);

        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this._m.GetCustomAttributes();
            return Utils.ReadCustomAttributes(coll, this, this._owner);
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return this._owner.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override string ResolveString(int metadataToken)
        {
            return this._owner.ResolveString(metadataToken);
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            return this._owner.ResolveSignature(metadataToken);
        }
    }
}
