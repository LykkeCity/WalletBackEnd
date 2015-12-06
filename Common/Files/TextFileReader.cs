using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Files
{
    public static class TextFileReader
    {
        public static IEnumerable<string> ReadTextFile(string fileName)
        {
            using (var streanReader = new StreamReader(new FileStream(fileName, FileMode.Open),Encoding.UTF8))
            {
                var line = streanReader.ReadLine();
                while (line != null)
                {
                    yield return line;
                    line = streanReader.ReadLine();
                }
            }
        }

    }

}
