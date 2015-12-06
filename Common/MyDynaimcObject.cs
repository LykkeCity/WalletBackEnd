using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class MyDynamicClass : DynamicObject
    {
        private readonly Dictionary<string, dynamic> _properties =
            new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);



        public override bool TryGetMember(GetMemberBinder binder, out dynamic result)
        {
            result = _properties.ContainsKey(binder.Name) ? this._properties[binder.Name] : null;

            return true;
        }

        public void SetMember(string memberName, dynamic value)
        {
            if (value == null)
            {
                if (_properties.ContainsKey(memberName))
                    _properties.Remove(memberName);
            }
            else
                _properties[memberName] = value;
            
        }

        public override bool TrySetMember(SetMemberBinder binder, dynamic value)
        {
            SetMember(binder.Name, value);
            return true;
        }

    }
}

