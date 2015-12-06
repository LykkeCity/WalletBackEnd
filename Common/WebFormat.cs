using System;
using System.Linq;

namespace Common
{
    public static class WebFormat
    {
        public static string FormatUrls(this string line, params string[] urls)
        {
            if (urls.Length == 0)
                return line;

            var no = 0;
            var indexFrom = line.IndexOf('{');

            while (indexFrom >= 0)
            {
                if (no >= urls.Length)
                    break;

                var indexTo = line.IndexOf('}', indexFrom);

                if (indexTo < 0)
                    break;


                var lineToReplace = line.Substring(indexFrom, indexTo - indexFrom + 1);

                line = line.Replace(lineToReplace,
                    "<a href=\"" + urls[no++] + "\">" + lineToReplace.Substring(1, lineToReplace.Length - 2) + "</a>");


                indexFrom = line.IndexOf('{');
            }

            return line;
        }




        private static int FindSeparatorForWikiUrl(this string line)
        {
            line = line.ToLower();
            if (line.IndexOf("http://", StringComparison.Ordinal) > 0)
                return line.FindIndexBeforeTheStatement("http://", ':');
            

            if (line.IndexOf("https://", StringComparison.Ordinal)>0)
                return line.FindIndexBeforeTheStatement("https://", ':');


            return line.IndexOfFromEnd(':');
        }



        private static string FindAndReplace(this string line, string open, string close, Func<string, string> action)
        {

            var indexFrom = line.IndexOf(open, StringComparison.Ordinal);

            while (indexFrom >= 0)
            {

                var indexTo = line.IndexOf(close, indexFrom+open.Length, StringComparison.Ordinal);

                if (indexTo < 0)
                    break;

                var resltString = action(line.Substring(indexFrom + open.Length, indexTo - indexFrom - open.Length));
                if (!string.IsNullOrEmpty(resltString))
                    line = line.Substring(0, indexFrom) + resltString + line.Substring(indexTo + open.Length, line.Length - indexTo - open.Length);


                indexFrom = line.IndexOf(open, StringComparison.Ordinal);
            }

            return line;
        }



        public static string FormatWiki(this string line, params string[] replaces)
        {

            var i = 0;

            line = replaces.Aggregate(line, (current, replace) => current.Replace("{" + (i++) + "}", replace));

            line = line.FindAndReplace("{clr:", "{clr}", detectedLine =>
            {
                var idx = detectedLine.IndexOf('}');
                var color = detectedLine.Substring(0, idx);
                return "<font color=\"" + color + "\">"+detectedLine.Substring(idx+1, detectedLine.Length - idx-1)+"</font>";

            });


            line = line.FindAndReplace("[[", "]]", detectedLine =>
            {

                var idx = detectedLine.FindSeparatorForWikiUrl();
                var lastLine = detectedLine.Substring(idx + 1, detectedLine.Length - idx - 1);

                int paramIdx;

                var urlToInsert = int.TryParse(lastLine, out paramIdx) ? replaces[paramIdx] : lastLine;
                var urlText = detectedLine.Substring(0, idx);

                if (!string.IsNullOrEmpty(urlToInsert))
                    return string.Format("<a href=\"{0}\">", urlToInsert) + urlText + "</a>";

                return null;
            });

            line = line.FindAndReplace("*", "*", toReplace => "<b>" + toReplace + "</b>");

            line = line.FindAndReplace("_", "_", toReplace => "<i>" + toReplace + "</i>");


            return line;
        }
    }
}
