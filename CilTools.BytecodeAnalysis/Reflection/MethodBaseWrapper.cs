/* CilTools.BytecodeAnalysis library 
* Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
* License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    internal class MethodBaseWrapper : MethodBase, ICustomMethod
    {
        MethodBase srcmethod;
        ITokenResolver resolver;
        int _stacksize;
        bool _hasstacksize;
        byte[] _code;
        byte[] _localssig;
        ExceptionBlock[] _blocks;
        bool _localsinit;
        bool _haslocalsinit;

        public MethodBaseWrapper(MethodBase mb) : base()
        {
            this.srcmethod = mb;
            this.resolver = CustomMethod.CreateResolver(mb);

            if (Types.IsDynamicMethod(srcmethod))
            {
                FieldInfo fieldGenerator = srcmethod.GetType().GetField("m_ilGenerator",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object valueGenerator = fieldGenerator.GetValue(srcmethod);
                FieldInfo field;

                if (valueGenerator != null)
                {
                    field = Types.ILGeneratorType.GetField("m_ILStream",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    byte[] ilbytes = (byte[])field.GetValue(valueGenerator);

                    field = Types.ILGeneratorType.GetField("m_length",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    int len = (int)field.GetValue(valueGenerator);

                    field = valueGenerator.GetType().GetField("m_methodBuilder",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    MethodBase val = (MethodBase)field.GetValue(valueGenerator);

                    byte[] il = new byte[len];
                    Array.Copy(ilbytes, il, len);
                    this._code = il;
                }
                else
                {
                    this._code = new byte[0];
                }

                FieldInfo fieldResolver = srcmethod.GetType().GetField("m_resolver",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object valueResolver = fieldResolver.GetValue(srcmethod);

                if (valueResolver != null)
                {
                    field = valueResolver.GetType().GetField("m_localSignature",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    byte[] sigbytes = (byte[])field.GetValue(valueResolver);
                    this._localssig = sigbytes;

                    field = valueResolver.GetType().GetField("m_stackSize",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    this._stacksize = (int)field.GetValue(valueResolver);
                    this._hasstacksize = true;

                    field = valueResolver.GetType().GetField("m_exceptions",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );
                    object[] arrExceptions = (object[])field.GetValue(valueResolver);

                    if (arrExceptions == null) arrExceptions = new object[0];

                    List<ExceptionBlock> blocks = new List<ExceptionBlock>(arrExceptions.Length * 2);

                    Type t = arrExceptions.GetType().GetElementType();

                    FieldInfo fieldStartAddr = t.GetField("m_startAddr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldEndAddr = t.GetField("m_endAddr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldCatchAddr = t.GetField("m_catchAddr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldCatchClass = t.GetField("m_catchClass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldCatchEndAddr = t.GetField("m_catchEndAddr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldFilterAddr = t.GetField("m_filterAddr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo fieldType = t.GetField("m_type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    for (int i = 0; i < arrExceptions.Length; i++)
                    {
                        int startaddr = (int)fieldStartAddr.GetValue(arrExceptions[i]);
                        int endAddr = (int)fieldEndAddr.GetValue(arrExceptions[i]);
                        int[] catchAddr = (int[])fieldCatchAddr.GetValue(arrExceptions[i]);
                        int[] catchEndAddr = (int[])fieldCatchEndAddr.GetValue(arrExceptions[i]);
                        Type[] catchClass = (Type[])fieldCatchClass.GetValue(arrExceptions[i]);
                        //int endFinally = (int)fieldEndFinally.GetValue(arrExceptions[i]);
                        int[] filterAddr = (int[])fieldFilterAddr.GetValue(arrExceptions[i]);
                        int[] type = (int[])fieldType.GetValue(arrExceptions[i]);

                        for (int j = 0; j < type.Length; j++)
                        {
                            int length = endAddr - startaddr;
                            int handler_length = catchEndAddr[j] - catchAddr[j];
                            int filter_offset = filterAddr[j];

                            if (handler_length <= 0) continue;

                            if (filter_offset < 0 || filter_offset > this._code.Length) filter_offset = 0;

                            blocks.Add(new ExceptionBlock(
                                        (ExceptionHandlingClauseOptions)type[j], startaddr, length,
                                        catchClass[j], catchAddr[j],
                                        handler_length, filter_offset
                                        ));
                        }
                    }

                    this._blocks = blocks.ToArray();
                }
                else
                {
                    this._localssig = new byte[0];
                    this._hasstacksize = false;
                    this._haslocalsinit = false;
                }
            }
            else //regular method
            {
                MethodBody body = srcmethod.GetMethodBody();

                if (body != null)
                {
                    this._code = body.GetILAsByteArray();
                    int token = body.LocalSignatureMetadataToken;

                    if (token == 0) this._localssig = new byte[0];
                    else this._localssig = this.resolver.ResolveSignature(token);

                    this._stacksize = body.MaxStackSize;
                    this._hasstacksize = true;

                    this._blocks = new ExceptionBlock[body.ExceptionHandlingClauses.Count];

                    for (int i = 0; i < body.ExceptionHandlingClauses.Count; i++)
                    {
                        this._blocks[i] = ExceptionBlock.FromReflection(body.ExceptionHandlingClauses[i]);
                    }

                    this._localsinit = body.InitLocals;
                    this._haslocalsinit = true;
                }
                else
                {
                    this._code = new byte[0];
                    this._localssig = new byte[0];
                    this._blocks = new ExceptionBlock[0];
                    this._hasstacksize = false;
                    this._haslocalsinit = false;
                }
            }
        }

        public override MethodAttributes Attributes { get { return srcmethod.Attributes; } }

        public override RuntimeMethodHandle MethodHandle { get { return srcmethod.MethodHandle; } }

        public override Type DeclaringType { get { return srcmethod.DeclaringType; } }

        public override MemberTypes MemberType { get { return srcmethod.MemberType; } }

        public override string Name { get { return srcmethod.Name; } }

        public override Type ReflectedType { get { return srcmethod.ReflectedType; } }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return srcmethod.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return srcmethod.GetCustomAttributes(attributeType, inherit);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return srcmethod.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return srcmethod.GetParameters();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            return srcmethod.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return srcmethod.IsDefined(attributeType, inherit);
        }

        public override bool IsGenericMethod
        {
            get
            {
                return srcmethod.IsGenericMethod;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return srcmethod.GetGenericArguments();
        }

        public virtual Type ReturnType
        {
            get
            {
                MethodInfo mi = srcmethod as MethodInfo;

                if (mi != null) return mi.ReturnType;
                else return null;
            }
        }

        public virtual ITokenResolver TokenResolver
        {
            get
            {
                return this.resolver;
            }
        }

        public virtual byte[] GetBytecode()
        {
            return this._code;
        }

        public virtual byte[] GetLocalVarSignature()
        {
            return this._localssig;
        }

        LocalVariable[] GetLocalVariables_Default()
        {
            byte[] sig = this.GetLocalVarSignature();

            return LocalVariable.ReadSignature(sig, this.TokenResolver, this);
        }

        public virtual LocalVariable[] GetLocalVariables()
        {
            LocalVariable[] ret = null;

            try
            {
                ret = this.GetLocalVariables_Default();
            }
            catch (NotSupportedException) { }
            catch (ArgumentOutOfRangeException) { }

            if (ret != null) return ret;
            else return LocalVariable.FromReflection(this.srcmethod);
        }

        public virtual int MaxStackSize
        {
            get { return this._stacksize; }
        }

        public virtual bool MaxStackSizeSpecified
        {
            get { return this._hasstacksize; }
        }

        public virtual ExceptionBlock[] GetExceptionBlocks()
        {
            return this._blocks;
        }

        public MethodBase GetDefinition()
        {
            return null;
        }

        public PInvokeParams GetPInvokeParams()
        {
            return null;
        }

        public virtual bool InitLocals
        {
            get { return this._localsinit; }
        }

        public virtual bool InitLocalsSpecified
        {
            get { return this._haslocalsinit; }
        }
    }
}
