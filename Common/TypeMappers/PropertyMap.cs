using System;
using System.Collections;
using System.Reflection;

namespace Common.TypeMappers
{
    /// <summary>
    /// Класс, который связывает между собой свойства, которые могут быть трёх типов.
    /// 1. тип значения или строки - не требуется создания экземпляра
    /// 2. класс - необходимо их между собой связывать
    /// 3. массив 1-х или 2-х типов
    /// </summary>
    public abstract class PropertyMap
    {
        public abstract void FillData(object srcInstance, object destInstance);

        public static bool IsInterfaces(Type src, Type dest)
        {
            if (!dest.IsInterface)
                return false;

            // проверяем, имплементирует ли входной тип выходной интерфейс
            if (ReflectionUtils.DoesThePropertyImplementTheInterface(src,
                                                                     dest))
                return true;

            // а может они оба одинаковых интерфейса
            if (src == dest)
                return true;

            throw new Exception("Dest type is an interface=[" + dest +
                                "], but source type=[" + src + "] does not implement it");
        }

        public static bool IsInterfaces(PropertyInfo srcPropertyInfo, PropertyInfo destPropertyInfo)
        {
            return IsInterfaces(srcPropertyInfo.PropertyType, destPropertyInfo.PropertyType);
        }


        public static PropertyMap Create(PropertyInfo srcPropertyInfo, PropertyInfo destPropertyInfo, string[] destPropertyPath, Type attrType)
        {
            if (ReflectionUtils.DoesThePropertyImplementTheInterface(destPropertyInfo.PropertyType, typeof (IList)))
                return new ArrayPropertyMap(srcPropertyInfo, destPropertyInfo, attrType);


            if (ReflectionUtils.IsSimpleType(srcPropertyInfo.PropertyType))
                return new SimplePropertyMap(srcPropertyInfo, destPropertyInfo, destPropertyPath);

            if (IsInterfaces(srcPropertyInfo, destPropertyInfo))
                return new SimplePropertyMap(srcPropertyInfo, destPropertyInfo, destPropertyPath);

            return new ClassPropertyMap(srcPropertyInfo, destPropertyInfo, attrType);
        }
    }



    public class ArrayPropertyMap : PropertyMap
    {

        // Конструктор, чтобы создавать массив получатель
        private readonly ArrayConstructorBase _destArrayConstructor;


        // мэппер типов на случай если у нас элемент массива класс или массив, иначе null
        private readonly TypeMap _elementTypeMap;

        private readonly PropertyInfo _destPropertyInfo;
        private readonly PropertyInfo _srcPropertyInfo;


        public ArrayPropertyMap(PropertyInfo srcPropertyInfo, PropertyInfo destPropertyInfo, Type attrType)
        {

            _srcPropertyInfo = srcPropertyInfo;
            _destPropertyInfo = destPropertyInfo;


            var srcElementType = ReflectionUtils.GetElementType(srcPropertyInfo.PropertyType);
            var destElementType = ReflectionUtils.GetElementType(destPropertyInfo.PropertyType);

            var isSimpleType = ReflectionUtils.IsSimpleType(srcElementType);
            var areInterfaces = IsInterfaces(srcElementType, destElementType);

            if (isSimpleType || areInterfaces)
            {
                // если тип простой, просто проверим чтобы они были одинаковые - иначе ничего может не получиться в рантайме
                if (!areInterfaces && srcElementType != destElementType)
                    throw new Exception("There are different simple element types for mapping Lists. ElementSrc=[" + srcElementType + "]. ElementDest=[" + destElementType + "]. Theyshould be the same");
            }
            else
            {
                _elementTypeMap = TypeMap.Create(srcElementType, destElementType,attrType);
            }

            _destArrayConstructor = ArrayConstructorBase.Create(destPropertyInfo.PropertyType);
        }

        private static void FillSimpleData(IListReadOnly src, IList dest)
        {
            var i = 0;
            foreach (var srcitem in src)
            {
               dest[i++] = srcitem; 
            }
                
        }

        private void FillReferenceData(IListReadOnly src, IList dest)
        {
            var i = 0;
            foreach (var srcitem in src)
            {
                if (dest[i] == null)
                {
                    var newElement = _elementTypeMap.CreateDestInstance(srcitem);
                    dest[i] = newElement;
                }
                else
                    _elementTypeMap.SynchInstances(srcitem, dest[i]);

                i++;
            }



        }

        public override void FillData(object srcInstance, object destInstance)
        {
            var srcValue = _srcPropertyInfo.GetValue(srcInstance, null);
            if (srcValue == null)
            {
                _destPropertyInfo.SetValue(destInstance, null, null);
                return;
            }

            var destValue = _destPropertyInfo.GetValue(destInstance, null);



            var srcList = SourceArrayList.Create(srcValue);


            if (srcList == null)
                throw new Exception("[" + srcValue + "] of the class[" + srcInstance +
                                    "] should have the IList or IListReadOnly interface");

            if (destValue == null)
            {
                destValue = _destArrayConstructor.Invoke(srcList);
                _destPropertyInfo.SetValue(destInstance, destValue, null);
            }

            var destList = destValue as IList;

            if (destList == null)
                throw new Exception("[" + destValue + "] of the class[" + destInstance +
                                    "] should have the IList interface");

            if (destList.Count != srcList.Count)
            {
                if (_destArrayConstructor.CahBeSynched)
                    _destArrayConstructor.SynchRecordsCount(srcList, destList);
                else
                {
                    destValue = _destArrayConstructor.Invoke(srcList);
                    _destPropertyInfo.SetValue(destInstance, destValue, null);
                }
            }

            if (_elementTypeMap == null)
                FillSimpleData(srcList, destList);
            else

                FillReferenceData(srcList, destList);


        }
    }

    public class SimplePropertyMap : PropertyMap
    {
        public SimplePropertyMap(PropertyInfo srcPropertyInfo, PropertyInfo destPropertyInfo, string[] destPropertyPath)
        {
            _srcPropertyInfo = srcPropertyInfo;
            _destPropertyInfo = destPropertyInfo;
            _destPropertyPath = destPropertyPath;
        }

        private readonly PropertyInfo _destPropertyInfo;
        private readonly PropertyInfo _srcPropertyInfo;
        private readonly string[] _destPropertyPath;

        private object GetSrcValue(object srcInstance)
        {
            if (_destPropertyPath.Length == 1)
                return _srcPropertyInfo.GetValue(srcInstance, null);

            var result = srcInstance;

            foreach (string t in _destPropertyPath)
            {
                var prop = ReflectionUtils.FindPropertyByName(result.GetType(), t);
                result = prop.GetValue(result, null);
            }

            return result;

        }

        public override void FillData(object srcInstance, object destInstance)
        {
            var srcValue = GetSrcValue(srcInstance);
            _destPropertyInfo.SetValue(destInstance, srcValue, null);
        }
        
    }

    /// <summary>
    /// Если это класс - то посути это корневой элемент, который надо мэпить через TypeMapper
    /// </summary>
    public class ClassPropertyMap : PropertyMap
    {

        private readonly TypeMap _typeMap;
        private readonly PropertyInfo _destPropertyInfo;
        private readonly PropertyInfo _srcPropertyInfo;
        public ClassPropertyMap(PropertyInfo srcPropertyInfo, PropertyInfo destPropertyInfo, Type attrType)
        {
            _srcPropertyInfo = srcPropertyInfo;
            _destPropertyInfo = destPropertyInfo;
            _typeMap = TypeMap.Create(srcPropertyInfo.PropertyType, destPropertyInfo.PropertyType, attrType);
        }

        public override void FillData(object srcInstance, object destInstance)
        {
            var srcValue = _srcPropertyInfo.GetValue(srcInstance, null);

            if (srcValue == null)
            {
                _destPropertyInfo.SetValue(destInstance, null, null);
                return;
            }

            var destValue = _destPropertyInfo.GetValue(destInstance, null);

            if (destValue == null)
            {
                destValue = _typeMap.CreateDestInstance(srcValue);
                _destPropertyInfo.SetValue(destInstance, destValue, null);
            }
            else
                _typeMap.SynchInstances(srcValue, destValue);

        }
    }
}
