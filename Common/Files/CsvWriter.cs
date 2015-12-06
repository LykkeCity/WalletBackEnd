using System.IO;
using System.Text;

namespace Common.Files
{
    public class CsvWriter
    {
        readonly MemoryStream _ms = new MemoryStream();

        private readonly Encoding _encoding;
        private readonly StringBuilder _sb = new StringBuilder();
        public CsvWriter():this(Encoding.GetEncoding(1251))
        {

        }

        public CsvWriter(Encoding encoding)
        {
            _encoding = encoding;
        }

        public void Clear()
        {
            _ms.SetLength(0);
        }

        public void WriteLine(params string[] data)
        {
            lock (_sb)
            {
                _sb.Clear();

                foreach (var s in data)
                {
                    if (_sb.Length > 0)
                        _sb.Append(";");

                    _sb.Append("\"" + s + "\"");
                }

                _sb.Append("\n");

                var buffer = _encoding.GetBytes(_sb.ToString());
                _ms.Write(buffer, 0, buffer.Length);
            }
        }

        public MemoryStream GetResult()
        {
            _ms.Position = 0;
            return _ms;
        }

    }
}
