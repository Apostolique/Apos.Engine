using System;

namespace Apos.Engine
{
    /// <summary>An interface to implement if you need a reset method when the object is free'd. NOT REQUIRED</summary>
    public interface IPoolable
    {
        /// <summary>Auto-called when <see cref="Pool{T}.Free(T)"/> is called on this object</summary>
        void Reset();
    }

    /// <summary>An object pool of <typeparamref name="T"/>. See <see cref="IPoolable"/></summary>
    public static class Pool<T> where T : class, new()
    {
        const int _defCap = 4;

        /// <summary>Returns the amount of free <typeparamref name="T"/> objects</summary>
        public static int Count { get; private set; }

        static T[] _arr = new T[0];

        /// <summary>Ensures there is <paramref name="size"/> amount of free <typeparamref name="T"/> objects</summary>
        public static void EnsureSize(int size)
        {
            if (Count < size)
                Expand(size - Count);
        }
        /// <summary>Expands the stored amount of free <typeparamref name="T"/> objects by <paramref name="amount"/></summary>
        public static void Expand(int amount)
        {
            ExpandArr(amount);
            for (var i = 0; i < amount; i++)
                _arr[Count++] = new T();
        }

        /// <summary>Returns a free instance of <typeparamref name="T"/> and auto-expands if there's none available</summary>
        public static T Spawn()
        {
            if (Count == 0)
                Expand(_defCap);
            T item = _arr[--Count];
            _arr[Count] = default;
            return item;
        }
        /// <summary>Frees <paramref name="obj"/> for use when <see cref="Spawn"/> is called
        /// Call <see cref="Free(T)"/> on <paramref name="obj"/> when you're done with it</summary>
        public static void Free(T obj)
        {
            if (obj is IPoolable p)
                p.Reset();
            if (Count == _arr.Length)
                ExpandArr(_defCap);
            _arr[Count++] = obj;
        }

        static void ExpandArr(int amount)
        {
            var newArr = new T[_arr.Length + amount];
            Array.Copy(_arr, 0, newArr, 0, Count);
            _arr = newArr;
        }
    }
}