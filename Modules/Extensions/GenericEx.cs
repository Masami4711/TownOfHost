using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;

namespace TownOfHost.Modules.Extensions
{
    public static class GenericEx
    {
        public static bool IsBetween<T>(this T value, T min, T max)
        where T : IComparable
            => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;

        // Collection
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
            => collection == null || collection.Count == 0;
        public static T GetRandomItem<T>(this IList<T> list)
        {
            if (list.IsNullOrEmpty()) return default;
            int index = IRandom.Instance.Next(list.Count);
            return list[index];
        }
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out TValue value);
            return value;
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            var rnd = IRandom.Instance;
            for (int i = 0; i < list.Count; i++)
            {
                int j = rnd.Next(list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Il2Cpp
        public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
        {
            casted = obj.TryCast<T>();
            return casted != null;
        }
        public static void Shuffle<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var rnd = IRandom.Instance;
            for (int i = 0; i < list.Count; i++)
            {
                int j = rnd.Next(list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}