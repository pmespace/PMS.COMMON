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

namespace COMMON
{
	static class CJsonTypeExtensions
	{
		public static IEnumerable<Type> BaseTypesAndSelf(this Type type)
		{
			while (null != type)
			{
				yield return type;
				type = type.BaseType;
			}
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
					CLog.EXCEPT(ex, $"File {value} can't be found");
					_filename = default;
				}
			}
		}
		private string _filename = default;
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
		#endregion

		#region methods
		/// <summary>
		/// Read settings from a file
		/// </summary>
		/// <param name="serializerSettings">Settings to use to deserialize, if null default settings are as iin <see cref="Deserialize(string, JsonSerializerSettings)"/></param>
		/// <returns>
		/// A structure of the specified settings if successful, null or an exception otherwise
		/// </returns>
		public TSettings ReadSettings(JsonSerializerSettings serializerSettings = default)
		{
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
				CLog.EXCEPT(ex);
				throw;
			}
		}

		/// <summary>
		/// Deserialize an object to a string
		/// </summary>
		/// <param name="o">The object to deserialize</param>
		/// <param name="serializerSettings">Settings to use to deserialize, if null default settings are: missing members are ignored</param>
		/// <returns>
		/// The deserialized object if successful, null otherwise
		/// </returns>
		public static TSettings Deserialize(string o, JsonSerializerSettings serializerSettings = default)
		{
			try
			{
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
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings are as in <see cref="Serialize(TSettings, JsonSerializerSettings)"/>/></param>
		/// <param name="overwrite">Indicates whether writing can overwrite an existing file; if false and the existing file is not empty then a new file is created and the old one is being added a ".sav" extension</param>
		/// <returns>
		/// True if successful, false or an exception otherwise
		/// </returns>
		public bool WriteSettings(TSettings o, JsonSerializerSettings serializerSettings = default, bool overwrite = true)
		{
			try
			{
				SafeFileWrite(overwrite);

				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(stream))
					{
						string data = Serialize(o, serializerSettings);
						writer.Write(data);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
				throw;
			}
		}
		private void SafeFileWrite(bool overwrite)
		{
			try
			{
				FileInfo fi = new FileInfo(FileName);
				if (fi.Exists && 0 != fi.Length && !overwrite)
					File.Move(FileName, FileName + $".{DateTime.Now.ToString(Chars.SDATETIMEEX)}.sav.json");
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
		/// <returns>
		/// A string representing the serialized object, null otherwise
		/// </returns>
		public static string Serialize(TSettings o, JsonSerializerSettings serializerSettings = default)
		{
			try
			{
				serializerSettings = serializerSettings ?? new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Include,
					Formatting = Newtonsoft.Json.Formatting.Indented,
					DefaultValueHandling = DefaultValueHandling.Include,
				};
				return JsonConvert.SerializeObject(o, serializerSettings.Formatting, serializerSettings);
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default;
		}
		///// <summary>
		///// Prepare json settings to use
		///// </summary>
		///// <param name="addNull">Indicates whether null values must be kept or not when serializing data</param>
		///// <param name="ignoreMissingMember">Indicates whether missing members must be ignored or not (thus preventing deserialization to complete)</param>
		///// <returns>A <see cref="JsonSerializerSettings"/> object to use</returns>
		//private static JsonSerializerSettings Prepare(bool addNull, bool ignoreMissingMember)
		//{
		//	return new JsonSerializerSettings()
		//	{
		//		NullValueHandling = addNull ? NullValueHandling.Include : NullValueHandling.Ignore,
		//		DefaultValueHandling = DefaultValueHandling.Include,
		//		MissingMemberHandling = ignoreMissingMember ? MissingMemberHandling.Ignore : MissingMemberHandling.Error,
		//	};
		//}

		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to put base class properties first
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <param name="alphabetical">true if properties must be order alphabetically inside the objects, false if not required</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public JsonSerializerSettings SerializeBaseClassFirst(JsonSerializerSettings settings = default, bool alphabetical = true)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = (alphabetical ? baseFirstThenAlphabeticalResolver : baseFirstResolver);
			settings.TypeNameHandling = TypeNameHandling.None;
			return settings;
		}
		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to serialize objects alphabetical order
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public JsonSerializerSettings SerializeAlphabetically(JsonSerializerSettings settings = default)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = alphabeticalResolver;
			settings.TypeNameHandling = TypeNameHandling.None;
			return settings;
		}
		/// <summary>
		/// Set <see cref="JsonSerializerSettings"/> to serialize objects without any specific order
		/// </summary>
		/// <param name="settings"><see cref="JsonSerializerSettings"/> to update if any</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> set accordingly, this could be <paramref name="settings"/> updated if specified</returns>
		public JsonSerializerSettings SerializeStandard(JsonSerializerSettings settings = default)
		{
			if (default == settings) settings = new JsonSerializerSettings();
			settings.ContractResolver = default;
			settings.TypeNameHandling = TypeNameHandling.None;
			return settings;
		}
		#endregion
	}
}
