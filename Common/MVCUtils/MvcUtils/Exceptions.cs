using System;

namespace MvcUtils
{
    public class WebSiteFailException : Exception
    {

        public WebSiteFailException(string divResult, string message, PopoverPosition position = PopoverPosition.Top)
            : base(message)
        {
            Position = position;
            DivResult = divResult;
        }

        public string DivResult { get; private set; }
        public PopoverPosition Position { get; private set; }

    }
}
