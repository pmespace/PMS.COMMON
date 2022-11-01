using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace COMMON
{
	[ComVisible(true)]
	[Guid("AF2F75A3-E80B-425D-B9B6-061CEDC0D3C1")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISafeList<T>
	{
		#region ISafeList properties
		[DispId(1)]
		List<T> List { get; }
		[DispId(2)]
		int Count { get; }
		[DispId(3)]
		T this[int index] { get; set; }
		#endregion

		#region ISafeList methods
		[DispId(100)]
		void Clear();
		[DispId(101)]
		bool Add(T o);
		[DispId(102)]
		bool Insert(T o, int index = 0);
		[DispId(103)]
		bool Remove(T o);
		[DispId(104)]
		bool RemoveAt(int index);
		[DispId(105)]
		T Get(int i);
		[DispId(106)]
		T[] ToArray();
		#endregion
	}
	[Guid("D9318A4A-0C92-4AAD-B8F3-6A5F96F2C9B0")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CSafeList<T> : ISafeList<T>
	{
		#region constructor
		public CSafeList() { }
		public CSafeList(T[] l) { List = new List<T>(l); }
		public CSafeList(List<T> l) { List = new List<T>(l); }
		public CSafeList(CSafeList<T> l) { List = new List<T>(l.List); }
		#endregion

		#region properties
		/// <summary>
		/// The encapsulated list
		/// </summary>
		public List<T> List { get => _list; private set => _list = null != value ? value : _list; }
		List<T> _list = new List<T>();
		/// <summary>
		/// Number of objects inside the list
		/// </summary>
		public int Count { get => List.Count; }
		/// <summary>
		/// Get the object at the specified position
		/// </summary>
		/// <param name="index">Zero-based index of the object to fetch</param>
		/// <returns>An object if successful, null otherwise</returns>
		public T this[int index]
		{
			get
			{
				try
				{
					return List[index];
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}
				return default(T);
			}
			set
			{
				try
				{
					List[index] = value;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}
			}
		}
		#endregion

		#region methods
		/// <summary>
		/// Clears the encapsulated list
		/// </summary>
		public void Clear() { List.Clear(); }
		/// <summary>
		/// Add an item at the end of the encapsulated list
		/// </summary>
		/// <param name="o">Object to add</param>
		/// <returns>true if added, false otherwise</returns>
		public bool Add(T o)
		{
			try
			{
				List.Add(o);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Insert an item inside the encapsulated list at the specified position
		/// </summary>
		/// <param name="index">Zero-based index at which the object should be inserted</param>
		/// <param name="o">Object to insert</param>
		/// <returns>true if inserted, false otherwise</returns>
		public bool Insert(T o, int index = 0)
		{
			try
			{
				List.Insert(index, o);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Remove an object from the encapsulated list
		/// </summary>
		/// <param name="o">Object to remove</param>
		/// <returns>true if removed, false otherwise</returns>
		public bool Remove(T o)
		{
			try
			{
				List.Remove(o);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Remove an object from the encapsulated list at the specified position
		/// </summary>
		/// <param name="index">Zero-based index of the object to remove</param>
		/// <returns>true if removed, false otherwise</returns>
		public bool RemoveAt(int index)
		{
			try
			{
				List.RemoveAt(index);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Get the object at the specified position inside the encapsulated list
		/// </summary>
		/// <param name="index">Zero-based index of the object to fetch</param>
		/// <returns>An object if successful, null otherwise</returns>
		public T Get(int index) { return List[index]; }
		/// <summary>
		/// Get the the encapsulated list as an array
		/// </summary>
		/// <returns>An arry of objects</returns>
		public T[] ToArray() { return List.ToArray(); }
		#endregion
	}

	[ComVisible(true)]
	[Guid("82015BF5-2536-4F5C-966D-625A899DE99C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISafeDictionaryElement<K, T>
	{
		#region ISafeDictionaryElement<K, T> properties
		[DispId(1)]
		K Key { get; set; }
		[DispId(2)]
		T Value { get; set; }
		#endregion
	}
	[Guid("E975A182-43E1-4CAD-9E8B-F4E7BAD75EB2")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class CSafeKeyValue<K, T> : ISafeDictionaryElement<K, T>
	{
		public K Key { get; set; }
		public T Value { get; set; }
	}

	[ComVisible(true)]
	[Guid("FBF1ED11-1E24-4967-BC4E-2E8D75B066A0")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISafeDictionary<K, T>
	{
		#region ISafeDictionary properties
		[DispId(1)]
		Dictionary<K, T> Dict { get; }
		[DispId(2)]
		int Count { get; }
		[DispId(3)]
		ISafeList<K> Keys { get; }
		[DispId(4)]
		ISafeList<T> Values { get; }
		[DispId(5)]
		T this[K k] { get; set; }
		#endregion

		#region ISafeDictionary methods
		[DispId(100)]
		void Clear();
		[DispId(101)]
		bool Add(K k, T o);
		[DispId(102)]
		bool Remove(K k);
		[DispId(103)]
		bool ContainsKey(K k);
		[DispId(104)]
		CSafeKeyValue<K, T>[] ToArray();
		[DispId(105)]
		T Get(K k);
		#endregion
	}
	[Guid("C67F6A91-8D10-462A-9F28-67F08705942D")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public abstract class CSafeDictionary<K, T> : ISafeDictionary<K, T>
	{
		#region constructor
		public CSafeDictionary() { }
		public CSafeDictionary(Dictionary<K, T> l) { Dict = new Dictionary<K, T>(l); }
		public CSafeDictionary(CSafeDictionary<K, T> l) { Dict = new Dictionary<K, T>(l.Dict); }
		#endregion

		#region properties
		/// <summary>
		/// The encapsulated dictionary
		/// </summary>
		public Dictionary<K, T> Dict { get => _dict; private set => _dict = null != value ? value : _dict; }
		Dictionary<K, T> _dict = new Dictionary<K, T>();
		/// <summary>
		/// Number of objects inside the ldictionary
		/// </summary>
		public int Count { get => Dict.Count; }
		/// <summary>
		/// A static list of all keys inside the dictionary
		/// </summary>
		public ISafeList<K> Keys
		{
			get
			{
				K[] k = new K[Dict.Count];
				Dict.Keys.CopyTo(k, 0);
				return new CSafeList<K>(k);
			}
		}
		/// <summary>
		/// A static list of all values inside the dictionary
		/// </summary>
		public ISafeList<T> Values
		{
			get
			{
				T[] k = new T[Dict.Count];
				Dict.Values.CopyTo(k, 0);
				return new CSafeList<T>(k);
			}
		}
		/// <summary>
		/// Get an iteml of the encapsulated dictionary by key
		/// </summary>
		/// <param name="k">Key to use</param>
		/// <returns></returns>
		public T this[K k]
		{
			get
			{
				try
				{
					return Dict[k];
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}
				return default(T);
			}
			set
			{
				try
				{
					Dict[k] = value;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex);
				}
			}
		}
		#endregion

		#region methods
		/// <summary>
		/// Clears the encapsulated dictionary
		/// </summary>
		public void Clear() { Dict.Clear(); }
		/// <summary>
		/// Add an item at the end of the encapsulated dictionary
		/// </summary>
		/// <param name="k">Object key</param>
		/// <param name="o">Object to add</param>
		/// <returns>true if added, false otherwise</returns>
		public bool Add(K k, T o)
		{
			try
			{
				Dict.Add(k, o);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Remove an object from the encapsulated dictionary
		/// </summary>
		/// <param name="k">Key of the object to remove</param>
		/// <returns>true if removed, false otherwise</returns>
		public bool Remove(K k)
		{
			try
			{
				Dict.Remove(k);
				return true;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Indicates whether the specified key is contained inside the encapsulated dictionary
		/// </summary>
		/// <param name="k">Key to look for</param>
		/// <returns>true if removed, false otherwise</returns>
		public bool ContainsKey(K k)
		{
			return Dict.ContainsKey(k);
		}
		/// <summary>
		/// Get the the encapsulated dictionary as an array of <see cref="CSafeKeyValue{K, T}"/>
		/// </summary>
		/// <returns>An arry of objects</returns>
		public CSafeKeyValue<K, T>[] ToArray()
		{
			int i = 0;
			CSafeKeyValue<K, T>[] e = new CSafeKeyValue<K, T>[Dict.Count];
			foreach (KeyValuePair<K, T> k in Dict)
			{
				e[i].Key = k.Key;
				e[i++].Value = k.Value;
			}
			return e;
		}
		/// <summary>
		/// Get the object at the specified position inside the encapsulated dictionary
		/// </summary>
		/// <param name="k">Key of the object to look for</param>
		/// <returns>true if removed, false otherwise</returns>
		public T Get(K k) { return this[k]; }
		#endregion
	}
}
