using System;
using System.Collections.Generic;


namespace Common.IocContainer
{

    public partial class IoC
    {
        private readonly Dictionary<Type, ObjectInstantiatorPerCall> _instanceFactories = new Dictionary<Type, ObjectInstantiatorPerCall>();

        public object CreateInstance(Type type)
        {

            var constructors = type.GetConstructors();

            if (constructors.Length != 1)
                return null;

            lock (_instanceFactories)
            {
                if (!_instanceFactories.ContainsKey(type))
                {
                    var newFactory = new ObjectInstantiatorPerCall(type, this);
                    _instanceFactories.Add(type, newFactory);
                }

                return _instanceFactories[type].GetInstance();
            }
        }

        public T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof(T));
        }


    }
}
