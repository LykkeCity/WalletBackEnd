using System;
using System.Collections;
using System.Reflection;

namespace Common.TypeMappers
{
    public abstract class SourceArrayList : IListReadOnly
    {
        public static SourceArrayList Create(object src)
        {
            if (src is IListReadOnly)
                return new SourceArrayListReadOnly(src as IListReadOnly);

            if (src is IList)
                return new SourceArrayIList(src as IList);

            return null;

        }

        public abstract IEnumerator GetEnumerator();

        public abstract int Count { get; }
    }

    public class SourceArrayIList : SourceArrayList
    {
        private readonly IList _list;

        public SourceArrayIList(IList list)
        {
            _list = list;
        }

        public override IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public override int Count
        {
            get { return _list.Count; }
        }
    }

    public class SourceArrayListReadOnly : SourceArrayList
    {
        private readonly IListReadOnly _list;

        public SourceArrayListReadOnly(IListReadOnly list)
        {
            _list = list;
        }

        public override IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public override int Count
        {
            get { return _list.Count; }
        }
    }

    public abstract class ArrayConstructorBase
    {
        public abstract IList Invoke(IListReadOnly src);
        public abstract void SynchRecordsCount(IListReadOnly src, IList dest);

        public static ArrayConstructorBase Create(Type destListType)
        {
            var elementType = ReflectionUtils.GetElementType(destListType);
            var elementCtor = ReflectionUtils.FindDefaultConstructor(elementType);

            if (destListType.BaseType == (typeof (Array)))
                return new ArrayConstructor(destListType, elementCtor);

            return new ListConstructor(destListType, elementCtor);

        }

        public abstract bool CahBeSynched { get; }

    }

    public class ArrayConstructor : ArrayConstructorBase
    {
        private readonly ConstructorInfo _destElementCtor;
        private readonly Type _destElementType;

        public ArrayConstructor(Type destListType, ConstructorInfo destElementCtor)
        {
            _destElementCtor = destElementCtor;

            _destElementType = ReflectionUtils.GetElementType(destListType);
        }


        public override IList Invoke(IListReadOnly src)
        {
            var result = (IList)Array.CreateInstance(_destElementType, src.Count);

            for (int i = 0; i < result.Count; i++)
                result[i] = _destElementCtor.Invoke(null);
            return result;
        }

        public override void SynchRecordsCount(IListReadOnly src, IList dest)
        {
            throw new Exception("Array can not be synched");
        }

        public override bool CahBeSynched
        {
            get { return false; }
        }


    }

    public class ListConstructor : ArrayConstructorBase
    {
        private readonly ConstructorInfo _destElementCtor;
        private readonly ConstructorInfo _destListConstructor;

        public ListConstructor(Type destListType, ConstructorInfo destElementCtor)
        {
            _destElementCtor = destElementCtor;
            _destListConstructor = ReflectionUtils.FindDefaultConstructor(destListType);

            if (_destListConstructor == null)
                throw new ExceptionOfMapping("Error mapping property. List=[" + destListType + "]. It must have the default constructor.");

        }

        public override IList Invoke(IListReadOnly src)
        {
            var result = (IList)_destListConstructor.Invoke(null);
            SynchRecordsCount(src, result);
            return result;
        }

        public override void SynchRecordsCount(IListReadOnly src, IList dest)
        {
            while (src.Count != dest.Count)
            {
                if (dest.Count < src.Count)
                    dest.Add(_destElementCtor.Invoke(null));
                else
                    dest.RemoveAt(dest.Count-1);
            }
        }

        public override bool CahBeSynched
        {
            get { return true; }
        }
    }
}
