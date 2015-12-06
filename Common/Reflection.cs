using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.TypeMappers;

namespace Common
{
    public class PropertyAttributePair
    {

        internal void Init(PropertyInfo propertyInfo, object attributeInstance)
        {
            PropertyInfo = propertyInfo;
            AttributeInstance = attributeInstance;
        }

        public PropertyInfo PropertyInfo { get; private set; }
        public object AttributeInstance { get; private set; }
    }

    public class PropertyAttributePair<TAttr> : PropertyAttributePair where TAttr : Attribute
    {
        internal void Init(PropertyInfo propertyInfo, TAttr attributeInstance)
        {
            base.Init(propertyInfo, attributeInstance);
        }

        public new TAttr AttributeInstance { get { return (TAttr)base.AttributeInstance; } }
    }

    public class MethodAttribytePair<TAttr> : PropertyAttributePair where TAttr : Attribute
    {
        public MethodAttribytePair(TAttr attribute, MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            Attribute = attribute;
        }

        public TAttr Attribute { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
    }

    public static class ReflectionUtils
    {
        private static readonly Dictionary<Type, Type> SimpleTypes = new Dictionary<Type, Type>{
                                                                                             {typeof(byte), typeof(byte)},
                                                                                             {typeof(sbyte), typeof(sbyte)},
                                                                                             {typeof(short), typeof(short)},
                                                                                             {typeof(ushort), typeof(ushort)},
                                                                                             {typeof(int), typeof(int)},
                                                                                             {typeof(uint), typeof(uint)},
                                                                                             {typeof(long), typeof(long)},
                                                                                             {typeof(ulong), typeof(ulong)},
                                                                                             {typeof(string), typeof(string)},
                                                                                             {typeof(char), typeof(char)},
                                                                                             {typeof(double), typeof(double)},
                                                                                             {typeof(float), typeof(float)},
                                                                                             {typeof(bool), typeof(bool)},
                                                                                             {typeof(DateTime), typeof(DateTime)},
        };

        private static readonly Dictionary<Type, Type> NullableTypes = new Dictionary<Type, Type>{
                                                                                             {typeof(byte?), typeof(byte)},
                                                                                             {typeof(sbyte?), typeof(sbyte)},
                                                                                             {typeof(short?), typeof(short)},
                                                                                             {typeof(ushort?), typeof(ushort)},
                                                                                             {typeof(int?), typeof(int)},
                                                                                             {typeof(uint?), typeof(uint)},
                                                                                             {typeof(long?), typeof(long)},
                                                                                             {typeof(ulong?), typeof(ulong)},
                                                                                             {typeof(char?), typeof(char)},
                                                                                             {typeof(double?), typeof(double)},
                                                                                             {typeof(float?), typeof(float)},
                                                                                             {typeof(bool?), typeof(bool)},
                                                                                             {typeof(DateTime?), typeof(DateTime)},
        };

        public static IEnumerable<string> GetEnumNames<T>()
        {
            return Enum.GetNames(typeof (T));
        } 

        public static bool IsSimpleType(Type type)
        {
            return SimpleTypes.ContainsKey(type);

        }

        public static bool IsNullableType(Type type)
        {
            return NullableTypes.ContainsKey(type);

        }

        public static Type GetNullableGenericType(Type type)
        {
            return NullableTypes[type];
        }


        public static Type GetElementType(Type type)
        {
            var result = type.GetElementType();

            if (result != null)
                return result;

            Type[] generics = null;
            if (DoesThePropertyImplementTheInterface(type, typeof(IListReadOnly)))
            {
                foreach (var interf in type.GetInterfaces())
                    if (interf.IsGenericType)
                    {
                        generics = interf.GetGenericArguments();
                        break;
                    }
            }
            else
                generics = type.GetGenericArguments();

            if (generics == null)
                throw new Exception("Invalid Generic type for '" + type + "'");

            if (generics.Length != 1)
                throw new Exception("Invalid Generic type for '" + type + "'");

            return generics[0];
        }

        public static TAttr GetAttribute<TAttr>(Type type) where TAttr : Attribute
        {
            return type.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
        }

        public static TAttr GetAttribute<TAttr>(PropertyInfo propertyInfo) where TAttr : Attribute
        {
            return propertyInfo.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
        }


        public static object GetAttribute(PropertyInfo propertyInfo, Type attr)
        {
            return propertyInfo.GetCustomAttributes(false).FirstOrDefault(customAttribute => customAttribute.GetType() == attr);
        }

        public static TAttr GetAttribute<TAttr>(object instance) where TAttr : Attribute
        {
            var attr = instance.GetType().GetCustomAttributes(true).OfType<TAttr>().FirstOrDefault();

            if (attr != null)
                return attr;

            foreach (var @interface in instance.GetType().GetInterfaces())
            {
                attr = @interface.GetCustomAttributes(true).OfType<TAttr>().FirstOrDefault();

                if (attr != null)
                    return attr;

            }

            return null;

        }


        public static PropertyInfo FindProperty(Type type, string propertyName)
        {
            return type.GetProperties().FirstOrDefault(propertyInfo => propertyInfo.Name == propertyName);
        }

        public static ConstructorInfo FindDefaultConstructor(this Type type)
        {
            return (from constructor in type.GetConstructors() let contructorParams = constructor.GetParameters() where contructorParams.Length == 0 select constructor).FirstOrDefault();
        }


        public static object CreateDefault(this Type type)
        {
            var ctr = FindDefaultConstructor(type);

            return ctr.Invoke(null);
        }


        public static ConstructorInfo FindConstructor(Type type, Type param0)
        {
            return (from constructorInfo in type.GetConstructors() let pars = constructorInfo.GetParameters() where pars.Length == 1 where pars[0].ParameterType == typeof(string) select constructorInfo).FirstOrDefault();
        }

        public static IEnumerable<PropertyAttributePair> FindProperiesWithAttribute(Type type, Type attr)
        {
            var result = new PropertyAttributePair();
            foreach (var propertyInfo in type.GetProperties())
            {
                var attributeInstance = GetAttribute(propertyInfo, attr);

                if (attributeInstance == null) continue;

                result.Init(propertyInfo, attributeInstance);
                yield return result;
            }

        }


        public static IEnumerable<PropertyAttributePair<TAttr>> FindProperiesWithAttribute<TAttr>(Type type) where TAttr : Attribute
        {
            var result = new PropertyAttributePair<TAttr>();

            foreach (var propertyInfo in type.GetProperties())
            {
                var attributeInstance = GetAttribute<TAttr>(propertyInfo);

                if (attributeInstance == null) continue;

                result.Init(propertyInfo, attributeInstance);
                yield return result;
            }
        }

        public static PropertyInfo FindPropertyByName(Type type, string name, out string[] propertyPath)
        {
            propertyPath = name.Split('.');

            if (propertyPath.Length == 1)
                return type.GetProperties().FirstOrDefault(propertyInfo => propertyInfo.Name == name);

            PropertyInfo pi = null;
            foreach (var prop in propertyPath)
            {
                pi = pi == null ? type.GetProperties().FirstOrDefault(propertyInfo => propertyInfo.Name == prop) :
                                  pi.PropertyType.GetProperties().FirstOrDefault(propertyInfo => propertyInfo.Name == prop);

                if (pi == null)
                    return null;
            }

            return pi;

        }

        public static PropertyInfo FindPropertyByName(Type type, string name)
        {
            string[] propertyPath;
            return FindPropertyByName(type, name, out propertyPath);

        }

        public static bool DoesThePropertyImplementTheInterface(Type propertyType, Type interfaceType)
        {
            return propertyType.GetInterfaces().Any(@interface => @interface == interfaceType);
        }


        public static TAttr GetAttribute<TAttr>(MethodInfo methodInfo) where TAttr : Attribute
        {
            return methodInfo.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
        }


        public static object GetAttribute(MethodInfo methodInfo, Type attr)
        {
            return methodInfo.GetCustomAttributes(false).FirstOrDefault(customAttribute => customAttribute.GetType() == attr);
        }


        public static IEnumerable<MethodAttribytePair<TAttr>> GetMethods<TAttr>(Type type) where TAttr : Attribute
        {
            return from methodInfo in type.GetMethods() let attr = GetAttribute<TAttr>(methodInfo) where attr != null select new MethodAttribytePair<TAttr>(attr, methodInfo);
        }

        public static IEnumerable<MethodAttribytePair<TAttr>> GetMethods<TAttr>(object instance) where TAttr : Attribute
        {
            foreach (var simpleType in GetMethods<TAttr>(instance.GetType()))
            {
                yield return simpleType;
            }


            foreach (var simpleType in instance.GetType().GetInterfaces().SelectMany(GetMethods<TAttr>))
            {
                yield return simpleType;
            }
        }


        public static void PopulateObject(this object data, Func<PropertyInfo, object> getValue)
        {
            foreach (var pi in data.GetType().GetProperties())
            {
                var value = getValue(pi);
                pi.SetValue(data, value);
            }
        }
    }
}
