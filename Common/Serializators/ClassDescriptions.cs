using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Common.Serializators
{

    public class TypeFabric
    {
        protected static IFormatProvider XmlCulture = CultureInfo.InvariantCulture;

        private readonly PropertyInfo _propertyInfo;

        public TypeFabric(Type type)
        {
            Type = type;
            Constructor = ReflectionUtils.FindDefaultConstructor(Type);
        }

        public TypeFabric(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
            Type = propertyInfo.PropertyType;
            Constructor = ReflectionUtils.FindDefaultConstructor(Type);
        }

        public bool TypeIsNullable { get; private set; }

        public Type NullableGenericType { get; private set; }

        private Type _type;

        public Type Type
        {
            get { return _type; }
            private set
            {
                _type = value;

                TypeIsNullable = ReflectionUtils.IsNullableType(_type);

                NullableGenericType = TypeIsNullable ? ReflectionUtils.GetNullableGenericType(_type) : null;

            }
        }

        public ConstructorInfo Constructor { get; private set; }

        public bool IsSimple { get { return Constructor == null; } }


        public object StringToObject(string data)
        {
            return Convert.ChangeType(data, NullableGenericType ?? Type, XmlCulture);
        }


        public string GetValueAsString(object instance)
        {
            if (_propertyInfo == null)
                return instance == null ? null : Convert.ToString(instance, XmlCulture);

            var value = GetValue<object>(instance);

            if (value is IList<byte>)
                value = (value as IList<byte>).ToHexString();

            return value == null ? null : Convert.ToString(value, XmlCulture);
        }

        public T GetValue<T>(object instance)
        {

            return (T)_propertyInfo.GetValue(instance, null);
        }

        public object CreateInstance(object instance = null)
        {
            var result = Constructor.Invoke(null);

            if (instance != null)
              _propertyInfo.SetValue(instance, result, null);

            return result;
        }

        private static object ConvertToObject(Type type, string value)
        {
            if (type == typeof (byte[]))
                return Utils.HexToArray(value);

            if (type == typeof(List<byte>))
                return Utils.HexToArray(value).ToList();

            return value == null ? null : Convert.ChangeType(value, type, XmlCulture);  
        }

        public void SetValue(object instance, string value)
        {
            var valueAsObject = ConvertToObject(NullableGenericType ?? Type, value);

            _propertyInfo.SetValue(instance, valueAsObject, null);
        }
    }

    public class ClassDescription
    {
        public Dictionary<string, PdSimple> Attributes = new Dictionary<string, PdSimple>();

        public Dictionary<string, PdClassBase> Nodes = new Dictionary<string, PdClassBase>();


    }

    public class PropertyDescription
    {
        protected PropertyInfo PropertyInfo;

        public PropertyDescription(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            PropertyFabric = new TypeFabric(propertyInfo);
        }

        public string PropertyName { get { return PropertyInfo.Name; } }

        public TypeFabric PropertyFabric { get; private set; }
    }

    


    /// <summary>
    /// Для членов класса, которые создаются без конструкторв (int, string, double?);
    /// Они сериализуются в аттрибуты;
    /// </summary>
    public class PdSimple : PropertyDescription
    {
        public PdSimple(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo)
        {
            AttrubuteName = attribute.XmlElementName ?? PropertyInfo.Name;
        }

        public string AttrubuteName { get; private set; }

    }

    /// <summary>
    /// Для членов класса, которые создаются с помощью конструктора. Классы, коллекции, словари;
    /// </summary>
    public abstract class PdClassBase : PropertyDescription
    {
        protected PdClassBase(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo)
        {
            NodeName = attribute.XmlElementName ?? PropertyInfo.Name;
        }

        public string NodeName { get; private set; }


    }

    /// <summary>
    /// Если член класса простой класс;
    /// </summary>
    public class PdClass : PdClassBase
    {
        public PdClass(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo, attribute)
        {
        }


    }

    public class PdByteArray : PdSimple
    {
        public PdByteArray(PropertyInfo propertyInfo, JsonSerialize attribute) : base(propertyInfo, attribute)
        {
        }
    }


    /// <summary>
    /// Если член класса имеет элементы коллекции;
    /// </summary>
    public abstract class PdListBase : PdClassBase
    {
        protected abstract TypeFabric GetItemFabric();

        protected PdListBase(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo, attribute)
        {

        }


        private TypeFabric _itemFabric;
        /// <summary>
        /// Фабрика элемента колекии
        /// </summary>
        public TypeFabric ItemFabric { get { return _itemFabric ?? (_itemFabric = GetItemFabric()); } }


    }

    /// <summary>
    /// Если свойстов - List;
    /// </summary>
    public class PdList : PdListBase
    {
        public PdList(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo, attribute)
        {
        }

        protected override TypeFabric GetItemFabric()
        {
            return new TypeFabric(PropertyInfo.PropertyType.GetGenericArguments()[0]);
        }
    }

    /// <summary>
    /// Если свойство - справочник;
    /// </summary>
    public class PdDictionary : PdListBase
    {
        public PdDictionary(PropertyInfo propertyInfo, JsonSerialize attribute)
            : base(propertyInfo, attribute)
        {
            KeyFabric = new TypeFabric(propertyInfo.PropertyType.GetGenericArguments()[0]);
        }


        protected override TypeFabric GetItemFabric()
        {
            return new TypeFabric(PropertyInfo.PropertyType.GetGenericArguments()[1]);
        }

        public TypeFabric KeyFabric { get; private set; }
    }
}
