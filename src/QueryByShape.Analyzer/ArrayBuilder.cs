using System;

namespace QueryByShape.Analyzer
{
    internal class ArrayBuilder<T>
    {
        private const int DefaultCapacity = 8;
        private T[] _items;

        public ArrayBuilder(int capacity = DefaultCapacity)
        {
            _items = new T[capacity];
        }

        public int Count { get; set; }

        private void EnsureCapacity(int additional)
        {
            int requiredCapacity = this.Count + additional;
            if (requiredCapacity <= _items.Length)
            {
                return;
            }

            int newCapacity = Math.Max(_items.Length * 2, requiredCapacity);
            Array.Resize(ref _items, newCapacity);
        }

        public void Add(T item)
        {
            EnsureCapacity(1);
            _items[this.Count++] = item;
        }

        public void AddRange(T[] items)
        {
            EnsureCapacity(items.Length);

            int offset = this.Count;
            this.Count += items.Length;

            Array.Copy(items, 0, _items, offset, items.Length);
        }


        public T[] ToArray()
        {
            if (this.Count == 0)
            {
                return Array.Empty<T>();
            }

            T[] result = new T[this.Count];
            Array.Copy(_items, result, this.Count);
            return result;
        }

        public void Clear()
        {
            this.Count = 0;
        }
    }
}
