using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Common
{
    public class XmlXPathReader
    {

        private readonly Stream _xmlInMemory;
        public XmlXPathReader(string xml)
        {
            _xmlInMemory = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        }


        private static bool FineNode(string nodeName, XmlReader xmlReader)
        {

            if (nodeName[0] == '@')
            {
                nodeName = nodeName.Substring(1, nodeName.Length - 1);
                xmlReader.MoveToAttribute(nodeName);

                return xmlReader.Name == nodeName;

            }

            var iLeft = nodeName.IndexOf('[');

            if (iLeft>0)
            {
                var iRight = nodeName.IndexOf(']', iLeft);
                var index = int.Parse(nodeName.Substring(iLeft+1, iRight - iLeft-1));
                nodeName = nodeName.Substring(0, iLeft);

                var i = 0;

                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType != XmlNodeType.Element)
                        continue;

                    if (xmlReader.Name == nodeName)
                        if (i == index)
                            return true;
                    i++;
                }

            }

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                    if (xmlReader.Name == nodeName)
                        return true;
                        
            }

            return false;

        }


        private string GetXPathValue(string xPath, bool throwException = true)
        {
            var paths = xPath.Split('/');
            _xmlInMemory.Position = 0;

            var xmlSettings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true, IgnoreProcessingInstructions = true};
            using (var xmlReader = XmlReader.Create(_xmlInMemory, xmlSettings))
            {
                foreach (var path in paths)
                {
                    if (FineNode(path, xmlReader))
                        continue;
                    
                   if (throwException)
                      throw new Exception("Invalid Path :" + xPath);
                    return null;

                }

                if (paths[paths.Length - 1][0] != '@')
                    xmlReader.Read();

                return xmlReader.Value;
            }
        }



        public string AsString(string xPath, bool throwException = true)
        {
            return GetXPathValue(xPath, throwException);
        }

        public int AsInt(string xPath)
        {
            return int.Parse(GetXPathValue(xPath));
        }


        public long AsLong(string xPath)
        {
            return long.Parse(GetXPathValue(xPath), CultureInfo.InvariantCulture);
        }

        public double AsDouble(string xPath)
        {
            return double.Parse(GetXPathValue(xPath), CultureInfo.InvariantCulture);
        }


        public bool AsBoolean(string xPath)
        {
            return bool.Parse(GetXPathValue(xPath));
        }

        public bool ElementExists(string xPath)
        {
            return GetXPathValue(xPath, false)!=null;

        }

        public object Cast(string xPath, Type type)
        {
            var value = GetXPathValue(xPath);
           return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        public static string Read(string xml, string xPath)
        {
            var reader = new XmlXPathReader(xml);
            return reader.AsString(xPath, false);
        }
    }
}
