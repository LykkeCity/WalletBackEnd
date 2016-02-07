using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Utility
{
    internal class FixVersionColumnConcurrencyMode
    {
        private static void Main(string[] args)
        {
            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(directoryPath, "*.edmx");
            foreach (var file in files)
            {
                XDocument xmlDoc = XDocument.Load(file);

                IEnumerable<XElement> versionColumns =
                    from el in xmlDoc.Descendants()
                    where (string)el.Attribute("Name") == "Version"
                    && (string)el.Attribute("Type") == "Binary"
                    && (string)el.Attribute("ConcurrencyMode") != "Fixed"
                    select el;
                bool modified = false;
                foreach (XElement el in versionColumns)
                {
                    modified = true;
                    el.SetAttributeValue("ConcurrencyMode", "Fixed");
                }
                if (modified)
                    xmlDoc.Save(file);
            }
        }
    }
}