using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Common
{

    public class PropNotClone : Attribute
    {
        
    }

    public class CopyInstance : Attribute
    {
        
        
    }

    public interface IClonable
    {
        object Clone();
    }

    public class PropertyMap
    {
        public PropertyMap(PropertyInfo src, PropertyInfo clone)
        {
            Clone = clone;
            Src = src;
        }

        public PropertyInfo Src { get; private set; }
        public PropertyInfo Clone { get; private set; }
    }

    public class CloneItem
    {
        private readonly Dictionary<string, PropertyMap> _maps = new Dictionary<string, PropertyMap>();
    
        public CloneItem(Type src, Type cloneType)
        {
            Constructor = ReflectionUtils.FindDefaultConstructor(cloneType);
            if (Constructor == null)
                throw new Exception("Default constructor not found");

            foreach (var propertyInfo in cloneType.GetProperties())
            {
                var piSrc = ReflectionUtils.FindPropertyByName(src, propertyInfo.Name);
                _maps.Add(propertyInfo.Name, new PropertyMap(piSrc, propertyInfo));
            }

        }

        public ConstructorInfo Constructor { get; private set; }

        public object GetInstance(object src)
        {
            var result = Constructor.Invoke(null);

            foreach (var map in _maps.Values)
            {
                var value = map.Src.GetValue(src, null);

                var cln = value as IClonable;
                map.Clone.SetValue(result, cln == null ? value : cln.Clone(), null);
            }

            return result;

        }
    }


    public class Cloner
    {
        private readonly Dictionary<Type, CloneItem> _items = new Dictionary<Type, CloneItem>();

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        

        public void Register(Type src, Type clone)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _items.Add(src, new CloneItem(src, clone));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
                
            }
        }

        public object Clone(object source)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _items[source.GetType()].GetInstance(source);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

        }

        public static Cloner Instance = new Cloner();
        
    }
}
