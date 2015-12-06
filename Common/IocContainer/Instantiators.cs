using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.IocContainer
{

    /// <summary>
    /// Interface, which resolves the object
    /// </summary>
    public interface IObjectResolver
    {
        // Метод создания объектов
        object GetInstance();

    }

    /// <summary>
    /// Interface, which resolves the object and has dependencies
    /// </summary>
    public interface IObjectResolverWithDependencies : IObjectResolver
    {
        /// <summary>
        /// parameter types. Even if one of parameter is Array, here is gonna be a basic type
        /// </summary>
        IEnumerable<Type> Dependencies { get; }

        Type ObjectType { get; }
    }


    public abstract class ObjectInstantiatorBase : IObjectResolverWithDependencies
    {
        internal readonly IoC IoC;

        private ConstructorInfo _theConstructor;
        internal ParameterInfo[] ConstructorParameters;
        public abstract object GetInstance();

        public IEnumerable<Type> Dependencies { get; private set; }
        public Type ObjectType { get; }


        private void PopulateConstructor()
        {
            _theConstructor = ObjectType.FindIocConstructor();
            ConstructorParameters = _theConstructor.GetParameters();
        }


        private void PopulateParamteterTypes()
        {
            var result = ConstructorParameters.Select(IocUtils.GetIocRealType).ToArray();
            Dependencies = result;
        }

        protected ObjectInstantiatorBase(Type objType, IoC ioC)
        {
            ObjectType = objType;
            IoC = ioC;
            PopulateConstructor();
            PopulateParamteterTypes();

        }


        protected object CreateInstance()
        {

            if (ConstructorParameters.Length == 0)
                return _theConstructor.Invoke(null);

            var parameters = IoC.CreateConstructorParamtersInstances(ConstructorParameters);

            return _theConstructor.Invoke(parameters);
        }

    }


    public class ObjectInstantiatorSingleTone : ObjectInstantiatorBase
    {

        private object _instance;

        public ObjectInstantiatorSingleTone(Type objType, IoC ioC) : base(objType, ioC)
        {
        }

        public override object GetInstance()
        {
            return _instance ?? (_instance = CreateInstance());
        }
    }


    public class ObjectInstantiatorPerCall : ObjectInstantiatorBase
    {
        public ObjectInstantiatorPerCall(Type objType, IoC ioC) : base(objType, ioC)
        {
        }

        public override object GetInstance()
        {
            return CreateInstance();
        }
    }


    public class ObjectInstanceLazy<T> : IObjectResolver
    {
        private readonly Lazy<T> _lazy;

        public ObjectInstanceLazy(Func<T> theInstanceFunc)
        {
            _lazy = new Lazy<T>(theInstanceFunc);
        }

        public object GetInstance()
        {
            return _lazy.Value;
        }

    }

    public class ObjectInstancePerCall<T> : IObjectResolver
    {
        private readonly Func<T> _theInstanceFunc;

        public ObjectInstancePerCall(Func<T> theInstanceFunc)
        {
            _theInstanceFunc = theInstanceFunc;
        }

        public object GetInstance()
        {
            return _theInstanceFunc();
        }

    }

    public class ObjectInstance : IObjectResolver
    {
        private readonly object _theInstance;

        public ObjectInstance(object theInstance)
        {
            _theInstance = theInstance;
        }

        public object GetInstance()
        {
            return _theInstance;
        }

    }

    public class SelfBondInstance : IObjectResolver
    {
        private readonly IoC _ioc;
        private readonly Type _bondedType;


        public SelfBondInstance(IoC ioc, Type bondedType)
        {
            _ioc = ioc;
            _bondedType = bondedType;
        }

        public object GetInstance()
        {
            return _ioc.GetObject(_bondedType);
        }
    }
}
