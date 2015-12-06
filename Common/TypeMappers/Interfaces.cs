using System.Collections;
using System.Collections.Generic;

namespace Common.TypeMappers
{
    public interface IListReadOnly : IEnumerable
    {
        int Count { get; }
    }

    public interface IListReadOnly<out T> : IListReadOnly, IEnumerable<T>
    {
        
    }
}
