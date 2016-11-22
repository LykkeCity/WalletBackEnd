using System;
using Newtonsoft.Json;

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

        public static string ToJson(this object src, bool ignoreNulls = false)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(src,
                new JsonSerializerSettings { NullValueHandling = ignoreNulls ? NullValueHandling.Ignore : NullValueHandling.Include });
        }
    }
}
