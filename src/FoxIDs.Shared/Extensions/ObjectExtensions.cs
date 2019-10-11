using System;
using System.Threading.Tasks;

namespace FoxIDs
{
    public static class ObjectExtensions
    {
        public static T Set<T>(this T obj, Action<T> setAction)
        {
            setAction(obj);
            return obj;
        }

        public static async Task<T> SetAsync<T>(this T obj, Func<T, Task> setActionAsync) 
        {
            await setActionAsync(obj);
            return obj;
        }
    }
}
