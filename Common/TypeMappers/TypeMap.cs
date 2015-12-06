using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Common.TypeMappers
{


    /// <summary>
    /// Корневой базовый класс мэппер. Мы можем на корневом уровне замэпить или классы или коллекции классов (IList)
    /// </summary>
    public abstract class TypeMap
    {

        public abstract object CreateDestInstance(object srcInstance);
        public abstract void SynchInstances(object srcInstance, object destInstance);


        /// <summary>
        /// Метод, который определяет что у нас в корне - класс или коллекция классов. Если это коллекции - они должны быть как в источнии так и в получателе
        /// </summary>
        /// <param name="typeSrc">Тип источник данных</param>
        /// <param name="typeDest">Тип получатель данных</param>
        /// <param name="attrDest">Аттрибут, которым помечены свойства получателя данных</param>
        /// <returns>Мэп типов (массивный или классовый)</returns>
        public static TypeMap Create(Type typeSrc, Type typeDest, Type attrDest)
        {
            var scrIsList = ReflectionUtils.DoesThePropertyImplementTheInterface(typeSrc, typeof(IList));
            var destIsList = ReflectionUtils.DoesThePropertyImplementTheInterface(typeDest, typeof(IList));

            if (scrIsList != destIsList)
                throw new ExceptionOfMapping(
                    "Error mapping data types. Both types should implement the IList interface or not");

            if (scrIsList && typeSrc != typeof(string))
                return new ArrayTypeMap(typeSrc, typeDest, attrDest);


            return new ClassTypeMap(typeSrc, typeDest, attrDest);
        }

    }



    public class ArrayTypeMap : TypeMap
    {
        private readonly ArrayConstructorBase _destArrayConstructor;
        private readonly TypeMap _arrayItemMap;


        public ArrayTypeMap(Type srcType, Type destType, Type attrDest)
        {
            var srcElement = ReflectionUtils.GetElementType(srcType);
            var destElement = ReflectionUtils.GetElementType(destType);

            _arrayItemMap = Create(srcElement, destElement, attrDest);
            _destArrayConstructor = ArrayConstructorBase.Create(destType);
        }


        public override object CreateDestInstance(object srcInstance)
        {
            var srcList = SourceArrayList.Create(srcInstance);

            if (srcList == null)
                throw new ExceptionOfMapping("Somehow appears that the class " + srcInstance.GetType() + " without IList interface tried to map to the array class (ArrayTypeMapper.CreateDestInstance)");

            var destList = _destArrayConstructor.Invoke(srcList);

            SynchInstances(srcInstance, destList);

            return destList;
        }

        public override void SynchInstances(object srcInstance, object destInstance)
        {
            var srcList = SourceArrayList.Create(srcInstance);

            if (srcList == null)
                throw new ExceptionOfMapping("Somehow appears that the class " + srcInstance.GetType() + " without IList interface tried to map to the array class (ArrayTypeMapper.SynchInstances)");


            var destList = destInstance == null ? _destArrayConstructor.Invoke(srcList) : destInstance as IList;

            if (destList == null)
                throw new ExceptionOfMapping("Somehow appears that the dest class for source class" + srcInstance.GetType() + " without IList interface tried to map to the array class (ArrayTypeMapper.SynchInstances)");

            var i = 0;
            foreach (var itm in srcList)
                destList[i++] = _arrayItemMap.CreateDestInstance(itm);
        }
    }

    /// <summary>
    /// Если мы мэппим простой класс (не массиа) - то используем эту карту связей
    /// </summary>
    public class ClassTypeMap : TypeMap
    {
        private readonly ConstructorInfo _destConstructor;
        private readonly List<PropertyMap> _classMapItems = new List<PropertyMap>();
        private readonly Type _srcType;
        private readonly Type _destType;

        public ClassTypeMap(Type srcType, Type destType, Type attrDest)
        {
            _srcType = srcType;
            _destType = destType;

            // Находим конструктор по умолчанию, чтобы иметь возможность создавать тип - получатель
            _destConstructor = ReflectionUtils.FindDefaultConstructor(destType);

            if (_destConstructor == null)
                throw new ExceptionOfMapping("Error of Mapping Type=" + _destType + ". It must has the default Constructor");

            // Бежим по всем свойствам получателя, помеченным аттрибутом, и ищем все свойства источника с такими же именами, или помеченными аттрибутом SrcPropertyName, 
            // с указанием имени  свойства на получателе
            // Если свойство на получателе имеет аттрибут DoNotMap - значит оно не участвует в мэппинге
            foreach (var destProp in ReflectionUtils.FindProperiesWithAttribute(destType, attrDest))
            {
                if (ReflectionUtils.GetAttribute<DoNotMap>(destProp.PropertyInfo) != null)
                    continue;

                // Если свойство на получателе так же имеет аттрибут SrcPropertyName, то на источнике мы ищем свойство с именем, которое указано в аттрибуте
                var attrSrcProperty = ReflectionUtils.GetAttribute<SrcPropertyName>(destProp.PropertyInfo);
                var propName = attrSrcProperty == null ? destProp.PropertyInfo.Name : attrSrcProperty.PropertyName;

                string[] propertyPath;
                var srcPropertyInfo = ReflectionUtils.FindPropertyByName(_srcType, propName, out propertyPath);

                // Если мы свойства на источнике не нашли - кричим об этом, чтобы потом не удивляться что свойство не обновилось
                if (srcPropertyInfo == null)
                    throw new ExceptionOfMapping("Error of Mapping Type=[" + _destType + "]. Data Property [" + destProp.PropertyInfo.Name + "] does not have a property to link to type [" + srcType + "]");

                _classMapItems.Add(PropertyMap.Create(srcPropertyInfo, destProp.PropertyInfo, propertyPath, attrDest));   
            }

            if (_classMapItems.Count == 0)
                throw new ExceptionOfMapping("Error of Mapping Type=" + _destType + ". It must has at least 1 parameter to map");
        }

        public override object CreateDestInstance(object srcInstance)
        {
            var destInstance = _destConstructor.Invoke(null);

           SynchInstances(srcInstance, destInstance);

            return destInstance;
        }

        public override void SynchInstances(object srcInstance, object destInstance)
        {
            foreach (var classMapItem in _classMapItems)
                (classMapItem).FillData(srcInstance, destInstance);
        }
    }

}
