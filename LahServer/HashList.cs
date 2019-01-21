using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer
{
	public class HashList<T> : IList<T>, ISet<T>
	{
		private const int DefaultCapacity = 32;

		private readonly object _editLock = new object();
		private readonly List<T> _list;
		private readonly HashSet<T> _hashSet;

		public HashList() : this(DefaultCapacity)
		{
		}

		public HashList(int capacity)
		{
			_list = new List<T>(capacity);
			_hashSet = new HashSet<T>(capacity);
		}

		public T this[int index]
		{
			get => _list[index];
			set
			{
				lock (_editLock)
				{
					if (_hashSet.Contains(value)) return;
					_hashSet.Remove(_list[index]);
					_hashSet.Add(value);
					_list[index] = value;
				}
			}
		}

		public int Count => _list.Count;

		public bool IsReadOnly => false;

		public bool Add(T item)
		{
			lock(_editLock)
			{
				if (!_hashSet.Add(item)) return false;
				_list.Add(item);
				return true;
			}
		}

		public void AddRange(IEnumerable<T> items)
		{
			lock(_editLock)
			{
				foreach (var item in items)
				{
					Add(item);
				}
			}
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			lock(_editLock)
			{
				foreach(var item in items)
				{
					Remove(item);
				}
			}
		}

		public bool Swap(int indexA, int indexB)
		{
			lock(_editLock)
			{
				if (indexA < 0 || indexA >= Count || indexB < 0 || indexB >= Count) return false;
				var temp = _list[indexA];
				_list[indexA] = _list[indexB];
				_list[indexB] = temp;
				return true;
			}
		}

		public void Clear()
		{
			lock(_editLock)
			{
				_hashSet.Clear();
				_list.Clear();
			}
		}

		public void MoveTo(HashList<T> target)
		{
			lock(_editLock)
			{
				target.AddRange(this);
				Clear();
			}
		}

		public bool Contains(T item)
		{
			return _hashSet.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			lock(_editLock)
			{
				_hashSet.ExceptWith(other);
				_list.RemoveAll(t => !_hashSet.Contains(t));
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return _list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			lock(_editLock)
			{
				if (_hashSet.Contains(item)) return;
				_hashSet.Add(item);
				_list.Insert(index, item);
			}
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			lock(_editLock)
			{
				_hashSet.IntersectWith(other);
				_list.RemoveAll(t => !_hashSet.Contains(t));
			}
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			return _hashSet.IsProperSubsetOf(other);
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			return _hashSet.IsProperSupersetOf(other);
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			return _hashSet.IsSubsetOf(other);
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			return _hashSet.IsSupersetOf(other);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			return _hashSet.Overlaps(other);
		}

		public bool Remove(T item)
		{
			lock(_editLock)
			{
				if (!_hashSet.Remove(item)) return false;
				_list.Remove(item);
				return true;
			}
		}

		public void RemoveAt(int index)
		{
			lock(_editLock)
			{
				var t = _list[index];
				_hashSet.Remove(t);
				_list.RemoveAt(index);
			}
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			return _hashSet.SetEquals(other);
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			lock(_editLock)
			{
				_hashSet.SymmetricExceptWith(other);
				_list.RemoveAll(t => !_hashSet.Contains(t));
				int c = _list.Count;
				foreach (var item in _hashSet)
				{
					if (!_list.Contains(item))
					{
						_list.Add(item);
					}
				}
			}
		}

		public void UnionWith(IEnumerable<T> other)
		{
			lock(_editLock)
			{
				_hashSet.UnionWith(other);
				foreach(var item in _hashSet)
				{
					if (!_list.Contains(item))
					{
						_list.Add(item);
					}
				}
			}
		}

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _list.GetEnumerator();
		}
	}
}
