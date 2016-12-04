using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace LineTrader.Util
{
    public interface IObservableList<T> : IList<T>, INotifyCollectionChanged { }

    public class ObservableSortedList<K, V> : IObservableList<V>
    {
        private SortedList<K, V> items;
        private Func<V, K> orderBy;

        public ObservableSortedList(Func<V, K> orderBy)
        {
            this.orderBy = orderBy;
            this.items = new SortedList<K, V>();
        }

        public ObservableSortedList(ObservableSortedList<K, V> that)
        {
            this.orderBy = that.orderBy;
            this.items = that.items;
            this.CollectionChanged = that.CollectionChanged;
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public V this[int index]
        {
            get
            {
                return this.items.Values[index];
            }

            set
            {
                this.RemoveAt(index);
                this.Add(value);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int IndexOf(V item)
        {
            var key = this.orderBy(item);
            return this.items.IndexOfKey(key);
        }

        public void Insert(int index, V item)
        {
            this.Add(item);
        }

        public void RemoveAt(int index)
        {
            var item = this.items.Values[index];
            this.items.RemoveAt(index);
            NotifyRemoved(index, item);
        }

        public void RemoveWithoutNotifyAt(int index)
        {
            this.items.RemoveAt(index);
        }

        public void Add(V item)
        {
            var key = this.orderBy(item);
            this.items.Add(key, item);
            var index = this.items.IndexOfKey(key);
            NotifyAdded(index, item);
        }

        public void AddWithoutNotify(V item)
        {
            var key = this.orderBy(item);
            this.items.Add(key, item);
        }

        public void Clear()
        {
            this.items = new SortedList<K, V>();
            NotifyReset();
        }

        public bool Contains(V item)
        {
            var key = this.orderBy(item);
            return this.items.ContainsKey(key);
        }

        public void CopyTo(V[] array, int arrayIndex)
        {
            this.items.Values.CopyTo(array, arrayIndex);
        }

        public bool Remove(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            if (index < 0)
            {
                return false;
            }
            this.RemoveAt(index);
            return true;
        }

        public bool RemoveWithoutNotify(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            if (index < 0)
            {
                return false;
            }
            this.RemoveWithoutNotifyAt(index);
            return true;
        }

        public IEnumerator<V> GetEnumerator()
        {
            return this.items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.Values.GetEnumerator();
        }

        public void Set(IEnumerable<V> items)
        {
            SetWithoutNotify(items);
            NotifyReset();
        }

        public void SetWithoutNotify(IEnumerable<V> items)
        {
            this.items = new SortedList<K, V>();
            foreach (var x in items.OrderBy(x => this.orderBy(x)))
            {
                var key = this.orderBy(x);
                this.items.Add(key, x);
            }
        }

        public void NotifyAdded(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            this.NotifyAdded(index, item);
        }

        public void NotifyAdded(int index, V item)
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void NotifyRemoved(V item)
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        public void NotifyRemoved(int index, V item)
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void NotifyReplaced(int index, V item)
        {
            this.NotifyReplaced(index, item, item);
        }

        public void NotifyReplaced(int index, V old, V item)
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, old, item, index));
        }

        public void NotifyReplaced(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index));
        }

        public void NotifyReset()
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerable<V> Head(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            for (var i = index - 1; i >= 0; i --)
            {
                yield return this.items.Values[i];
            }
        }

        public IEnumerable<V> Tail(V item)
        {
            var key = this.orderBy(item);
            var index = this.items.IndexOfKey(key);
            for (var i = index + 1; i < this.Count; i++)
            {
                yield return this.items.Values[i];
            }
        }
    }
}
