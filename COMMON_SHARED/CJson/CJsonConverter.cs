using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System;
using Newtonsoft.Json;

namespace COMMON
{
	/// <summary>
	/// 
	/// </summary>
	[ComVisible(false)]
	public static class CJsonConverter
	{
		#region methods
		/// <summary>
		/// Converts a JSON to an XML representation
		/// </summary>
		/// <param name="json">The JSON string to convert</param>
		/// <param name="root">The starting node name to use when converting from xml to json</param>
		/// <param name="writeArrayAttribute">[NewtonSoft.Json] This attribute helps preserve arrays when converting the written XML back to JSON</param>
		/// <param name="encodeSpecialCharacters">[NewtonSoft.Json] A value to indicate whether to encode special characters when converting JSON to XML</param>
		/// <returns>The XML produced from the JSON string, or an empty string if an error has occurred. This is required for non .NET 3.5 builds</returns>
		public static string JsonToXML(string json, string root = default, bool writeArrayAttribute = true, bool encodeSpecialCharacters = false)
		{
			if (!string.IsNullOrEmpty(json))
			{
				// convert from JSON to XML
				XmlDocument doc = new XmlDocument();
				try
				{
					// convert to XML
					doc = JsonConvert.DeserializeXmlNode(json, root, writeArrayAttribute, encodeSpecialCharacters);
					// get the XML in a string
					return doc.InnerXml;
				}
				catch (Exception) { }
			}
			return default;
		}
		/// <summary>
		/// Converts a XML to a JSPN representation
		/// </summary>
		/// <param name="xml">The XML string to convert</param>
		/// <returns>The JSON produced from the XML string, or an empty string if an error has occurred</returns>
		public static string XMLToJson(string xml)
		{
			if (!string.IsNullOrEmpty(xml))
			{
				// convert from XML to JSON
				XmlDocument doc = new XmlDocument();
				try
				{
					// try to load the XML into a XML document
					doc.LoadXml(xml);
					// arrived here the XML document has been created, serialize it to JSON
					return JsonConvert.SerializeXmlNode(doc);
				}
				catch (Exception) { }
			}
			return default;
		}
		#endregion
	}
}
