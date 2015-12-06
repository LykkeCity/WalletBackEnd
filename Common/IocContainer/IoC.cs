using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.IocContainer
{

    public class IocException : Exception
    {
        public IocException(string message):base(message)
        {
            
        }
    }



    /// <summary>
    /// IoC контейнер, задача которого связывать реализации интерфейсов с классами
    /// Контейнер создаёт таблицу интерфейсов. При попытке регистрировать два объекта с одинаковыми интерфейсами, они записываются в отдельный лист, и по этому интерфейсу можно вызвать у всех объектов этот интерфейс.
    /// Если несколько объектов реализуют один и тот же интерфейс, они не используются для подставления в конструктор;
    /// </summary>
    public partial class IoC
    {

        private readonly Dictionary<Type, List<IObjectResolver>> _interfaces = new Dictionary<Type, List<IObjectResolver>>();

        private bool _selfTested;




        private void RegisterInterface(Type interfaceType, IObjectResolver objectResolver)
        {
            if (!_interfaces.ContainsKey(interfaceType))
                _interfaces.Add(interfaceType, new List<IObjectResolver>());

            _interfaces[interfaceType].Add(objectResolver);
        }

        public void RegisterSingleTone<TInterface, TInstance>() where TInstance : TInterface
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);
            

            RegisterInterface(interfaceType, new ObjectInstantiatorSingleTone(typeof(TInstance), this));

        }


        public void RegisterPerCall<TInterface, TInstance>() where TInstance : TInterface
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);


            RegisterInterface(interfaceType, new ObjectInstantiatorPerCall(typeof(TInstance), this));

        }

        public void RegisterSingleTone<TInterface>()
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);


            RegisterInterface(interfaceType, new ObjectInstantiatorSingleTone(typeof(TInterface), this));
        }



        public void RegisterPerCall<TInterface>()
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);

            RegisterInterface(interfaceType, new ObjectInstantiatorPerCall(typeof(TInterface), this));
        }

        public void Register<TInstance>(TInstance instance)
        {
            _selfTested = false;

            var interfaceType = typeof(TInstance);

            RegisterInterface(interfaceType, new ObjectInstance(instance));
        }


        public void RegisterFactorySingleTone<TInterface>(Func<TInterface> factory)
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);

            RegisterInterface(interfaceType, new ObjectInstanceLazy<TInterface>(factory));
        }

        public void RegisterFactoryPerCall<TInterface>(Func<TInterface> factory)
        {
            _selfTested = false;

            var interfaceType = typeof (TInterface);

            RegisterInterface(interfaceType, new ObjectInstancePerCall<TInterface>(factory));
        }


        public void SelfBond<TInterface, TInstance>() 
            where TInstance : TInterface
        {
            _selfTested = false;

            var interfaceType = typeof(TInterface);

            RegisterInterface(interfaceType, new SelfBondInstance(this, typeof(TInstance)));
        }

        /// <summary>
        /// Проверяет есть зарегистрированный объект, который определенного типа или реализует определенный интерфейс.
        /// В случае с интерфейсом, если найдено два объекта, которые реализуют данный интерфейс, считается что нет объекта, реализующего данный интерфейс
        /// </summary>
        /// <param name="type">тип или интерфейс</param>
        /// <returns>Да, если объект существует</returns>
        private bool HasType(Type type)
        {
            return (_interfaces.ContainsKey(type));
        }


        /// <summary>
        /// Запукается всегда после регистрации, и проверяет наличие всех зарегистрированных типов и наличие всех типов в конструкторах
        /// </summary>
        private void SelfTest()
        {
            foreach (var resolverList in _interfaces.Values)

                foreach (var resolver in resolverList)
                {

                    var resolverWithDependencies = resolver as IObjectResolverWithDependencies;

                    if (resolverWithDependencies == null)
                        continue;

                    foreach (var parameterType in resolverWithDependencies.Dependencies)
                    {
                        if (!HasType(parameterType))
                            throw new IocException("[" + resolverWithDependencies.ObjectType+ "] has the constructor parameter [" + parameterType + "] which is not found in the IoC container");
                    }


            }
            _selfTested = true;
        }


        public object GetObject(Type type)
        {
            if (!_selfTested)
                SelfTest();

            if (!_interfaces.ContainsKey(type))
                throw new IocException("There is no object instance for type " + type);


            var interfaces = _interfaces[type];
            if (interfaces.Count>1)
                throw new IocException($"Instance for the type [{type}] is requested as a single one, but containter has {_interfaces[type].Count} of them");

            return interfaces[0].GetInstance();
        }


        public T GetObject<T>() where T : class
        {
            return (T) GetObject(typeof (T));
        }

        private readonly object[] _null = new object[0];


        public object[] GetObjects(Type type)
        {
            if (!_selfTested)
                SelfTest();

            return !_interfaces.ContainsKey(type) ? _null : _interfaces[type].Select(itm => itm.GetInstance()).ToArray();
        }

        public IEnumerable<T> GetObjects<T>()
        {
            return GetObjects(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Invoke a method at the all instances of the interface
        /// </summary>
        /// <typeparam name="T">interface</typeparam>
        /// <param name="action">Action to do</param>
        public void InvokeAllInterfaces<T>(Action<T> action)
        {
            var t = typeof (T);

            if (!_interfaces.ContainsKey(t))
                return;

            foreach (var itm in _interfaces[t])
                action((T) itm.GetInstance());
        }

    }




}
