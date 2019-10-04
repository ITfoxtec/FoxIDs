using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace FoxIDs.Infrastructure.ApiDescription
{
    public class ApiDescriptionMethodInfo : MethodInfo
    {
        private readonly MethodInfo methodInfo;
        private readonly string name;

        public ApiDescriptionMethodInfo(MethodInfo methodInfo, string name)
        {
            this.methodInfo = methodInfo;
            this.name = name;
        }

        public override Type ReturnType => methodInfo.ReturnType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => methodInfo.ReturnTypeCustomAttributes;

        public override MethodAttributes Attributes => methodInfo.Attributes;

        public override RuntimeMethodHandle MethodHandle => methodInfo.MethodHandle;

        public override Type DeclaringType => methodInfo.DeclaringType;

        public override string Name { get { return name; } }

        public override Type ReflectedType => methodInfo.ReflectedType;

        public override MethodInfo GetBaseDefinition()
        {
            return methodInfo.GetBaseDefinition();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return methodInfo.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return methodInfo.GetCustomAttributes(attributeType, inherit);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return methodInfo.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return methodInfo.GetParameters();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            return methodInfo.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return methodInfo.IsDefined(attributeType, inherit);
        }

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return methodInfo.MakeGenericMethod(typeArguments);
        }

        public override Type[] GetGenericArguments()
        {
            return methodInfo.GetGenericArguments();
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return methodInfo.GetGenericMethodDefinition();
        }

        public override bool IsGenericMethod
        {
            get { return methodInfo.IsGenericMethod; }
        }

        public override bool IsGenericMethodDefinition
        {
            get { return methodInfo.IsGenericMethodDefinition; }
        }

        public override bool ContainsGenericParameters
        {
            get { return methodInfo.ContainsGenericParameters; }
        }
    }
}
