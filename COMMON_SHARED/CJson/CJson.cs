using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System;

#if NET35
using System.Linq;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
#else
using Newtonsoft.Json;
#endif

namespace COMMON
{
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
		/// If the file name is not valid a temp file is opened whose name is tored inside this variable
		/// </summary>
		public string FileName
		{
			get => _filename;
			set
			{
				value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
				// check whether the file is valid or not
				try
				{
					FileInfo fi = new FileInfo(value);
					_filename = fi.FullName;
				}
				catch (Exception ex)
				{
					// use a default file for settings
					_filename = Path.GetRandomFileName();
					CLog.AddException(MethodBase.GetCurrentMethod().Name, ex, $"Value: {value} - Value that will be used is: {_filename}");
				}
			}
		}
		private string _filename = string.Empty;
		#endregion

		#region methods
		/// <summary>
		/// Read settings from a file
		/// </summary>
		/// <param name="addNull">Indicates whether null values must be kept or not inside </param>
		/// <returns>A structure of the specified settings if successful, null otherwise</returns>
		public TSettings ReadSettings(bool addNull = false)
		{
			try
			{
				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						string data = reader.ReadToEnd();
						return Deserialize(data, addNull);
					}
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				return default(TSettings);
			}
		}
		/// <summary>
		/// Write settings of the specified type
		/// </summary>
		/// <param name="settings">The settings to write</param>
		/// <param name="addNull">Indicates whether null values must be kept or not when serializing</param>
		/// <returns>TRUE if the settings have been written, FALSE otherwise</returns>
		public bool WriteSettings(TSettings settings, bool addNull = false)
		{
			try
			{
				// open file and deserialize it
				using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(stream))
					{
						string data = Serialize(settings, addNull);
						writer.Write(data);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				return false;
			}
		}
		/// <summary>
		/// Serialize a TSettings object
		/// </summary>
		/// <param name="settings">The object to serialize</param>
		/// <param name="addNull">Indicates whether <see langword="null"/>values must be kept or not</param>
		/// <returns></returns>
		public static string Serialize(TSettings settings, bool addNull = false)
		{
#if NET35
			JavaScriptSerializer JsonConvert = new JavaScriptSerializer();
			try
				{
				string data = JsonConvert.Serialize(settings);
				if (null == data)
					return string.Empty;
				return data;
				}
			catch (Exception)
				{
				return string.Empty;
				}
#else
			try
			{
				string data = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented, (JsonSerializerSettings)Prepare(addNull));
				if (null == data)
					return string.Empty;
				return data;
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				return string.Empty;
			}
#endif
		}
		/// <summary>
		/// Deserialize an object to a string
		/// </summary>
		/// <param name="settings">The object to deserialize</param>
		/// <param name="addNull">Indicates whether <see langword="null"/>values must be kept or not</param>
		/// <returns>The desirialized object or null if an error has occurred</returns>
		public static TSettings Deserialize(string settings, bool addNull = false)
		{
#if NET35
			try
				{
				return new JavaScriptSerializer().Deserialize<TSettings>(settings);
				}
			catch (Exception ex)
				{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				return default(TSettings);
				}
#else
			try
			{
				return JsonConvert.DeserializeObject<TSettings>(settings, (JsonSerializerSettings)Prepare(addNull));
			}
			catch (Exception ex)
			{
				CLog.AddException(MethodBase.GetCurrentMethod().Name, ex);
				return default(TSettings);
			}
#endif
		}
		/// <summary>
		/// Prepare json settings to use
		/// </summary>
		/// <param name="addNull">Indicates whether <see langword="null"/>values must be kept or not</param>
		/// <returns></returns>
		private static object Prepare(bool addNull)
		{
#if NET35
			return null;
#else
			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
			jsonSerializerSettings.NullValueHandling = addNull ? NullValueHandling.Include : NullValueHandling.Ignore;
			jsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
			jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
			return jsonSerializerSettings;
#endif
		}
		#endregion
	}
}
