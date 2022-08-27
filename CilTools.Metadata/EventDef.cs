/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using CilTools.Internal;

namespace CilTools.Metadata
{
    class EventDef : EventInfo
    {
        EventDefinition e;
        EventDefinitionHandle he;
        MetadataAssembly owner;
        TypeDef declaringType;

        public EventDef(EventDefinition eventDefinition, EventDefinitionHandle hEventDefinition, MetadataAssembly o, TypeDef dt)
        {
            this.e = eventDefinition;
            this.he = hEventDefinition;
            this.owner = o;
            this.declaringType = dt;
        }

        void ThrowIfDisposed()
        {
            if (this.owner.MetadataReader == null)
            {
                throw new ObjectDisposedException("MetadataReader");
            }
        }

        public override EventAttributes Attributes => this.e.Attributes;

        public override Type DeclaringType => this.declaringType;

        public override string Name
        {
            get
            {
                this.ThrowIfDisposed();
                return this.owner.MetadataReader.GetString(this.e.Name);
            }
        }

        public override Type ReflectedType => throw new NotImplementedException();

        MethodInfo GetAccessor(MethodDefinitionHandle mdh, bool nonPublic)
        {
            if (mdh.IsNil) return null;

            MethodBase ret = this.owner.GetMethodDefinition(mdh);

            if (!nonPublic && !ret.IsPublic) return null;
            else return (MethodInfo)ret;
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            this.ThrowIfDisposed();
            MethodDefinitionHandle mdh = this.e.GetAccessors().Adder;
            return this.GetAccessor(mdh, nonPublic);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.e.GetCustomAttributes();

            return Utils.ReadCustomAttributes(coll, this, this.owner);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            this.ThrowIfDisposed();
            MethodDefinitionHandle mdh = this.e.GetAccessors().Raiser;
            return this.GetAccessor(mdh, nonPublic);
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            this.ThrowIfDisposed();
            MethodDefinitionHandle mdh = this.e.GetAccessors().Remover;
            return this.GetAccessor(mdh, nonPublic);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
