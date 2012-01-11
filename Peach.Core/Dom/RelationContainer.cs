﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@phed.org)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Reflection;
using System.Runtime.Serialization;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Abstract base class for DataElements that contain other
	/// data elements.  Such as Block, Choice, or Flags.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class RelationContainer : IEnumerable<Relation>, IList<Relation>
	{
		protected DataElement parent;
		protected List<Relation> _childrenList = new List<Relation>();
		protected Dictionary<Type, Relation> _childrenDict = new Dictionary<Type, Relation>();

		public RelationContainer(DataElement parent)
		{
			this.parent = parent;
		}

		public Relation this[int index]
		{
			get { return _childrenList[index]; }
			set
			{
				if (value == null)
					throw new ApplicationException("Cannot set null value");

				_childrenDict.Remove(_childrenList[index].GetType());
				_childrenDict.Add(value.GetType(), value);

				_childrenList[index].parent = null;

				_childrenList.RemoveAt(index);
				_childrenList.Insert(index, value);

				value.parent = parent;
			}
		}

		public Relation this[Type key]
		{
			get { return _childrenDict[key]; }
			set
			{
				if (value == null)
					throw new ApplicationException("Cannot set null value");

				int index = _childrenList.IndexOf(_childrenDict[key]);
				_childrenList.RemoveAt(index);
				_childrenDict[key].parent = null;
				_childrenDict[key] = value;
				_childrenList.Insert(index, value);

				value.parent = parent;
			}
		}

		public bool hasOfSizeRelation
		{
			get
			{
				foreach (Relation rel in _childrenList)
					if (rel is SizeRelation && rel.Of == parent)
						return true;

				return false;
			}
		}

		public bool hasOfOffsetRelation
		{
			get
			{
				foreach (Relation rel in _childrenList)
					if (rel is OffsetRelation && rel.Of == parent)
						return true;

				return false;
			}
		}

		public bool hasOfCountRelation
		{
			get
			{
				foreach (Relation rel in _childrenList)
					if (rel is CountRelation && rel.Of == parent)
						return true;

				return false;
			}
		}

		public SizeRelation getSizeRelation()
		{
			foreach (Relation rel in _childrenList)
			{
				if (rel is SizeRelation && rel.Of == parent)
					return rel as SizeRelation;
			}

			return null;
		}

		public CountRelation getCountRelation()
		{
			foreach (Relation rel in _childrenList)
			{
				if (rel is CountRelation && rel.Of == parent)
					return rel as CountRelation;
			}

			return null;
		}

		public OffsetRelation getOffsetRelation()
		{
			foreach (Relation rel in _childrenList)
			{
				if (rel is OffsetRelation && rel.Of == parent)
					return rel as OffsetRelation;
			}

			return null;
		}

		#region IEnumerable<Relation> Members

		public IEnumerator<Relation> GetEnumerator()
		{
			return _childrenList.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _childrenList.GetEnumerator();
		}

		#endregion

		#region IList<Relation> Members

		public int IndexOf(Relation item)
		{
			return _childrenList.IndexOf(item);
		}

		protected bool HaveKey(Type key)
		{
			foreach (Type k in _childrenDict.Keys)
				if (k == key)
					return true;

			return false;
		}

		public void Insert(int index, Relation item)
		{
			if (HaveKey(item.GetType()))
				throw new ApplicationException(
					string.Format("Child Relation typed {0} already exists.", item.GetType()));

			_childrenList.Insert(index, item);
			_childrenDict[item.GetType()] = item;

			item.parent = parent;
		}

		public void RemoveAt(int index)
		{
			_childrenDict.Remove(_childrenList[index].GetType());
			_childrenList[index].parent = null;
			_childrenList.RemoveAt(index);
		}

		#endregion

		#region ICollection<Relation> Members

		public void Add(Relation item)
		{
			foreach (Type k in _childrenDict.Keys)
				if (k == item.GetType())
					throw new ApplicationException(
						string.Format("Child Relation typed {0} already exists.", item.GetType()));

			_childrenList.Add(item);
			_childrenDict[item.GetType()] = item;
			item.parent = parent;
		}

		public void Clear()
		{
			foreach (Relation e in _childrenList)
				e.parent = null;

			_childrenList.Clear();
			_childrenDict.Clear();
		}

		public bool Contains(Relation item)
		{
			return _childrenList.Contains(item);
		}

		public void CopyTo(Relation[] array, int arrayIndex)
		{
			_childrenList.CopyTo(array, arrayIndex);
			foreach (Relation e in array)
			{
				_childrenDict[e.GetType()] = e;
				e.parent = parent;
			}
		}

		public int Count
		{
			get { return _childrenList.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Relation item)
		{
			_childrenDict.Remove(item.GetType());
			bool ret = _childrenList.Remove(item);
			item.parent = null;

			return ret;
		}

		#endregion
	}

}

// end