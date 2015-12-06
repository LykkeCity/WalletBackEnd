using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Common.Log;

namespace Common
{
    public static class UrlUtils
    {
        public static string EncodeUrl(this string data)
        {
            return HttpUtility.UrlEncode(data);
        }

        public static string CutTheHttpFromUrl(string url)
        {
            var p = url.IndexOf('/');
            url = url.Substring(p + 2, url.Length - p - 2);

            return url;
        }


        private static string ToUrlParamString(IEnumerable<KeyValuePair<string, string>> data)
        {
            var result = new StringBuilder();
            foreach (var itm in data)
            {
                if (result.Length > 0)
                    result.Append('&');

                result.Append(itm.Key + '=' + itm.Value.EncodeUrl());
            }

            return result.ToString();
        }


        public static string ToUrlParamString(this object data, params string[] ignoreFields)
        {
            return ToUrlParamString(data, null, ignoreFields);
        }


        public static string ToUrlParamString(this object data, ILog log, params string[] ignoreFields)
        {
            if (data == null)
                return null;

            var strData = data as string;
            if (strData != null)
                return strData;

            var kvp = data as IEnumerable<KeyValuePair<string, string>>;
            if (kvp != null)
                return ToUrlParamString(kvp);



            var result = new StringBuilder();
            foreach (var pi in data.GetType().GetProperties())
            {

                try
                {
                    var value = pi.GetValue(data, null);
                    if (value == null)
                        continue;

                    var field = pi.Name.ToLower();

                    var found = ignoreFields.Any(ignoreField => field == ignoreField.ToLower());

                    if (found) continue;

                    if (result.Length > 0)
                        result.Append('&');


                    var valueAsString = ConvertParamToString(value, pi.PropertyType);


                    result.Append(field + '=' + valueAsString.EncodeUrl());
                }
                catch (Exception ex)
                {
                    log?.WriteError("ParseModelToParams", data.GetType().ToString(), pi.Name, ex);
                }
            }

            return result.ToString();
        }



        private static string ConvertIenumerable(IEnumerable src)
        {
            var strs = new StringBuilder();
            foreach (var o in src)
            {
                if (strs.Length > 0)
                    strs.Append("|");

                strs.Append(ConvertParamToString(o, o.GetType()));
            }
            return strs.ToString();
        }


        private static string ConvertParamToString(object value, Type type)
        {
            if (type == typeof (DateTime))
                return ((DateTime) value).ToIsoDateTime();

            if (type == typeof (string))
                return (string) value;

            var objects = value as IEnumerable;
            if (objects != null)
                return ConvertIenumerable(objects);

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }


        public static string ExtractDomain(string url)
        {
            var i1 = url.IndexOf(@"//", StringComparison.Ordinal);
            var i2 = url.IndexOf(@"/",i1+2, url.Length - i1 -2, StringComparison.Ordinal);
            if (i2 == -1)
                return url;

            return url.Substring(0, i2);
        }

        public static readonly IFormatProvider FormatProvider = new DateTimeFormatInfo { FullDateTimePattern = "dd.MM.yyyy", LongDatePattern = "dd.MM.yyyy", ShortDatePattern = "dd.MM.yyyy"};

        public static void PopulateObject(this NameValueCollection src, object obj)
        {
            foreach (
                var pi in
                    obj.GetType().GetProperties().Where(itm => itm.PropertyType.IsPublic && itm.CanRead && itm.CanWrite)
                )
            {
                var value = src[pi.Name];
                if (value != null)
                    pi.SetValue(obj, value);
            }
        }

    
    }
}
