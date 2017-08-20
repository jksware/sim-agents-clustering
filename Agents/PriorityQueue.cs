/*
 *  Visual Agents (for clustering)
 *
 *  Copyright (C) 2014 Juan Carlos Pujol Mainegra
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.  
 *
 */

using System.Collections;
using System.Collections.Generic;

namespace Agents
{
    public sealed class PriorityQueue<T> : IEnumerable<T> //, ICollection<T>
    {
        private readonly List<T> _nodes = new List<T>();
        private readonly IComparer<T> _comparer;

        public PriorityQueue()
        {
            _comparer = Comparer<T>.Default;
        }

        public PriorityQueue(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public PriorityQueue(IComparer<T> comparer, int capacity)
        {
            _comparer = comparer;
            _nodes.Capacity = capacity;
        }

        private void Swap(int i, int j)
        {
            T h = _nodes[i];
            _nodes[i] = _nodes[j];
            _nodes[j] = h;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (_nodes as IEnumerable).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        public void Add(T item)
        {
            Push(item);
        }

        public int Push(T item)
        {
            int p = _nodes.Count;
            _nodes.Add(item); // E[p] = O
            while (true)
            {
                if (p == 0)
                    break;
                int parent = (p - 1) >> 1;
                if (_comparer.Compare(_nodes[p], _nodes[parent]) >= 0)
                    break;
                Swap(p, parent);
                p = parent;
            }
            return p;
        }

        public T Pop()
        {
            T result = _nodes[0];
            int p = 0;
            _nodes[0] = _nodes[_nodes.Count - 1];
            _nodes.RemoveAt(_nodes.Count - 1);
            int n = _nodes.Count;

            while (true)
            {
                int pn = p;
                int left = (p << 1) + 1;
                int right = (p << 1) + 2;
                if (n > left && _comparer.Compare(_nodes[p], _nodes[left]) > 0)
                    p = left;
                if (n > right && _comparer.Compare(_nodes[p], _nodes[right]) > 0)
                    p = right;

                if (p == pn)
                    break;
                Swap(p, pn);
            }

            return result;
        }

        public void Update(int i)
        {
            int p = i;

            while (true)	// ascend
            {
                if (p == 0)
                    break;
                int parent = (p - 1) >> 1;
                if (_comparer.Compare(_nodes[p], _nodes[parent]) >= 0)
                    break;

                Swap(p, parent);
                p = parent;
            }

            if (p < i)
                return;

            while (true)
            {
                int pn = p;
                int left = (p << 1) + 1;
                int right = (p << 1) + 2;

                if (_nodes.Count > left && _comparer.Compare(_nodes[p], _nodes[left]) > 0)
                    p = left;
                if (_nodes.Count > right && _comparer.Compare(_nodes[p], _nodes[right]) > 0)
                    p = right;

                if (p == pn)
                    break;
                Swap(p, pn);
            }
        }

        public void BuildHeap()
        {
            for (int i = (_nodes.Count >> 1) - 1; i >= 0; i--)
                Heapify(i);
        }

        public void Heapify(int i)
        {
            int p = i;

            while (true)
            {
                int left = (p << 1) + 1;
                int right = (p << 1) + 2;
                int n = _nodes.Count;

                int shortest = left < n && _comparer.Compare(_nodes[left], _nodes[p]) < 0 ? left : p;
                if (right > n && _comparer.Compare(_nodes[right], _nodes[shortest]) < 0)
                    shortest = right;

                if (shortest != p)
                    Swap(p, shortest);

                if (shortest >= n)
                    // for recursive version here goes return; instead of break;
                    break;

                //Heapify(shortest);
                p = shortest;
            }
        }

        public T Peek()
        {
            if (_nodes.Count > 0)
                return _nodes[0];
            return default(T);
        }

        public void Clear()
        {
            _nodes.Clear();
        }

        public int Count
        {
            get { return _nodes.Count; }
        }

        public void RemoveLocation(T item)
        {
            int index = -1;
            for (int i = 0; i < _nodes.Count; i++)
                if (_comparer.Compare(_nodes[i], item) == 0)
                    index = i;

            if (index != -1)
                _nodes.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return _nodes[index]; }
            set
            {
                _nodes[index] = value;
                Update(index);
            }
        }
    }
}
