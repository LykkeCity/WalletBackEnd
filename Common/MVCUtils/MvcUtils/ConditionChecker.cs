using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Common;

namespace MvcUtils
{
    public enum PopoverPosition
    {
        Left, Top, Right, Bottom
    }
    public class SpaDivError : Attribute
    {
        public SpaDivError(string errorDiv, PopoverPosition position = PopoverPosition.Left)
        {
            ErrorDiv = errorDiv;
            Position = position;
        }

        public string ErrorDiv { get; private set; }

        public PopoverPosition Position { get; private set; }

    }




    public class ModelResult
    {
        public string DivId { get; set; }
        public string Text { get; set; }
        public PopoverPosition Position { get; set; }
    }


    public static class MvcConditionChecker
    {
        public static ModelResult Check(object data, IEnumerable<KeyValuePair<string, ModelState>> modelState)
        {
            foreach (var ms in modelState)
            {
                if (ms.Value.Errors.Count ==0 )
                    continue;

                var pi = data.GetType().GetProperty(ms.Key);
                var attr = ReflectionUtils.GetAttribute<SpaDivError>(pi);
                if (attr != null)
                  return new ModelResult {DivId = attr.ErrorDiv, Text = ms.Value.Errors[0].ErrorMessage, Position = attr.Position};
            }

            return null;

        }


    }
}
