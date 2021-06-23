using System.Runtime.InteropServices;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System;

#if OLD_NET35
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
		public static string JsonToXML(string json, string root = null, bool writeArrayAttribute = true, bool encodeSpecialCharacters = false)
		{
			if (!string.IsNullOrEmpty(json))
			{
				// convert from JSON to XML
				XmlDocument doc = new XmlDocument();
				try
				{

#if OLD_NET35
					doc.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new XmlDictionaryReaderQuotas()));
#else
					// convert to XML
					doc = JsonConvert.DeserializeXmlNode(json, root, writeArrayAttribute, encodeSpecialCharacters);
#endif

					// get the XML in a string
					return doc.InnerXml;
				}
				catch (Exception) { }
			}
			return string.Empty;
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

#if OLD_NET35
					var json = new JavaScriptSerializer().Serialize(GetXmlData(XElement.Parse(xml)));
#else
					// arrived here the XML document has been created, serialize it to JSON
					return JsonConvert.SerializeXmlNode(doc);
#endif

				}
				catch (Exception) { }
			}
			return string.Empty;
		}

#if OLD_NET35
		private static Dictionary<string, object> GetXmlData(XElement xml)
			{
			var attr = xml.Attributes().ToDictionary(d => d.Name.LocalName, d => (object)d.Value);
			if (xml.HasElements)
				attr.Add("_value", xml.Elements().Select(e => GetXmlData(e)));
			else if (!xml.IsEmpty)
				attr.Add("_value", xml.Value);
			return new Dictionary<string, object> { { xml.Name.LocalName, attr } };
			}
#endif

		#endregion
	}
}
