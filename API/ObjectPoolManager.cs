using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Raldi
{
    public static class ObjectPoolManager
    {
        private static readonly Dictionary<Type, UnityEngine.Object> _Objects = new Dictionary<Type, UnityEngine.Object>();
        private static readonly Dictionary<Type, float> _Timestamps = new Dictionary<Type, float>();

        private static readonly Dictionary<Type, List<UnityEngine.Object>> _ObjectLists = new Dictionary<Type, List<UnityEngine.Object>>();
        private static readonly Dictionary<Type, float> _ListTimestamps = new Dictionary<Type, float>();

        private const float CACHE_DURATION = 5f;
        private static float removeNullTimer;

        public static T Find<T>() where T : UnityEngine.Object
        {
            Type type = typeof(T);
            float currentTime = Time.time;

            if (currentTime - removeNullTimer >= CACHE_DURATION)
            {
                ValidateCache();
                removeNullTimer = currentTime;
            }

            if (_Objects.ContainsKey(type) && _Objects[type] != null && _Timestamps.ContainsKey(type) && currentTime - _Timestamps[type] < CACHE_DURATION)
            {
                return (T)_Objects[type];
            }

            T obj = UnityEngine.Object.FindObjectOfType<T>();

            if (obj != null)
            {
                _Objects[type] = obj;
                _Timestamps[type] = currentTime;
            }
            else
            {
                _Objects.Remove(type);
                _Timestamps.Remove(type);
            }

            return obj;
        }

        public static T FindWithRefresh<T>() where T : UnityEngine.Object
        {
            Type type = typeof(T);
            T obj = UnityEngine.Object.FindObjectOfType<T>();
            _Objects[type] = obj;
            _Timestamps[type] = Time.time;
            return obj;
        }

        public static List<T> FindAll<T>() where T : UnityEngine.Object
        {
            Type type = typeof(T);
            float currentTime = Time.time;

            if (currentTime - removeNullTimer >= CACHE_DURATION)
            {
                ValidateCache();
                removeNullTimer = currentTime;
            }

            if (_ObjectLists.ContainsKey(type) &&
                _ListTimestamps.ContainsKey(type) &&
                currentTime - _ListTimestamps[type] < CACHE_DURATION)
            {
                return _ObjectLists[type].Where(obj => obj != null).Cast<T>().ToList();
            }

            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            List<UnityEngine.Object> objectList = objects.Cast<UnityEngine.Object>().ToList();

            _ObjectLists[type] = objectList;
            _ListTimestamps[type] = currentTime;

            return objects.ToList();
        }

        public static List<T> FindAllWithRefresh<T>() where T : UnityEngine.Object
        {
            Type type = typeof(T);
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            List<UnityEngine.Object> objectList = objects.Cast<UnityEngine.Object>().ToList();

            _ObjectLists[type] = objectList;
            _ListTimestamps[type] = Time.time;

            return objects.ToList();
        }

        public static T FindRandom<T>() where T : UnityEngine.Object
        {
            var all = FindAll<T>();
            if (all.Count == 0) return null;
            return all[UnityEngine.Random.Range(0, all.Count)];
        }

        public static T FindClosest<T>(Vector3 position) where T : Component
        {
            var all = FindAll<T>();
            if (all.Count == 0) return null;

            T closest = all[0];
            Vector3 closestPosition = closest.transform.position;
            float closestSqrDistance = (position - closestPosition).sqrMagnitude;

            for (int i = 1; i < all.Count; i++)
            {
                Vector3 currentPosition = all[i].transform.position;
                float sqrDistance = (position - currentPosition).sqrMagnitude;

                if (sqrDistance < closestSqrDistance)
                {
                    closest = all[i];
                    closestPosition = currentPosition;
                    closestSqrDistance = sqrDistance;
                }
            }

            return closest;
        }

        public static void ClearCache()
        {
            _Objects.Clear();
            _Timestamps.Clear();
            _ObjectLists.Clear();
            _ListTimestamps.Clear();
        }

        public static void ValidateCache()
        {
            List<Type> toRemove = new List<Type>();
            foreach (var kvp in _Objects)
            {
                if (kvp.Value == null) toRemove.Add(kvp.Key);
            }
            foreach (Type type in toRemove)
            {
                _Objects.Remove(type);
                _Timestamps.Remove(type);
            }

            toRemove.Clear();
            foreach (var kvp in _ObjectLists)
            {
                kvp.Value.RemoveAll(obj => obj == null);

                if (kvp.Value.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (Type type in toRemove)
            {
                _ObjectLists.Remove(type);
                _ListTimestamps.Remove(type);
            }
        }

        public static void RemoveFromCache<T>(T obj) where T : UnityEngine.Object
        {
            Type type = typeof(T);

            if (_Objects.ContainsKey(type) && _Objects[type] == obj)
            {
                _Objects.Remove(type);
                _Timestamps.Remove(type);
            }

            if (_ObjectLists.ContainsKey(type))
            {
                _ObjectLists[type].Remove(obj);
                if (_ObjectLists[type].Count == 0)
                {
                    _ObjectLists.Remove(type);
                    _ListTimestamps.Remove(type);
                }
            }
        }

        public static void AddToCache<T>(T obj) where T : UnityEngine.Object
        {
            Type type = typeof(T);

            if (!_ObjectLists.ContainsKey(type))
            {
                _ObjectLists[type] = new List<UnityEngine.Object>();
            }

            if (!_ObjectLists[type].Contains(obj))
            {
                _ObjectLists[type].Add(obj);
                _ListTimestamps[type] = Time.time;
            }
        }
    }
}