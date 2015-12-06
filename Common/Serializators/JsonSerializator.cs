using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Common.Json;

namespace Common.Serializators
{
    public class JsonSerialize:Attribute
    {
        public JsonSerialize(string xmlElementName = null)
        {
            XmlElementName = xmlElementName;

        }
        public string XmlElementName { get; private set; }
    }




    public class JsonSerializator<T> where T : new()
    {

        protected readonly Encoding XmlEncoding = Encoding.UTF8;
        protected IFormatProvider XmlCulture = CultureInfo.InvariantCulture;

        private readonly Dictionary<Type, ClassDescription> _types = new Dictionary<Type, ClassDescription>();

        private void DetectXmlAttributes(Type type)
        {  
            if (_types.ContainsKey(type))
                return;

            var classDescription = new ClassDescription();
            _types.Add(type, classDescription);

            foreach (var pi in ReflectionUtils.FindProperiesWithAttribute<JsonSerialize>(type))
            {

                if (ReflectionUtils.DoesThePropertyImplementTheInterface(pi.PropertyInfo.PropertyType,
                                                                         typeof (IList<byte>)))
                {
                    var pd = new PdByteArray(pi.PropertyInfo, pi.AttributeInstance);
                    classDescription.Attributes.Add(pi.PropertyInfo.Name, pd);
                }
                else
                if (ReflectionUtils.DoesThePropertyImplementTheInterface(pi.PropertyInfo.PropertyType,
                                                                         typeof (IDictionary)))
                {
                    var pd = new PdDictionary(pi.PropertyInfo, pi.AttributeInstance);
                    classDescription.Nodes.Add(pi.PropertyInfo.Name, pd);
                    // Исследуем элемент словаря
                    DetectXmlAttributes(pd.ItemFabric.Type);
                }

                else

                if (ReflectionUtils.DoesThePropertyImplementTheInterface(pi.PropertyInfo.PropertyType, typeof (IList)))
                {
                    var pd = new PdList(pi.PropertyInfo, pi.AttributeInstance);
                    classDescription.Nodes.Add(pi.PropertyInfo.Name, pd);
                    // Исследуем элемент листа
                    DetectXmlAttributes(pd.ItemFabric.Type);
                }

                else
                if (ReflectionUtils.IsSimpleType(pi.PropertyInfo.PropertyType) || (ReflectionUtils.IsNullableType(pi.PropertyInfo.PropertyType)))
                    classDescription.Attributes.Add(pi.PropertyInfo.Name,
                                                    new PdSimple(pi.PropertyInfo, pi.AttributeInstance));



                else
                {
                    var pd = new PdClass(pi.PropertyInfo, pi.AttributeInstance);
                    classDescription.Nodes.Add(pi.PropertyInfo.Name, pd);
                    // Исследуем член класса
                    DetectXmlAttributes(pd.PropertyFabric.Type);
                }
            }
        }

        public JsonSerializator()
        {

            DetectXmlAttributes(typeof(T));
        }


        public string Serialize(T obj)
        {
            string result;

            using (var jw = new JsonWriter())
            {
                Serialize(obj, jw);
                result = jw.Json;
            }

            return result;
        }

   
        private void SerializeProperties(object obj, IJsonWriter jsonWriter)
        {

            foreach (var info in _types[obj.GetType()].Attributes)
            {
                var value = info.Value.PropertyFabric.GetValueAsString(obj);
                jsonWriter.Write(info.Key, value);
            }

        }

        private void SerializeListItem(PdListBase pdList, object item, string key, IJsonWriter jsonWriter)
        {
            if (item == null)
            {
                if (key == null)
                    jsonWriter.Write(null);
                else
                    jsonWriter.Write(key, null);

            }
            else if (pdList.ItemFabric.IsSimple)
            {
                if (key == null)
                    jsonWriter.Write(pdList.ItemFabric.GetValueAsString(item));
                else
                    jsonWriter.Write(key, pdList.ItemFabric.GetValueAsString(item));
            }
            else
                using (var jw = jsonWriter.WriteClass(key))
                {
                    Serialize(item, jw);

                }
        }

        private void SerializeList(object obj, PdListBase pdList, IJsonWriter jsonWriter)
        { 
            var list = pdList.PropertyFabric.GetValue<IList>(obj);

            if (list == null)
                jsonWriter.Write(pdList.NodeName, null);
            else
                using (var jw = jsonWriter.WriteArray(pdList.NodeName))
                {
                    foreach (var itm in list)
                        SerializeListItem(pdList, itm, null, jw);
                }


        }

        private void SerializeDict(object obj, PdDictionary pdDict, IJsonWriter jsonWriter)
        {
            var dict = pdDict.PropertyFabric.GetValue<IDictionary>(obj);

            if (dict == null)
                jsonWriter.Write(pdDict.NodeName, null);
            else
                using (var jw = jsonWriter.WriteArray(pdDict.NodeName))
                {
                    foreach (var key in dict.Keys)
                    {
                        var keyStr = pdDict.KeyFabric.GetValueAsString(key);
                        SerializeListItem(pdDict, dict[key], keyStr, jw);
                    }

                }
        }


        private void SerializeNodes(object obj, IJsonWriter jsonWriter)
        {
            foreach (var info in _types[obj.GetType()].Nodes)
            {
                if (info.Value is PdClass)
                {
                    var pdClass = info.Value as PdClass;

                    var instance = pdClass.PropertyFabric.GetValue<object>(obj);
                    if (instance == null)
                        jsonWriter.Write(pdClass.NodeName, null);
                    else
                    using (var jw =  jsonWriter.WriteClass(pdClass.NodeName))
                    {
                        Serialize(instance, jw);  
                    }


                }

                if (info.Value is PdList)
                {
                    var pdList = info.Value as PdList;
                    SerializeList(obj, pdList, jsonWriter);

                }

                if (info.Value is PdDictionary)
                {
                    var pdDict = info.Value as PdDictionary;
                    SerializeDict(obj, pdDict, jsonWriter);
                }
            }
        }

        private void Serialize(object obj, IJsonWriter jsonWriter)
        {

            // Сериализуем всё, что попадает в аттрибуты - простые классы свойства
            SerializeProperties(obj, jsonWriter);

            SerializeNodes(obj, jsonWriter);

        }

        private void DeserializeClassProperty(object obj, PdClassBase pd, IJsonReader jsonReader)
        {
            var instance = pd.PropertyFabric.CreateInstance(obj);
            Deserialize(instance, jsonReader);

        }

        private object DeserializeListItem(PdListBase pd, JsonObjectBase jsonData)
        {
            object item = null;

            if (jsonData is JsonObjectSimple)
            {
                var jsonSimple = jsonData as JsonObjectSimple;

                if (jsonSimple.Value != null)
                    item = pd.ItemFabric.StringToObject(jsonSimple.Value);
            }
            else

                if (jsonData is JsonObjectClass)
                {
                    var jsonClass = jsonData as JsonObjectClass;
                    item = pd.ItemFabric.CreateInstance();
                    Deserialize(item, jsonClass.Class);
                }

            return item;
        }

        private void DeserializeList(object obj, PdList pd, IJsonReader jsonReader)
        {
 
            var list = (IList)pd.PropertyFabric.CreateInstance(obj);


            var jsonData = jsonReader.ReadNext();

            while (jsonData != null)
            {
                var item = DeserializeListItem(pd, jsonData);
                list.Add(item);
                jsonData = jsonReader.ReadNext();
            }
         
        }

        private void DeserializeDict(object obj, PdDictionary pd, IJsonReader jsonReader)
        {
      
            var dict = (IDictionary)pd.PropertyFabric.CreateInstance(obj);

            var jsonData = jsonReader.ReadNext();

            while (jsonData != null)
            {
                var key = pd.KeyFabric.StringToObject(jsonData.Name);
                var item = DeserializeListItem(pd, jsonData);
                dict.Add(key, item);
                jsonData = jsonReader.ReadNext();
            }
          
        }

        public void Deserialize(object obj, IJsonReader jsonReader)
        {

            var cd = _types[obj.GetType()];

            var data = jsonReader.ReadNext();

            while (data != null)
            {
                if (data is JsonObjectSimple)
                {
                    var jsonSimple = data as JsonObjectSimple;

                    if (cd.Attributes.ContainsKey(jsonSimple.Name))
                        cd.Attributes[jsonSimple.Name].PropertyFabric.SetValue(obj, jsonSimple.Value);
                }

                if (data is JsonObjectClass)
                {
                    var jsonObject = data as JsonObjectClass;

                    if (cd.Nodes.ContainsKey(jsonObject.Name))
                        DeserializeClassProperty(obj, cd.Nodes[jsonObject.Name], jsonObject.Class); 
                }

                if (data is JsonArray)
                {
                    var jsonArray = data as JsonArray;

                    if (cd.Nodes.ContainsKey(jsonArray.Name))
                    {
                        var pd = cd.Nodes[jsonArray.Name];

                        if (pd is PdList)
                            DeserializeList(obj, (PdList)cd.Nodes[jsonArray.Name], jsonArray.Array);

                        if (pd is PdDictionary)
                            DeserializeDict(obj, (PdDictionary)cd.Nodes[jsonArray.Name], jsonArray.Array);  
                        
                    }
                }

                data = jsonReader.ReadNext();

            }

        }


        public T Deserialize(string json)
        {
            var obj = new T();
            var jsonReader = new JsonReader(json);
            Deserialize(obj, jsonReader);

            return obj;
        }
   
    }
      
}
