using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace OpentracingExtension
{
    public static class ReflectionExtension
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo> PropertyDictionary = 
            new ConcurrentDictionary<string, PropertyInfo>();
        public static T GetProperty<T>(this object obj, string propertyName)
        {
            var type = obj.GetType();
            PropertyInfo property = null;
            var key = $"{type.Name}.{propertyName}";
            if(PropertyDictionary.ContainsKey(key))
            {
                property = PropertyDictionary[key];
            }
            else
            {
                lock(PropertyDictionary)
                {
                    if(PropertyDictionary.ContainsKey(key))
                    {
                        property = PropertyDictionary[key];
                    }
                    else
                    {
                        property = type.GetProperty(propertyName);
                        PropertyDictionary.TryAdd(key, property);
                    }
                }
            }

            return (T)property.GetValue(obj);
        }
    }
}
