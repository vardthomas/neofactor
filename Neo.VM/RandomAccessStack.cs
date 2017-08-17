using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.VM
{
    /// <summary>
    ///   <en>
    ///     Serves as a FIFO stack that can also be indexed into.
    ///   </en>
    ///   <zh-CN>
    ///     作为一个也可以编入索引的FIFO堆栈。
    ///   </zh-CN>
    ///   <es>
    ///     Sirve como una pila FIFO que también se puede indexar en.
    ///   </es>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RandomAccessStack<T> : IReadOnlyCollection<T>
    {
        private readonly List<T> list = new List<T>();

        public int Count => list.Count;

        public void Clear()
        {
            list.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Insert(int index, T item)
        {
            if (index > list.Count) throw new InvalidOperationException();
            list.Insert(list.Count - index, item);
        }

        public T Peek(int index = 0)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            return list[list.Count - 1 - index];
        }

        public T Pop()
        {
            return Remove(0);
        }

        public void Push(T item)
        {
            list.Add(item);
        }

        public T Remove(int index)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            T item = list[list.Count - index - 1];
            list.RemoveAt(list.Count - index - 1);
            return item;
        }

        public void Set(int index, T item)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            list[list.Count - index - 1] = item;
        }
    }
}
