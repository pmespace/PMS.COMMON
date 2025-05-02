using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using COMMON.Properties;
using Newtonsoft.Json.Linq;

namespace COMMON
{
	static class CJsonTypeExtensions
	{
		public static IEnumerable<Type> BaseTypesAndSelf(this Type type)
		{
			while (default != type)
			{
				yield return type;
				type = type.BaseType;
			}
		}
	}

	/// <summary>
	/// Inheriting that class provides a <see cref="JsonExtensionDataAttribute"/> collector
	/// </summary>
	public class CJsonObject
	{
		public CJsonObject()
		{
			extensionData = new Dictionary<string, object>();
		}
		[JsonExtensionData]
		public Dictionary<string, object> extensionData;
	}

	public static class CJson
	{
		/// <summary>
		/// To write elements base class before child class
		/// </summary>
		static readonly IContractResolver baseFirstResolver = new BaseFirstContractResolver { };
		class BaseFirstContractResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
				 base.CreateProperties(type, memberSerialization)
					  ?.OrderBy(b => b.DeclaringType.BaseTypesAndSelf().Count()).ToList();
		}
		/// <summary>
		/// To write elements in alphabatical order
		/// </summary>
		static readonly IContractResolver alphabeticalResolver = new AlphabeticalContractResolver { };
		class AlphabeticalContractResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
				 base.CreateProperties(type, memberSerialization)
					  ?.OrderBy(p => p.PropertyName).ToList();
		}
		/// <summary>
		/// To write elements base class first but in alphabetical order
		/// </summary>
		static readonly IContractResolver baseFirstThenAlphabeticalResolver = new BaseFirstThenAlphabeticalContractResolver { };
		class BaseFirstThenAlphabeticalContractResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
					 base.CreateProperties(type, memberSerialization)
						  ?.OrderBy(b => b.DeclaringType.BaseTypesAndSelf().Count()).ThenBy(p => p.PropertyName).ToList();
		}
		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to put base class properties first
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <param name="alphabetical">true if properties must be order alphabetically inside the objects, false if not required</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public static JsonSerializerSettings SerializeBaseClassFirstEx(JsonSerializerSettings settings = default, bool alphabetical = true)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = (alphabetical ? baseFirstThenAlphabeticalResolver : baseFirstResolver);
			settings.TypeNameHandling = TypeNameHandling.None;
			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			return settings;
		}
		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to serialize objects alphabetical order
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public static JsonSerializerSettings SerializeAlphabeticallyEx(JsonSerializerSettings settings = default)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = alphabeticalResolver;
			settings.TypeNameHandling = TypeNameHandling.None;
			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			return settings;
		}
		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to serialize objects without any specific order
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public static JsonSerializerSettings SerializeStandardEx(JsonSerializerSettings settings = default)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = default;
			settings.TypeNameHandling = TypeNameHandling.None;
			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			return settings;
		}
		/// <summary>
		/// Sort JSON extension data alphabetically and recursively
		/// The extension data MUST be of type <see cref="Dictionary{TKey, TValue}"/> with TKey=<see cref="string"/> and TValue=<see cref="object"/>
		/// </summary>
		/// <param name="extensionData">The extension data to sort</param>
		/// <returns>
		/// A new extension data object sorted alphabetically if successful, null otherwise
		/// </returns>
		public static IDictionary<string, object> SortExtensionData(IDictionary<string, object> extensionData)
		{
			if (default == extensionData) return null;
			CJson<IDictionary<string, object>> json = new CJson<IDictionary<string, object>>();
			SortedUniversalObject su = new SortedUniversalObject();
			try
			{
				foreach (var v in extensionData)
				{
					CJsonObject ou = default == v.Value ? default : CJson<CJsonObject>.Deserialize(v.Value.ToString());
					if (default != ou)
					{
						if (default != ou.extensionData)
							su.extensionData.Add(v.Key, SortExtensionData(ou.extensionData));
					}
					else
					{
						su.extensionData.Add(v.Key, v.Value);
					}
				}
				return new Dictionary<string, object>(su.extensionData);
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return null;
		}

		class SortedUniversalObject
		{
			public SortedUniversalObject()
			{
				extensionData = new SortedDictionary<string, object>();
			}
			[JsonExtensionData]
			public SortedDictionary<string, object> extensionData;
		}
		/// <summary>
		/// Sort JSON extension data alphabetically and recursively
		/// </summary>
		/// <param name="o">The object inside an extension data property or field having the <see cref="JsonExtensionDataAttribute"/> set</param>
		/// <returns>
		/// true if sorted, false otherwise
		/// </returns>
		public static bool SortExtensionData<T>(T o)
		{
			bool ok = false;
			try
			{
				if (!ok)
					// lokk extension data inside the properties
					foreach (PropertyInfo k in o.GetType().GetProperties())
					{
						// it is the extension data
						if (Attribute.IsDefined(k, typeof(JsonExtensionDataAttribute)))
						{
							// replace the extension data by a sorted version of it
							k.SetValue(o, SortExtensionData(k.GetValue(o) as IDictionary<string, T>));
							ok = true;
							break;
						}
					}

				if (!ok)
					// lokk extension data inside the fields
					foreach (FieldInfo k in o.GetType().GetFields())
					{
						// it is the extension data
						if (Attribute.IsDefined(k, typeof(JsonExtensionDataAttribute)))
						{
							// replace the extension data by a sorted version of it
							k.SetValue(o, SortExtensionData(k.GetValue(o) as IDictionary<string, T>));
							ok = true;
							break;
						}
					}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return ok;
		}
	}

	/// <summary>
	/// Provides an easy way to read and write settings to a JSON file
	/// </summary>
	/// <typeparam name="TSettings"></typeparam>
	[ComVisible(false)]
	public class CJson<TSettings>
	{
		#region constructors
		/// <summary>
		/// Creates a JSON settings file managed in memory
		/// </summary>
		public CJson() { }
		/// <summary>
		/// Creates a JSON settings file, allowing to easily manipulate a TSettings class
		/// </summary>
		/// <param name="fname"></param>
		public CJson(string fname) { FileName = fname; }
		#endregion

		#region properties
		/// <summary>
		/// Buffer size to use when opening a json file
		/// </summary>
		public int BufferSize { get => _buffersize; set => _buffersize = 0 < value ? value : DEFAULT_BUFFER_SIZE; }
		int _buffersize = DEFAULT_BUFFER_SIZE;
		public const int DEFAULT_BUFFER_SIZE = 4096;
		/// <summary>
		/// The fully qualified file name for the settings file
		/// Setting this file name will open a LOG file if it is valid
		/// If the file name cannot be found the file name remains null
		/// </summary>
		public string FileName
		{
			get => _filename;
			set
			{
				value = string.IsNullOrEmpty(value) ? default : value.Trim();
				// check whether the file is valid or not
				try
				{
					FileInfo fi = new FileInfo(value);
					_filename = fi.FullName;
				}
				catch (Exception ex)
				{
					CLog.EXCEPT(ex, Resources.CJsonInvalidFileName.Format(value));
					_filename = default;
				}
			}
		}
		string _filename = default;
		/// <summary>
		/// The fully qualified file name of the old settings file after <see cref="WriteSettings(TSettings, JsonSerializerSettings, bool)"/> has been called.
		/// If overwrite was true, that property contains the name of the old settings file,
		/// if overwrite was false that property is equal to <see cref="FileName"/>.
		/// </summary>
		public string SavedFileName { get; private set; }
		/// <summary>
		/// Last exception encountered during processing
		/// </summary>
		public Exception LastException { get; private set; }
		///// <summary>
		///// To write elements base class before child class
		///// </summary>
		//static readonly IContractResolver baseFirstResolver = new BaseFirstContractResolver { };
		//class BaseFirstContractResolver : DefaultContractResolver
		//{
		//	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
		//		 base.CreateProperties(type, memberSerialization)
		//			  ?.OrderBy(b => b.DeclaringType.BaseTypesAndSelf().Count()).ToList();
		//}
		///// <summary>
		///// To write elements in alphabatical order
		///// </summary>
		//static readonly IContractResolver alphabeticalResolver = new AlphabeticalContractResolver { };
		//class AlphabeticalContractResolver : DefaultContractResolver
		//{
		//	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
		//		 base.CreateProperties(type, memberSerialization)
		//			  ?.OrderBy(p => p.PropertyName).ToList();
		//}
		///// <summary>
		///// To write elements base class first but in alphabetical order
		///// </summary>
		//static readonly IContractResolver baseFirstThenAlphabeticalResolver = new BaseFirstThenAlphabeticalContractResolver { };
		//class BaseFirstThenAlphabeticalContractResolver : DefaultContractResolver
		//{
		//	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
		//			 base.CreateProperties(type, memberSerialization)
		//				  ?.OrderBy(b => b.DeclaringType.BaseTypesAndSelf().Count()).ThenBy(p => p.PropertyName).ToList();
		//}
		#endregion

		#region methods
		public override string ToString() => FileName;
		/// <summary>
		/// Opens a settings file and reads its content.
		/// If no file could be read the function may create it and write a default sample
		/// </summary>
		/// <param name="filename">File to read, set to the file's full name if found</param>
		/// <param name="sample">Sample of appropriate type to save inside the file if no data is available</param>
		/// <returns>
		/// An object of the desired type if available, null otherwise
		/// </returns>
		public static TSettings GetSettings(ref string filename, TSettings sample = default)
		{
			TSettings settings = default;
			try
			{
				// tries to open the file and verifies whether it exists
				CJson<TSettings> json = new CJson<TSettings>(filename);
				if (!json.FileName.IsNullOrEmpty())
				{
					// read data
					settings = json.ReadSettings();
					if (null == settings && null != sample)
					{
						// nothing to read, let's create a content
						json.WriteSettings(sample);
					}
				}
				filename = json.FileName;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
				filename = default;
			}
			return settings;
		}
		/// <summary>
		/// Tries to open a settings file and saves the settings
		/// </summary>
		/// <param name="filename">File to write to</param>
		/// <param name="settings">data to save</param>
		/// <returns>
		/// true if successfully saved, false otherwise
		/// </returns>
		public static bool SetSettings(string filename, TSettings settings)
		{
			try
			{
				// tries to open the file and verifies whether it exists
				CJson<TSettings> json = new CJson<TSettings>(filename);
				if (!json.FileName.IsNullOrEmpty())
				{
					// write data
					json.WriteSettings(settings);
					return true;
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Read settings from a file
		/// </summary>
		/// <param name="serializerSettings">Settings to use to deserialize, if null default settings are as iin <see cref="Deserialize(string, JsonSerializerSettings, bool)"/></param>
		/// <returns>
		/// A structure of the specified settings if successful, null otherwise
		/// </returns>
		public TSettings ReadSettings(JsonSerializerSettings serializerSettings = default)
		{
			LastException = default;
			try
			{
				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						string data = reader.ReadToEnd();
						return Deserialize(data, serializerSettings);
					}
				}
			}
			catch (Exception ex)
			{
				LastException = ex;
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Deserialize an object to a string
		/// </summary>
		/// <param name="o">The json file to deserialize as a string or a file name to read and deserialize the content; if <paramref name="isFile"/> is true this must be a valid file name containing the json to deserialize</param>
		/// <param name="serializerSettings">Settings to use to deserialize, if null default settings are: missing members are ignored</param>
		/// <param name="isFile">True if <paramref name="o"/> links to a file, false if a regular string</param>
		/// <returns>
		/// The deserialized object if successful, null otherwise
		/// </returns>
		public static TSettings Deserialize(string o, JsonSerializerSettings serializerSettings = default, bool isFile = false)
		{
			try
			{
				// read file if necessary
				if (isFile)
				{
					using (StreamReader sr = new StreamReader(o))
					{
						o = sr.ReadToEnd();
					}
				}
				serializerSettings = serializerSettings ?? new JsonSerializerSettings()
				{
					MissingMemberHandling = MissingMemberHandling.Ignore,
				};
				return JsonConvert.DeserializeObject<TSettings>(o, serializerSettings);
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Write settings of the specified type
		/// </summary>
		/// <param name="o">The settings to write</param>
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings are as in <see cref="Serialize(TSettings, JsonSerializerSettings, string)"/>/></param>
		/// <param name="overwrite">Indicates whether writing can overwrite an existing file; if false and the existing file is not empty then a new file is created and the old one is being added a ".sav" extension</param>
		/// <returns>
		/// True if successful, false otherwise
		/// </returns>
		public bool WriteSettings(TSettings o, JsonSerializerSettings serializerSettings = default, bool overwrite = true)
		{
			LastException = default;
			try
			{
				SafeFileWrite(overwrite);

				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))//, FileShare.ReadWrite, BufferSize, FileOptions.WriteThrough))
				{
					using (StreamWriter writer = new StreamWriter(stream))
					{
						string data = Serialize(o, serializerSettings);
						writer.Write(data);
						writer.Flush();
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				LastException = ex;
				CLog.EXCEPT(ex);
			}
			return false;
		}
		private void SafeFileWrite(bool overwrite)
		{
			try
			{
				FileInfo fi = new FileInfo(FileName);
				if (fi.Exists && 0 != fi.Length && !overwrite)
				{
					SavedFileName = FileName + $".{DateTime.Now.ToString(Chars.SDATETIMEEX)}.sav{fi.Extension}";
					File.Move(FileName, FileName + $".{DateTime.Now.ToString(Chars.SDATETIMEEX)}.sav.json");
				}
				else
					SavedFileName = FileName;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
		}
		/// <summary>
		/// Serialize a TSettings object
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings are: null and default values added, indentation</param>
		/// <param name="fname">Name of a file to create and that will contain the serialised json</param>
		/// <returns>
		/// A string representing the serialized object, null otherwise
		/// </returns>
		public static string Serialize(TSettings o, JsonSerializerSettings serializerSettings = default, string fname = null)
		{
			try
			{
				serializerSettings = serializerSettings ?? new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Include,
					Formatting = Newtonsoft.Json.Formatting.Indented,
					DefaultValueHandling = DefaultValueHandling.Include,
				};
				string s = JsonConvert.SerializeObject(o, serializerSettings.Formatting, serializerSettings);
				try
				{
					if (!fname.IsNullOrEmpty())
					{
						using (StreamWriter sw = new StreamWriter(fname))
						{
							serializerSettings = new JsonSerializerSettings()
							{
								NullValueHandling = NullValueHandling.Include,
								Formatting = Newtonsoft.Json.Formatting.Indented,
								DefaultValueHandling = DefaultValueHandling.Include,
							};
							string sx = JsonConvert.SerializeObject(o, serializerSettings.Formatting, serializerSettings);
							sw.Write(sx);
						}
					}
				}
				catch (Exception ex) { }
				return s;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		///// <summary>
		///// Set <see cref="JsonSerializerSettings"/> to put base class properties first
		///// </summary>
		///// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		///// <param name="alphabetical">true if properties must be order alphabetically inside the objects, false if not required</param>
		///// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		//public JsonSerializerSettings SerializeBaseClassFirstEx(JsonSerializerSettings settings = default, bool alphabetical = true)
		//{
		//	if (default == settings) settings = new JsonSerializerSettings();
		//	settings.ContractResolver = (alphabetical ? baseFirstThenAlphabeticalResolver : baseFirstResolver);
		//	settings.TypeNameHandling = TypeNameHandling.None;
		//	settings.Formatting = Newtonsoft.Json.Formatting.Indented;
		//	return settings;
		//}
		public JsonSerializerSettings SerializeBaseClassFirst(JsonSerializerSettings settings = default, bool alphabetical = true) => CJson.SerializeBaseClassFirstEx(settings);
		///// <summary>
		///// Set <see cref="JsonSerializerSettings"/> to serialize objects alphabetical order
		///// </summary>
		///// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		///// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		//public static JsonSerializerSettings SerializeAlphabeticallyEx(JsonSerializerSettings settings = default)
		//{
		//	if (default == settings) settings = new JsonSerializerSettings();
		//	settings.ContractResolver = alphabeticalResolver;
		//	settings.TypeNameHandling = TypeNameHandling.None;
		//	settings.Formatting = Newtonsoft.Json.Formatting.Indented;
		//	return settings;
		//}
		public JsonSerializerSettings SerializeAlphabetically(JsonSerializerSettings settings = default) => CJson.SerializeAlphabeticallyEx(settings);
		///// <summary>
		///// Set <see cref="JsonSerializerSettings"/> to serialize objects without any specific order
		///// </summary>
		///// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		///// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		//public static JsonSerializerSettings SerializeStandardEx(JsonSerializerSettings settings = default)
		//{
		//	if (default == settings) settings = new JsonSerializerSettings();
		//	settings.ContractResolver = default;
		//	settings.TypeNameHandling = TypeNameHandling.None;
		//	settings.Formatting = Newtonsoft.Json.Formatting.Indented;
		//	return settings;
		//}
		public JsonSerializerSettings SerializeStandard(JsonSerializerSettings settings = default) => CJson.SerializeStandardEx(settings);
		#endregion
	}
}
