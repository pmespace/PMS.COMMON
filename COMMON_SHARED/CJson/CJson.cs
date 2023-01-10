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
		/// <param name="addNull">Indicates whether null values must be kept or not inside </param>
		/// <param name="except">Indicate whether an exception occured while processing the json data, set to true if an exception occured during operation, false otherwise</param>
		/// <returns>
		/// A structure of the specified settings if successful, null otherwise
		/// </returns>
		[Obsolete("Consider using ReadSettings(out Exception except, JsonSerializerSettings serializerSettings = null)")]
		public TSettings ReadSettings(out bool except, bool addNull = false)
		{
			except = false;
			try
			{
				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						string data = reader.ReadToEnd();
						return Deserialize(data, out except, addNull);
					}
				}
			}
			catch (Exception ex)
			{
				except = true;
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Read settings from a file
		/// </summary>
		/// <param name="except">The exception if one occurred</param>
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings will be used</param>
		/// <returns>
		/// A structure of the specified settings if successful, null otherwise
		/// </returns>
		public TSettings ReadSettings(out Exception except, JsonSerializerSettings serializerSettings = default)
		{
			except = default;
			try
			{
				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						string data = reader.ReadToEnd();
						return Deserialize(data, out except, serializerSettings);
					}
				}
			}
			catch (Exception ex)
			{
				except = ex;
				CLog.EXCEPT(ex);
			}
			return default;
		}
		/// <summary>
		/// Write settings of the specified type
		/// </summary>
		/// <param name="o">The settings to write</param>
		/// <param name="addNull">Indicates whether null values must be kept or not when serializing</param>
		/// <param name="overwrite">Indicates whether writing can overwrite an existing file; if false and the existing file is not empty then a new file is created and the old one is being added a ".sav" extension</param>
		/// <returns>
		/// TRUE if the settings have been written, FALSE otherwise
		/// </returns>
		[Obsolete("Consider using WriteSettings(TSettings o, out Exception except, JsonSerializerSettings serializerSettings = null, bool overwrite = true)")]
		public bool WriteSettings(TSettings o, bool addNull = false, bool overwrite = true)
		{
			try
			{
				SafeFileWrite(overwrite);

				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(stream))
					{
						string data = Serialize(o, addNull);
						writer.Write(data);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
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
					File.Move(FileName, FileName + $".{DateTime.Now.ToString(Chars.SDATETIMEEX)}.sav.json");
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
		}
		/// <summary>
		/// Write settings of the specified type
		/// </summary>
		/// <param name="o">The settings to write</param>
		/// <param name="except">The exception if one occurred</param>
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings will be used</param>
		/// <param name="overwrite">Indicates whether writing can overwrite an existing file; if false and the existing file is not empty then a new file is created and the old one is being added a ".sav" extension</param>
		/// <returns>
		/// A structure of the specified settings if successful, null otherwise
		/// </returns>
		public bool WriteSettings(TSettings o, out Exception except, JsonSerializerSettings serializerSettings = default, bool overwrite = true)
		{
			except = default;
			try
			{
				SafeFileWrite(overwrite);

				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(stream))
					{
						string data = Serialize(o, out except, serializerSettings);
						writer.Write(data);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				except = ex;
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Serialize a TSettings object
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <param name="addNull">Indicates whether null values must be kept or not</param>
		/// <returns>
		/// A string representing the serialized object, null otherwise
		/// </returns>
		[Obsolete("Consider using Serialize(TSettings o, out Exception except, JsonSerializerSettings serializerSettings = null, bool indent = false)", true)]
		public static string Serialize(TSettings o, bool addNull = false)
		{
			try
			{
				return JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented, (JsonSerializerSettings)Prepare(addNull, true));
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
				return default;
			}
		}
		/// <summary>
		/// Serialize a TSettings object
		/// </summary>
		/// <param name="o">The object to serialize</param>
		/// <param name="except">The exception if one occurred</param>
		/// <param name="serializerSettings">Settings to use to serialize, if null default settings will be used (no null values)</param>
		/// <param name="indent">If true the resulting json is indented, no indent otherwise</param>
		/// <returns>
		/// A string representing the serialized object, null otherwise
		/// </returns>
		public static string Serialize(TSettings o, out Exception except, JsonSerializerSettings serializerSettings = default, bool indent = true)
		{
			except = null;
			try
			{
				return JsonConvert.SerializeObject(o, indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None, serializerSettings ?? SerializerSettings());
			}
			catch (Exception ex)
			{
				except = ex;
				CLog.EXCEPT(ex);
				return default;
			}
		}
		static JsonSerializerSettings SerializerSettings()
		{
			return new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
			};
		}
		/// <summary>
		/// Deserialize an object to a string
		/// </summary>
		/// <param name="o">The object to deserialize</param>
		/// <param name="jsonException">Indicate whether an exception occured while processing the json data, set to true if an exception occured during operation, false otherwise</param>
		/// <param name="ignoreMissingMember">Indicates whether missing members must be ignored or not (thus preventing deserialization to complete)</param>
		/// <returns>
		/// The deserialized object if successful, null otherwise
		/// </returns>
		[Obsolete("Consider using Deserialize(string o, out Exception except, JsonSerializerSettings serializerSettings = null)")]
		public static TSettings Deserialize(string o, out bool jsonException, bool ignoreMissingMember = true)
		{
			jsonException = false;
			try
			{
				return JsonConvert.DeserializeObject<TSettings>(o, (JsonSerializerSettings)Prepare(false, ignoreMissingMember));
			}
			catch (Exception ex)
			{
				jsonException = true;
				CLog.EXCEPT(ex);
				return default;
			}
		}
		/// <summary>
		/// Deserialize an object to a string
		/// </summary>
		/// <param name="o">The object to deserialize</param>
		/// <param name="except">The exception if one occurred</param>
		/// <param name="serializerSettings">Settings to use to deserialize, if null default settings will be used (missing members are ignored, default values are added)</param>
		/// <returns>
		/// The deserialized object if successful, null otherwise
		/// </returns>
		public static TSettings Deserialize(string o, out Exception except, JsonSerializerSettings serializerSettings = default)
		{
			except = null;
			try
			{
				return JsonConvert.DeserializeObject<TSettings>(o, serializerSettings ?? DeserializerSettings());
			}
			catch (Exception ex)
			{
				except = ex;
				CLog.EXCEPT(ex);
				return default;
			}
		}
		static JsonSerializerSettings DeserializerSettings()
		{
			return new JsonSerializerSettings()
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Include,
			};
		}
		/// <summary>
		/// Prepare json settings to use
		/// </summary>
		/// <param name="addNull">Indicates whether null values must be kept or not when serializing data</param>
		/// <param name="ignoreMissingMember">Indicates whether missing members must be ignored or not (thus preventing deserialization to complete)</param>
		/// <returns>A <see cref="JsonSerializerSettings"/> object to use</returns>
		private static JsonSerializerSettings Prepare(bool addNull, bool ignoreMissingMember)
		{
			return new JsonSerializerSettings()
			{
				NullValueHandling = addNull ? NullValueHandling.Include : NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Include,
				MissingMemberHandling = ignoreMissingMember ? MissingMemberHandling.Ignore : MissingMemberHandling.Error,
			};
		}
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
