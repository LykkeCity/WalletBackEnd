using System;
using Common;

namespace MvcUtils
{


    public static class WebSiteUtils
    {

        public static double ParseWebSiteRequestDouble(this string src, string divResult, string errorMessage, PopoverPosition position = PopoverPosition.Top)
        {
            try
            {
                return src.ParseAnyDouble();
            }
            catch (Exception)
            {
                throw new WebSiteFailException(divResult, errorMessage, position);
            }
        }

    }
}
