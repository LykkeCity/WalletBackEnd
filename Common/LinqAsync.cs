using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class LinqAsync
    {



        public static async Task<IEnumerable<TO>> SelectAsync<TI, TO>(this IEnumerable<Task<TI>> src, Func<TI, TO> convert)
        {
            var result = new List<TO>();

            await Task.WhenAll(
                src.Select(itm => itm.ContinueWith(task =>
                {
                    lock (result)
                        result.Add(convert(task.Result));
                }))
                );

            return result;
        }

        public static async Task<IEnumerable<TO>> SelectAsync<TI, TO>(this IEnumerable<TI> src, Func<TI, Task<TO>> convert)
        {
            var result = new List<TO>();
            await Task.WhenAll(
                src.Select(itm => convert(itm).ContinueWith(task =>
                {
                    lock (result)
                        result.Add(task.Result);
                }))
                );

            return result;
        }
    }
}
