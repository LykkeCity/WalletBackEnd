using System;

namespace Common
{
    public static class JsonSerialisersExt
    {

        public static T DeserializeJson<T>(this string json, Func<T> createDefault)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                return createDefault();
            }
        }

        public static T DeserializeJson<T>(this string json)
        {
           return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public static string ToJson(this object src)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(src);
        }
    }
}
