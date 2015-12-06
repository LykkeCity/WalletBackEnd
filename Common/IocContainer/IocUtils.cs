using System;
using System.Linq;
using System.Reflection;


namespace Common.IocContainer
{
    public static class IocUtils
    {

        internal static ConstructorInfo FindIocConstructor(this Type src)
        {
            var constructors = src.GetConstructors();


            if (constructors.Length != 1)
                throw new Exception("Class '" + src.Name + "' has " + constructors.Length + " constructors. It should have the single one.");

            return constructors[0];
        }

        internal static Type GetIocArrayElementType(this ParameterInfo src)
        {
            return src.ParameterType.GetElementType();
        }

        internal static Type GetIocRealType(this ParameterInfo src)
        {
            return IocParameterIsArray(src) ? GetIocArrayElementType(src) : src.ParameterType;
        }

        internal static bool IocParameterIsArray(this ParameterInfo src)
        {
            return src.ParameterType.BaseType == typeof(Array);
        }


        internal static object[] CreateConstructorParamtersInstances(this IoC ioc, ParameterInfo[] src)
        {
            return src
                .Select(pi => pi.IocParameterIsArray()
                    ? ioc.GetParameterAsArray(pi.GetIocRealType())
                    : ioc.GetObject(pi.ParameterType)).ToArray();
        }


        internal static object GetParameterAsArray(this IoC ioc, Type type)
        {

            var instances = ioc.GetObjects(type);
             
            var result = Array.CreateInstance(type, instances.Length);

            Array.Copy(instances, result, instances.Length);

            return result;
        }

    }
}
