using System;
using System.Collections.Generic;

namespace Common.TypeMappers
{
    /// <summary>
    /// Кидаем при любых ошибках в классах связок
    /// </summary>
    public class ExceptionOfMapping : Exception
    {

        public ExceptionOfMapping(string message):base(message)
        {
            
        }
        
    }

    /// <summary>
    /// Аттрибут, который мы вешаем  над свойством доменного класса, чтобы указать в какое свойство контрактного класса мы мэппим это свойство
    /// </summary>
    public class SrcPropertyName : Attribute
    {
        public SrcPropertyName(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }

    /// <summary>
    /// Атрибут, который можно повесить свойству классу контракту, чтобы он не мэппился с доменным классом
    /// </summary>
    public class DoNotMap : Attribute
    {
        
    }

    /// <summary>
    /// Класс - контейнер, содержащий наборы типов, которые необходимо мэпить друг с другом;
    /// </summary>
    public class TypeMapper
    {

        private readonly Dictionary<Type, TypeMap> _maps = new Dictionary<Type, TypeMap>();
        private readonly object _lock = new object();

        /// <summary>
        /// Регистрируем типы, которые необходимо связать между собой.
        /// Для типа получателя необходимо указать аттрибуты у свойств, которые должны получить данные
        /// </summary>
        /// <param name="typeSrc">тип источник источник</param>
        /// <param name="typeDest">тип получатель информации</param>
        /// <param name="attrDest">тип аттрубута, которым помечается свойства в классе получателе</param>
        public void Register(Type typeSrc, Type typeDest, Type attrDest)
        {
            var newMap = TypeMap.Create(typeSrc, typeDest, attrDest);

            lock (_lock)
                _maps.Add(typeSrc, newMap);
        }

        public void Map(object src, object dest)
        {
            TypeMap map;

            lock (_lock)
            {
                if (!_maps.ContainsKey(src.GetType()))

                    throw new Exception("Mapper does not have the map for the src Type " +
                                        src.GetType());

                map = _maps[src.GetType()];
            }

            map.SynchInstances(src, dest);

        }

        public object CreateMappedInstance(object srcObject)
        {
            TypeMap map;

            lock (_lock)
            {
                if (!_maps.ContainsKey(srcObject.GetType()))

                    throw new Exception("Mapper does not have the map for the src Type " +
                                        srcObject.GetType());

                map = _maps[srcObject.GetType()];
            }

            return map.CreateDestInstance(srcObject);
        }

        public TSrc CreateMappedInstance<TSrc>(object srcObject) where TSrc : class
        {
            return (TSrc) CreateMappedInstance(srcObject);
        }


    }
}
