using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace ConcurrentUtil
{
    public class ConcurrentDict<TKey, TValue>
    {
        private Dictionary<TKey, TValue> dict;
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return dict.GetEnumerator();
        }
        public TValue this[TKey key]
        {
            get { lock (this) { return dict[key]; } }
            set { lock (this) { 
                    //UnityEngine.Debug.Log("UPDATING CDICT VAL: " + key + " = " + value);
                    dict[key] = value; 
                } }
        }
        public int Count
        {
            get { lock (this) { return dict.Count; } }
        }
        public bool ContainsKey(TKey key) { lock (this) { return dict.ContainsKey(key); } }
        public TValue GetValueOrDefault(TKey key)
        {
            TValue ret;
            // Ignore return value
            dict.TryGetValue(key, out ret);
            return ret;
        }
        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            TValue ret;
            // Ignore return value
            if (dict.TryGetValue(key, out ret))
                return ret;
            else
                return defaultValue;
        }
        public ConcurrentDict()
        {
            dict = new Dictionary<TKey, TValue>();
        }

        public Dictionary<TKey,TValue>.KeyCollection Keys()
        {
            return dict.Keys;
        }
    }


}