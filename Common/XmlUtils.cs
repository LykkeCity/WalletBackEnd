using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Common
{
    public static class XmlUtils
    {
        public static void IgnoreXmlNode(XmlReader xmlReader, string nodeName, bool isEmpty)
        {
            if (isEmpty)
                return;

            xmlReader.Read();

            if (xmlReader.Name == nodeName && xmlReader.NodeType == XmlNodeType.EndElement)
                return;

            while (xmlReader.NodeType != XmlNodeType.Element)
                xmlReader.Read();


            IgnoreXmlNode(xmlReader, xmlReader.Name, xmlReader.IsEmptyElement);

        }

        private static readonly Dictionary<char, string> XmlEscapes = new Dictionary<char, string>
        {
            {'<',"&lt"},
            {'>',"&gt"},
            {'"',"&quot"},
            {'\'',"&apos"},
            {'&',"&amp"},

        };

        public static string EncodeXmlString(this string src)
        {
            var hasEscape = src.Any(s => XmlEscapes.ContainsKey(s));

            if (!hasEscape)
                return src;

            var result = new StringBuilder();

            foreach (var s in src)
                if (XmlEscapes.ContainsKey(s))
                    result.Append(XmlEscapes[s]);
                else
                    result.Append(s);

            return result.ToString();
        }
    }
}
