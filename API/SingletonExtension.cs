using System.Reflection;

namespace Raldi
{
    public static class SingletonExtension
    {
        public static bool TryGetSingleton<T>(out T instance) where T : class
        {
            instance = null;

            try
            {
                var type = typeof(T);
                var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (instanceProperty != null && instanceProperty.CanRead)
                {
                    instance = instanceProperty.GetValue(null) as T;
                    return instance != null;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}