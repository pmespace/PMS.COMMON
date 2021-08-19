using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using System;
using System.IO;

namespace COMMON
{
	[ComVisible(false)]
	static class RegexIP
	{
		/// <summary>
		/// IP formats
		/// </summary>
		public const string REGEX_IPV4_PORT_NUMBER = @"(:([0-9]{1,4}|[1-5][0-9]{1,4}|6[0-9]{1,3}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5]))";
		public const string REGEX_IPV4_ADDRESS_PART = @"([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])";
		public const string REGEX_IPV4_PARTS_1TO3 = "(" + REGEX_IPV4_ADDRESS_PART + @"\.)";
		public const string REGEX_IPV4_PART_4 = REGEX_IPV4_ADDRESS_PART;
		public const string REGEX_HEADER = @"^";
		public const string REGEX_TRAILER = @"$";
		public const string REGEX_URL_CHARACTER_SET = @"[0-9A-Za-z\./-]";
		public const string REGEX_IPV4_WITHOUT_PORT = REGEX_HEADER + REGEX_IPV4_PARTS_1TO3 + "{3}" + REGEX_IPV4_PART_4 + "{1}" + REGEX_TRAILER;
		public const string REGEX_IPV4_WITH_PORT = REGEX_HEADER + REGEX_IPV4_PARTS_1TO3 + "{3}" + REGEX_IPV4_PART_4 + "{1}" + REGEX_IPV4_PORT_NUMBER + "{0,1}" + REGEX_TRAILER;
		public const string REGEX_URL_WITHOUT_PORT = REGEX_HEADER + REGEX_URL_CHARACTER_SET + "+" + REGEX_TRAILER;
		public const string REGEX_URL_WITH_PORT = REGEX_HEADER + REGEX_URL_CHARACTER_SET + "+" + REGEX_IPV4_PORT_NUMBER + "{0,1}" + REGEX_TRAILER;
	}

	/// <summary>
	/// COMMON extensions to c#
	/// </summary>
	static public class CMiscExtensions
	{
		/// <summary>
		/// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another 
		/// specified string according the type of search to use for the specified string.
		/// [created by: Oleg Zarevennyi - https://stackoverflow.com/questions/6275980/string-replace-ignoring-case]
		/// </summary>
		/// <param name="str">The string performing the replace method.</param>
		/// <param name="oldValue">The string to be replaced.</param>
		/// <param name="newValue">The string replace all occurrences of <paramref name="oldValue"/>. 
		/// If value is equal to <c>null</c>, than all occurrences of <paramref name="oldValue"/> will be removed from the <paramref name="str"/>.</param>
		/// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
		/// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>. 
		/// If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</returns>
		public static string Replace(this string str, string oldValue, string @newValue, StringComparison comparisonType)
		{
			// Check inputs.
			if (str == null)
			{
				// Same as original .NET C# string.Replace behavior.
				throw new ArgumentNullException(nameof(str));
			}
			if (str.Length == 0)
			{
				// Same as original .NET C# string.Replace behavior.
				return str;
			}
			if (oldValue == null)
			{
				// Same as original .NET C# string.Replace behavior.
				throw new ArgumentNullException(nameof(oldValue));
			}
			if (oldValue.Length == 0)
			{
				// Same as original .NET C# string.Replace behavior.
				throw new ArgumentException("String cannot be of zero length.");
			}

			// Prepare string builder for storing the processed string.
			// Note: StringBuilder has a better performance than String by 30-40%.
			StringBuilder resultStringBuilder = new StringBuilder(str.Length);

			// Analyze the replacement: replace or remove.
			bool isReplacementNullOrEmpty = string.IsNullOrEmpty(@newValue);

			// Replace all values.
			const int valueNotFound = -1;
			int foundAt;
			int startSearchFromIndex = 0;
			while ((foundAt = str.IndexOf(oldValue, startSearchFromIndex, comparisonType)) != valueNotFound)
			{
				// Append all characters until the found replacement.
				int @charsUntilReplacment = foundAt - startSearchFromIndex;
				bool isNothingToAppend = @charsUntilReplacment == 0;
				if (!isNothingToAppend)
				{
					resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilReplacment);
				}

				// Process the replacement.
				if (!isReplacementNullOrEmpty)
				{
					resultStringBuilder.Append(@newValue);
				}

				// Prepare start index for the next search.
				// This needed to prevent infinite loop, otherwise method always start search 
				// from the start of the string. For example: if an oldValue == "EXAMPLE", newValue == "example"
				// and comparisonType == "any ignore case" will conquer to replacing:
				// "EXAMPLE" to "example" to "example" to "example" … infinite loop.
				startSearchFromIndex = foundAt + oldValue.Length;
				if (startSearchFromIndex == str.Length)
				{
					// It is end of the input string: no more space for the next search.
					// The input string ends with a value that has already been replaced. 
					// Therefore, the string builder with the result is complete and no further action is required.
					return resultStringBuilder.ToString();
				}
			}
			// Append the last part to the result.
			int @charsUntilStringEnd = str.Length - startSearchFromIndex;
			resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilStringEnd);

			return resultStringBuilder.ToString();
		}
	}

	/// <summary>
	/// Various tool functions
	/// </summary>
	[ComVisible(false)]
	public static class CMisc
	{
		public const int ONEBYTE = 1;
		public const int TWOBYTES = 2;
		public const int FOURBYTES = 4;
		public const int EIGHTBYTES = 8;
		public const int UNKNOWN = -int.MaxValue;
		/// <summary>
		/// Converts an array of bytes to an hexadecimal string
		/// Each byte gives a 2 chars hexadecimal value
		/// </summary>
		/// <param name="buffer">The array of bytes to convert</param>
		/// <returns>The converted array into a string if successful, an empty string if any error occured</returns>
		public static string BytesToHexStr(byte[] buffer)
		{
			string res = string.Empty;
			try
			{
				foreach (byte b in buffer)
					res += b.ToString("X2");
			}
			catch (Exception) { res = string.Empty; }
			return res;
		}
		/// <summary>
		/// Converts an array of bytes to a UTF-8 string (if possible)
		/// </summary>
		/// <param name="buffer">The array of bytes to convert</param>
		/// <returns>The converted array into a UTF-8 string if successful, an empty string if any error occured</returns>
		public static string BytesToStr(byte[] buffer)
		{
			string res = string.Empty;
			try
			{
				Encoding.UTF8.GetString(buffer);
			}
			catch (Exception) { res = string.Empty; }
			return res;
		}
		/// <summary>
		/// Safe string to int function
		/// </summary>
		/// <param name="s">The string to convert to int</param>
		/// <param name="alwayspositive">Indicates whether the value must always be positive or not</param>
		/// <returns>The value of the string, 0 if an error occured</returns>
		public static long StrToLong(string s, bool alwayspositive = false)
		{
			long i = 0;
			try
			{
				i = long.Parse(s);
				if (alwayspositive && 0 > i)
					i = -i;
			}
			catch (Exception) { i = 0; }
			return i;
		}
		/// <summary>
		/// Copy bytes from short integral type to byte[].
		/// The array of bytes is 2 bytes long (size of short).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of butes</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(short value)
		{
			byte[] bb = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bb);
			return bb;
		}
		/// <summary>
		/// Copy bytes from int integral type to byte[].
		/// The array of bytes is 4 bytes long (size of int).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of butes</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(int value)
		{
			byte[] bb = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bb);
			return bb;
		}
		/// <summary>
		/// Copy bytes from long integral type to byte[].
		/// The array of bytes is 8 bytes long (size of long).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of butes</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(long value)
		{
			byte[] bb = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bb);
			return bb;
		}
		/// <summary>
		/// Copy bytes from long integral type to byte[].
		/// The array of bytes is 8 bytes long (size of long).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of butes</param>
		/// <param name="maxlen">Size of the buffer to create that will receive the value computed</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(long value, int maxlen)
		{
			if (CMisc.TWOBYTES == maxlen || CMisc.FOURBYTES == maxlen || CMisc.EIGHTBYTES == maxlen)
			{
				switch (maxlen)
				{
					case CMisc.TWOBYTES:
						{
							short t = (short)value;
							return SetBytesFromIntegralTypeValue(t);
						}
					case CMisc.FOURBYTES:
						{
							int t = (int)value;
							return SetBytesFromIntegralTypeValue(t);
						}
					case CMisc.EIGHTBYTES:
					default:
						return SetBytesFromIntegralTypeValue(value);
				}
			}
			return null;
		}
		/// <summary>
		/// Get integral long value value from an array of bytes where each byte (up to 8 bytes) represents a part of the integral value.
		/// This function is useful to retrieve an integral value from a set of bytes
		/// </summary>
		/// <param name="buffer">The array of bytes to analyse</param>
		/// <param name="maxlen">The number of bytes to use to build the integral value</param>
		/// <returns>A long describing the value stored inside the array of bytes, 0 otherwise</returns>
		public static long GetIntegralTypeValueFromBytes(byte[] buffer, int maxlen)
		{
			return GetIntegralTypeValueFromBytes(buffer, 0, maxlen);
		}
		/// <summary>
		/// Get integral long value value from an array of bytes where each byte (up to 8 bytes) represents a part of the integral value.
		/// This function is useful to retrieve an integral value from a set of bytes
		/// </summary>
		/// <param name="buffer">The array of bytes to analyse</param>
		/// <param name="start">The starting position, inside the array of bytes, to get the value</param>
		/// <param name="maxlen">The number of bytes to use to build the integral value</param>
		/// <returns>A long describing the value stored inside the array of bytes, 0 otherwise</returns>
		public static long GetIntegralTypeValueFromBytes(byte[] buffer, int start, int maxlen = CMisc.FOURBYTES)
		{
			long l = 0;
			if (null != buffer)
			{
				maxlen = Math.Min(buffer.Length - start, maxlen);
				byte[] ab = new byte[maxlen];
				Buffer.BlockCopy(buffer, start, ab, 0, maxlen);
				foreach (byte b in ab)
				{
					l += (long)b << 8 * (maxlen - 1);
					maxlen--;
				}
			}
			return l;
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than 1, setting it to 1, and if maxlen is higher then 65535, setting it to 65535
		/// </summary>
		/// <param name="minlen">Minimum length to use</param>
		/// <param name="maxlen">Maximum length to use</param>
		public static void AdjustMinMax1N(ref int minlen, ref int maxlen) { AdjustMinMax(1, ref minlen, ref maxlen); }
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than 1, setting it to 1, and if maxlen is higher then 65535, setting it to 65535
		/// </summary>
		/// <param name="minlen">Minimum length to use</param>
		/// <param name="maxlen">Maximum length to use</param>
		public static void AdjustMinMax0N(ref int minlen, ref int maxlen) { AdjustMinMax(0, ref minlen, ref maxlen); }
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than 1, setting it to 1, and if maxlen is higher then 65535, setting it to 65535
		/// </summary>
		/// <param name="min">The minimum to not go over</param>
		/// <param name="minlen">Minimum length to use</param>
		/// <param name="maxlen">Maximum length to use</param>
		private static void AdjustMinMax(int min, ref int minlen, ref int maxlen)
		{
			minlen = 0 > minlen ? -minlen : minlen;
			maxlen = 0 > maxlen ? -maxlen : maxlen;
			if (maxlen < minlen)
			{
				int i = maxlen;
				maxlen = minlen;
				minlen = i;
			}
			if (min > minlen)
				minlen = min;
			if (0xFFFF < maxlen)
				maxlen = 0xFFFF;
		}

		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// Throws an Exception if the array of bytes length does not comply with the min and max bounds
		/// </summary>
		/// <param name="value">The array of bytes to check</param>
		/// <param name="minlen">The minimum length</param>
		/// <param name="maxlen">The maximum length</param>
		public static void AdjustMinMax1N(byte[] value, ref int minlen, ref int maxlen)
		{
			AdjustMinMax1N(ref minlen, ref maxlen);
			if (value.Length < minlen || value.Length > maxlen)
				throw new Exception("Invalid length - Min length: " + minlen.ToString() + " ; Max length: " + maxlen.ToString() + " ; Actual length: " + value.Length.ToString());
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// Throws an Exception if the string length does not comply with the min and max bounds
		/// </summary>
		/// <param name="value">The string to check</param>
		/// <param name="minlen">The minimum length</param>
		/// <param name="maxlen">The maximum length</param>
		public static void AdjustMinMax1N(string value, ref int minlen, ref int maxlen)
		{
			AdjustMinMax1N(ref minlen, ref maxlen);
			if (value.Length < minlen || value.Length > maxlen)
				throw new Exception("Invalid length - Value: " + value + " - Min length: " + minlen.ToString() + " ; Max length: " + maxlen.ToString() + " ; Actual length: " + value.Length.ToString());
		}
		/// <summary>
		/// Help deciding the length to use when manipulating an array of bytes, according to the min and max considered.
		/// Beware, the function will invert minlen and maxlen if they are not accurate
		/// </summary>
		/// <param name="buffer">Buffer to evaluate</param>
		/// <param name="minlen">Minimum length to use</param>
		/// <param name="maxlen">Maximum length to use</param>
		/// <returns>The length to use, buffer length if between boundaries, 0 otherwise</returns>
		public static int LenToUse(byte[] buffer, ref int minlen, ref int maxlen)
		{
			AdjustMinMax1N(ref minlen, ref maxlen);
			int len = buffer.Length;
			if (len >= minlen && len <= maxlen)
				return len;
			else
				return 0;
		}
		/// <summary>
		/// Test whether a string is composed according to a specified regular expression
		/// </summary>
		/// <param name="value">The value to test</param>
		/// <param name="format">The regular expression to match. The regular expression must be complete and well formatted</param>
		/// <param name="validIfEmpty">TRUE if an empty string bypasses the verification (en empty string is always valid), FALSE otherwise</param>
		/// <returns>TRUE if the value complies with the regular expression (or is empty if allowed), FALSE otherwise</returns>
		public static bool IsValidFormat(string value, string format, bool validIfEmpty = false)
		{
			if (string.IsNullOrEmpty(value) && validIfEmpty)
				return true;
			Regex regex = new Regex(AsString(format));
			return regex.IsMatch(AsString(value));
		}
		/// <summary>
		/// Test whether a string is composed according to a specified character set and length complies with specified bounds
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="characterSet">The character set the value must comply with</param>
		/// <param name="minlen">The minimum length the value must comply with</param>
		/// <param name="maxlen">The maximum length the value must comply with</param>
		/// <param name="validIfEmpty">TRUE if an empty string bypasses the verification (en empty string is always valid), FALSE otherwise</param>
		/// <returns>TRUE if the value complies with the regular expression (or is empty if allowed), FALSE otherwise</returns>
		public static bool IsValidFormat(string value, string characterSet, int minlen, int maxlen, bool validIfEmpty = false)
		{
			if (string.IsNullOrEmpty(value) && validIfEmpty)
				return true;
			// build regular expression to check against
			string count = "{" + (minlen == maxlen ? minlen.ToString() + "}" : minlen.ToString() + "," + maxlen.ToString() + "}");
			Regex regex = new Regex(characterSet + count.ToString());
			return regex.IsMatch(value);
		}
		/// <summary>
		/// Converts a char holding an hexadecimal value to its binary value (A=10,...).
		/// If the character is not hexadecimal compliant (0123456789ABCDEF) a value 0 is returned
		/// </summary>
		/// <param name="c">The char to convert</param>
		/// <returns>The binary value, an exception if the char is not hexadecimal compatible</returns>
		public static byte OneHexToBin(char c)
		{
			int i = HEXCHARS.IndexOf(c.ToString().ToUpper());
			if (-1 != i)
				return (byte)i;
			else
				throw new EInvalidFormat($"{c} is not an compatible hexadecimal value");
		}
		/// <summary>
		/// Converts a 2 characters string holding an hexadecimal value to its binary value.
		/// Only the first 2 characters are considered.
		/// A <see cref="EInvalidFormat"/> Exception is raised if:
		///  - The string is less than 2 characters
		///  - The string contains invalid characters
		/// </summary>
		/// <param name="s">The string to convert</param>
		/// <returns>The binary value of valid chars composing the 2 chars string, 0 if the string char is not hexadecimal compatible</returns>
		public static byte TwoHexToBin(string s)
		{
			if (2 > s.Length)
				throw new EInvalidFormat($"Invalid length");
			// convert hex value (on 2 positions) to byte
			try
			{
				s = s.ToUpper();
				byte p = OneHexToBin(s[0]);
				byte b = (byte)(p * 16);
				p = OneHexToBin(s[1]);
				b += p;
				return b;
			}
			catch (EInvalidFormat)
			{
				throw new EInvalidFormat($"{s} is not an compatible hexadecimal value");
			}
		}
		private const string HEXCHARS = "0123456789ABCDEF";
		/// <summary>
		/// Converts a numric value to it hexadecimal representation
		/// This function may throw an exception
		/// </summary>
		/// <param name="v">Value to convert</param>
		/// <param name="minlen">The minimum number of characters inside the string (completed with 0 on the left if necessary), 0 means no minimum length</param>
		/// <returns>A string with the hexadecimal representation of the passed value or an exception if an error occurs</returns>
		public static string ValueToHex(decimal v, int minlen = 0)
		{
			string s = null;
			while (0 != v)
			{
				int f = (int)(v % 16);
				s = f.ToString("X") + s;
				v = (v - f) / 16;
			}
			if (minlen < s.Length)
				s = new string('0', minlen - s.Length) + s;
			return s;
		}
		/// <summary>
		/// Converts an hexadecimal representation to its decimal value
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or an exception if out of range or not a valid hexadecimal string</returns>
		public static decimal HexToDecimal(string s)
		{
			decimal d = 0M;
			try
			{
				foreach (char c in s)
					d = d * 16 + OneHexToBin(c);
				return d;
			}
			catch (EInvalidFormat)
			{
				throw new EInvalidFormat($"{s} is not an compatible hexadecimal value");
			}
		}
		/// <summary>
		/// Converts an hexadecimal representation to its double value
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static double HexToDouble(string s)
		{
			if (sizeof(double) < s.Length) throw new EOutOfRange(s);
			return (double)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its long value
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static long HexToLong(string s)
		{
			if (sizeof(double) < s.Length) throw new EOutOfRange(s);
			return (long)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its int value
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static int HexToInt(string s)
		{
			if (sizeof(int) < s.Length) throw new EOutOfRange(s);
			return (int)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its short value
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static short HexToShort(string s)
		{
			if (sizeof(short) < s.Length) throw new EOutOfRange(s);
			return (short)HexToDecimal(s);
		}
		/// <summary>
		/// Check a string value against an enum type values
		/// </summary>
		/// <param name="type">the enum type to consider</param>
		/// <param name="value">the string to search inside this enum</param>
		/// <param name="defvalue">default value to use (if not null) it the string does not apply to the enum</param>
		/// <returns>the value inside the enum matching the string, defvalue if not null and value not found, <see cref="CMisc.NotEnumValue"/> otherwise</returns>
		public static object StringToEnumValue(Type type, string value, object defvalue = null)
		{
			if (!string.IsNullOrEmpty(value))
			{
				Array array = Enum.GetValues(type);
				foreach (object i in array)
					if (value.ToLower() == EnumValueToString(type, i).ToLower())
						return i;
			}
			return (null != defvalue ? defvalue : NotEnumValue);
		}
		public static int NotEnumValue = int.MaxValue;
		/// <summary>
		/// Get the name of a value inside an enum
		/// </summary>
		/// <param name="type">the enum type to consider</param>
		/// <param name="value">the value to search for</param>
		/// <returns>The name of the value inside the enum if available, an empty string otherwise</returns>
		public static string EnumValueToString(Type type, object value)
		{
			try
			{ return Enum.GetName(type, value); }
			catch (Exception)
			{ return null; }
		}
		/// <summary>
		/// Indicates whether a value is contained inside an enum type
		/// </summary>
		/// <param name="type">enum type to consider</param>
		/// <param name="value">value to search inside the enum</param>
		/// <returns>true if the value is contained inside the enum type, false otherwise</returns>
		public static bool IsEnumValue(Type type, object value)
		{
			if (null != value)
				try
				{
					Array array = Enum.GetValues(type);
					foreach (object i in array)
						if (value.Equals(i))
							return true;
				}
				catch (Exception) { }
			// arrived here the value isn't inside the enum
			return false;
		}
		/// <summary>
		/// Returns the simples form of a string
		/// </summary>
		/// <param name="s"></param>
		/// <returns>a trimmed and set to lowercase string or null if string is empty</returns>
		public static string Trimmed(string s)
		{
			if (string.IsNullOrEmpty(s))
				return null;
			s = s.Trim();
			if (string.IsNullOrEmpty(s))
				return null;
			return s;
		}
		/// <summary>
		/// Returns the simples form of a string
		/// </summary>
		/// <param name="s"></param>
		/// <returns>a trimmed and set to lowercase string or null if string is empty</returns>
		public static string Lowered(string s)
		{
			s = CMisc.Trimmed(s);
			if (string.IsNullOrEmpty(s))
				return null;
			return s.ToLower();
		}
		/// <summary>
		/// Makes sure a string is always returned
		/// </summary>
		/// <param name="s">The string to test</param>
		/// <returns></returns>
		public static string AsString(string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : s;
		}
		/// <summary>
		/// Safely return a string content, even if the original string is null
		/// </summary>
		/// <param name="s">The string to test</param>
		/// <returns>The string or an empty string if that string doesn't exist (is null)</returns>
		public static string IsString(string s)
		{
			if (string.IsNullOrEmpty(s))
				return null;
			return s;
		}
		/// <summary>
		/// Get the different versions included inside the module
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Version(VersionType type = VersionType.assemblyInfo)
		{
			switch (type)
			{
				case VersionType.assemblyFile:
					return System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
				case VersionType.assembly:
					return Assembly.GetExecutingAssembly().GetName().Version.ToString();
				case VersionType.assemblyInfo:
				default:
					return System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
			}
		}
		public enum VersionType
		{
			assembly,
			assemblyFile,
			assemblyInfo,
		}
		/// <summary>
		/// Get the value associated to an enum entry from its name
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="s">The name of the value</param>
		/// <param name="def">Default value to use, if null <see cref="CMisc.UNKNOWN"/> will be used</param>
		/// <returns>The value represented by the string inside the enum, UNKNOWN if the string does not exist inside the enum</returns>
		public static object GetEnumValue(Type T, string s, object def = null)
		{
			if (null == def)
				def = (object)UNKNOWN;
			if (!string.IsNullOrEmpty(s))
				try
				{
					if (Enum.IsDefined(T, s))
						return Enum.Parse(T, s);
				}
				catch (Exception) { }
			return def;
		}
		/// <summary>
		/// Get the litteral name of an enum value
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to find</param>
		/// <param name="def">Default value to use, if null <see cref="CMisc.UNKNOWN"/> will be used</param>
		/// <returns>The litteral string describing the value inside the enum, null if it does not exist</returns>
		public static string GetEnumName(Type T, object value, object def = null)
		{
			if (null == def)
				def = (object)UNKNOWN;
			if (null != value && def != value)
				try
				{ return Enum.GetName(T, value); }
				catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Verify a value is valid inside an enumeration
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to verify</param>
		/// <param name="def">Default value to use, if null <see cref="CMisc.UNKNOWN"/> will be used</param>
		/// <returns>"value" if it is inside the enumeration, "def" is not inside the enumeration</returns>
		public static object GetEnumValue(Type T, object value, object def = null)
		{
			if (Enum.IsDefined(T, value))
				return value;
			return def;
		}
		/// <summary>
		/// Allows verifying a folder exists, eventually with write privileges if required
		/// </summary>
		/// <param name="dir">Folder to verify existence</param>
		/// <param name="addtrailer">Indicates whether the returned value must contain a final "\" or not</param>
		/// <param name="writeaccess">Indicates whether write privilege is required or not</param>
		/// <returns>The folder path (eventually with a #\" trailer if required) if exists with the requested privileges, null otherwise</returns>
		public static string VerifyDirectory(string dir, bool addtrailer, bool writeaccess = true)
		{
			string final = dir, fullfinal;
			// chech whether directory exists or not
			if (Directory.Exists(final))
			{
				fullfinal = final;
				// add "\" if required
				if (!final.EndsWith(@"\"))
					fullfinal += @"\";
				if (addtrailer)
					final = fullfinal;

				// test write access if required
				if (!writeaccess)
					return final;
				// try creating a temp file with write access (then delete it)
				string s = Path.GetRandomFileName();
				try
				{
					string sdir = fullfinal + s;
					FileStream fs = File.Open(sdir, FileMode.CreateNew);
					// arrived here write access is granted
					fs.Close();
					File.Delete(sdir);
					return final;
				}
				catch (Exception) { }
				// arrived here write access is not granted
				return null;
			}
			return null;
		}
	}
}
