using System;

namespace FoxIDs.Client
{
    public static class MapperExtensions
    {
        public static T Map<T>(this object inObj, Action<T> afterMap = null) 
        {
            var json = inObj.JsonSerialize();
            var outObj = json.JsonDeserialize<T>();
            afterMap?.Invoke(outObj);
            return outObj;
        }
    }
}
