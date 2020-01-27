using System;

namespace Apos.Engine
{
    /// <summary>This is an interface to implement if you need a reset method when the object is free'd. NOT REQUIRED</summary>
    public interface IPoolable
    {
        /// <summary>This will auto-call when <see cref="Pool{T}.Free(T)"/> is called on this object</summary>
        void Reset();
    }

    /// <summary>An object pool of <typeparamref name="T"/>. See <see cref="IPoolable"/></summary>
    public static class Pool<T> where T : class, new()
    {
        const int _defaultCapacity = 4;

        /// <summary>Returns the amount of free <typeparamref name="T"/> objects</summary>
        public static int Count { get; private set; }

        static T[] _arr = new T[0];

        /// <summary>This will ensure there is <paramref name="size"/> amount of free <typeparamref name="T"/> objects</summary>
        public static void EnsureSize(int size)
        {
            if (Count < size)
                Expand(size - Count);
        }
        /// <summary>This will expand the stored amount of free <typeparamref name="T"/> objects by <paramref name="amount"/></summary>
        public static void Expand(int amount)
        {
            for (int i = 0; i < amount; i++)
                Free(new T());
        }

        /// <summary>Returns a free instance of <typeparamref name="T"/> and auto-expand if there's none available</summary>
        public static T Spawn()
        {
            if (Count == 0)
                Expand(_defaultCapacity);
            T item = _arr[--Count];
            _arr[Count] = default;
            return item;
        }
        /// <summary>This will free the <paramref name="obj"/> object for use when <see cref="Spawn"/> is called (on <typeparamref name="T"/>)</summary>
        public static void Free(T obj)
        {
            if (obj is IPoolable p)
                p.Reset();
            if (Count == _arr.Length)
            {
                T[] newArr = new T[_arr.Length + _defaultCapacity];
                Array.Copy(_arr, 0, newArr, 0, Count);
                _arr = newArr;
            }
            _arr[Count++] = obj;
        }
    }
}