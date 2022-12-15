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
		public const string REGEX_HEADER = @"^";
		public const string REGEX_TRAILER = @"$";
		public const string REGEX_IPV4_ADDRESS_PART = @"([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])";
		public const string REGEX_IPV4_PORT_NUMBER_PART = @":([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])";
		public const string REGEX_IPV4_PARTS_1TO3 = "(" + REGEX_IPV4_ADDRESS_PART + @"\.){3}";
		public const string REGEX_IPV4_PART_4 = REGEX_IPV4_ADDRESS_PART;
		public const string REGEX_IPV4_WITHOUT_PORT = REGEX_HEADER + REGEX_IPV4_PARTS_1TO3 + REGEX_IPV4_PART_4 + REGEX_TRAILER;
		public const string REGEX_IPV4_WITH_PORT = REGEX_IPV4_WITHOUT_PORT + REGEX_IPV4_PORT_NUMBER_PART + REGEX_TRAILER;
		public const string REGEX_URL_CHARACTER_SET = @"^(?i)([a-z0-9]|[a-z0-9][a-z0-9\-]{0,61}[a-z0-9])(\.([a-z0-9]|[a-z0-9][a-z0-9\-]{0,61}[a-z0-9]))*$";
		public const string REGEX_URL_WITHOUT_PORT = REGEX_HEADER + REGEX_URL_CHARACTER_SET + "+" + REGEX_TRAILER;
		public const string REGEX_URL_WITH_PORT = REGEX_HEADER + REGEX_URL_CHARACTER_SET + "+" + REGEX_IPV4_PORT_NUMBER_PART + REGEX_TRAILER;
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
				throw new ArgumentException("String cannot be of zero length");
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
		/// <summary>
		/// Indicates whether a byte[] is null or empty
		/// </summary>
		/// <param name="ab">The byte[] data to verify</param>
		/// <returns>True if null or Length=0, false otherwise</returns>
		public static bool IsNullOrEmpty(this byte[] ab)
		{
			return (null == ab || 0 == ab.Length);
		}
		/// <summary>
		/// Indicates whether a string is null or empty
		/// </summary>
		/// <param name="s">The string to verify</param>
		/// <returns>True if null or Length=0, false otherwise</returns>
		public static bool IsNullOrEmpty(this string s)
		{
			return string.IsNullOrEmpty(s);
		}
		/// <summary>
		/// Compares the current string with another one
		/// </summary>
		/// <param name="s">The string to compare</param>
		/// <param name="value">The string to match to</param>
		/// <param name="ignoreCase">true if case must be ignored, false otherwise</param>
		/// <returns>true if both strings are equal, false otherwise</returns>
		public static bool Compare(this string s, string value, bool ignoreCase = true)
		{
			return 0 == string.Compare(s, value, ignoreCase);
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
		///// <summary>
		///// Converts an array of bytes to a UTF-8 string (if possible)
		///// </summary>
		///// <param name="buffer">The array of bytes to convert</param>
		///// <returns>The converted array into a UTF-8 string if successful, an empty string if any error occured</returns>
		//public static string BytesToStr(byte[] buffer)
		//{
		//	if (null == buffer || 0 == buffer.Length) return null;
		//	string res = string.Empty;
		//	try
		//	{
		//		res = Encoding.UTF8.GetString(buffer);
		//	}
		//	catch (Exception) { res = null; }
		//	return res;
		//}
		/// <summary>
		/// Safe string to int function
		/// </summary>
		/// <param name="s">The string to convert to int</param>
		/// <param name="defv">Default value if the string can't convert to a long</param>
		/// <param name="alwayspositive">Indicates whether the value must always be positive or not</param>
		/// <returns>The value of the string, 0 if an error occured</returns>
		public static long StrToLong(string s, long defv = 0, bool alwayspositive = false)
		{
			long i = 0;
			if (string.IsNullOrEmpty(s)) return 0;
			try
			{
				i = long.Parse(s);
				if (alwayspositive && 0 > i)
					i = -i;
			}
			catch (Exception) { i = defv; }
			return i;
		}
		/// <summary>
		/// Copy bytes from short integral type to byte[].
		/// The array of bytes is 2 bytes long (size of short).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of bytes</param>
		/// <param name="optimize">If true the created buffer is optimized removing the bytes on the right set to 0, false means the buffer length is according to the length of the <paramref name="value"/> type</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(short value, bool optimize = false)
		{
			//byte[] bb = BitConverter.GetBytes(value);
			//if (BitConverter.IsLittleEndian)
			//	Array.Reverse(bb);
			//return bb;
			return SetBytesFromIntegralTypeValue(value, sizeof(short), optimize);
		}
		/// <summary>
		/// Copy bytes from int integral type to byte[].
		/// The array of bytes is 4 bytes long (size of int).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of bytes</param>
		/// <param name="optimize">If true the created buffer is optimized removing the bytes on the right set to 0, false means the buffer length is according to the length of the <paramref name="value"/> type</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(int value, bool optimize = false)
		{
			//byte[] bb = BitConverter.GetBytes(value);
			//if (BitConverter.IsLittleEndian)
			//	Array.Reverse(bb);
			//return bb;
			return SetBytesFromIntegralTypeValue(value, sizeof(int), optimize);
		}
		/// <summary>
		/// Copy bytes from long integral type to byte[].
		/// The array of bytes is 8 bytes long (size of long).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of bytes</param>
		/// <param name="optimize">If true the created buffer is optimized removing the bytes on the right set to 0, false means the buffer length is according to the length of the <paramref name="value"/> type</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(long value, bool optimize = false)
		{
			//byte[] bb = BitConverter.GetBytes(value);
			//if (BitConverter.IsLittleEndian)
			//	Array.Reverse(bb);
			//return bb;
			return SetBytesFromIntegralTypeValue(value, sizeof(long), optimize);
		}
		/// <summary>
		/// Copy bytes from long integral type to byte[].
		/// The array of bytes is 8 bytes long (size of long).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of bytes</param>
		/// <param name="maxsize">The maximum size of the expected buffer</param>
		/// <param name="optimize">If true the created buffer is optimized removing the bytes on the right set to 0, false means the buffer length is according to the length of the <paramref name="value"/> type</param>
		/// <returns>The array of bytes created after copying the integral type</returns>
		public static byte[] SetBytesFromIntegralTypeValue(long value, int maxsize, bool optimize)
		{
			if (sizeof(long) < maxsize) return new byte[0];

			byte[] bb = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bb);

			byte[] bbx = new byte[maxsize];
			// copy the buffer according to its expected maximum size (taking only the final part)
			Buffer.BlockCopy(bb, bb.Length - maxsize, bbx, 0, bbx.Length);
			bb = bbx;

			// if optimization has been requested remove all trailing bytes set to 0
			if (optimize)
			{
				int i = 0;
				int counter = 0;
				// determine the number of bytes set to 0 at the beginning of the buffer taking care to always keep at least 1 byte (the most right one)
				while (i < bb.Length - 1 && 0x00 == bb[i++]) counter++;
				bbx = new byte[bb.Length - counter];
				// copy the buffer according to its expected maximum size (taking only the final part)
				Buffer.BlockCopy(bb, counter, bbx, 0, bbx.Length);
				bb = bbx;
			}
			return bb;
		}
		/// <summary>
		/// Copy bytes from long integral type to byte[].
		/// The array of bytes is 8 bytes long (size of long).
		/// This function is useful to transform an integral type to bytes
		/// </summary>
		/// <param name="value">The integral type to copy to an array of bytes</param>
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
			return new byte[maxlen];
		}
		/// <summary>
		/// Get integral long value value from an array of bytes where each byte (up to 8 bytes) represents a part of the integral value.
		/// This function is useful to retrieve an integral value from a set of bytes
		/// </summary>
		/// <param name="buffer">The array of bytes to analyse</param>
		/// <returns>A long describing the value stored inside the array of bytes, 0 otherwise</returns>
		public static long GetIntegralTypeValueFromBytes(byte[] buffer)
		{
			return GetIntegralTypeValueFromBytes(buffer, 0, buffer.Length);
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
		/// <param name="start">The 0 based starting position, inside the array of bytes, to get the value</param>
		/// <param name="maxlen">The number of bytes to use to build the integral value</param>
		/// <returns>A long describing the value stored inside the array of bytes, 0 otherwise</returns>
		public static long GetIntegralTypeValueFromBytes(byte[] buffer, int start, int maxlen = CMisc.FOURBYTES)
		{
			if (null == buffer || 0 == buffer.Length || buffer.Length <= start || maxlen > sizeof(long)) return 0;
			long l = 0;
			maxlen = Math.Min(buffer.Length - start, maxlen);
			byte[] ab = new byte[maxlen];
			Buffer.BlockCopy(buffer, start, ab, 0, maxlen);
			foreach (byte b in ab)
			{
				l += (long)b << 8 * (maxlen - 1);
				maxlen--;
			}
			return l;
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than 1, setting it to 1, or maximum is higher than <paramref name="maxv"/> (defaulting to 2147483647)
		/// </summary>
		/// <param name="min">Minimum length to use</param>
		/// <param name="max">Maximum length to use</param>
		/// <param name="maxv">The maximum to not go over (default is 2147483647)</param>
		public static void AdjustMinMax1N(ref int min, ref int max, int maxv = int.MaxValue)
		{
			if (0 == maxv) maxv = 1;
			else if (1 > maxv) maxv = -maxv;
			AdjustMinMax(ref min, ref max, 1, maxv);
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than 0, setting it to 0, or maximum is higher than <paramref name="maxv"/> (defaulting to 2147483647)
		/// </summary>
		/// <param name="min">Minimum length to use</param>
		/// <param name="max">Maximum length to use</param>
		/// <param name="maxv">The maximum to not go over (default is 2147483647)</param>
		public static void AdjustMinMax0N(ref int min, ref int max, int maxv = int.MaxValue)
		{
			if (0 > maxv) maxv = -maxv;
			AdjustMinMax(ref min, ref max, 0, maxv);
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// It also changes boundaries if minimum is less than <paramref name="minv"/> (defaulting to 0) or maximum is higher than <paramref name="maxv"/> (defaulting to 2147483647)
		/// </summary>
		/// <param name="min">Minimum value</param>
		/// <param name="max">Maximum value</param>
		/// <param name="minv">The minimum to not go under (default is 0)</param>
		/// <param name="maxv">The maximum to not go over (default is 2147483647)</param>
		public static void AdjustMinMax(ref int min, ref int max, int minv = 0, int maxv = int.MaxValue)
		{
			int i;
			// test minimum and maximum, interverting them if necessary
			if (maxv < minv)
			{
				i = maxv;
				maxv = minv;
				minv = i;
			}
			// check min and max, interverting them if necessary
			if (max < min)
			{
				i = max;
				max = min;
				min = i;
			}
			// check boundaries
			if (minv > min)
				min = minv;
			if (maxv < max)
				max = maxv;
		}
		/// <summary>
		/// Adjust min and max value (inverting them if necessary).
		/// Throws an Exception if the array of bytes length does not comply with the min and max bounds
		/// </summary>
		/// <param name="value">The array of bytes to check</param>
		/// <param name="minlen">The minimum length</param>
		/// <param name="maxlen">The maximum length</param>
		public static void CheckBufferMinMax1N(byte[] value, ref int minlen, ref int maxlen)
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
		public static void CheckBufferMinMax1N(string value, ref int minlen, ref int maxlen)
		{
			AdjustMinMax1N(ref minlen, ref maxlen);
			if (value.Length < minlen || value.Length > maxlen)
				throw new Exception("Invalid length - Value: " + value + " - Min length: " + minlen.ToString() + " ; Max length: " + maxlen.ToString() + " ; Actual length: " + value.Length.ToString());
		}
		/// <summary>
		/// Help deciding the length to use when manipulating an array of bytes, according to the min and max considered.
		/// If the buffer is empty or is out of bounds of minlen and maxlen the length to consider is 0, otherwise it is the buffer length.
		/// Beware, the function will invert minlen and maxlen if they are not accurate
		/// </summary>
		/// <param name="buffer">Buffer to evaluate</param>
		/// <param name="minlen">Minimum length to use</param>
		/// <param name="maxlen">Maximum length to use</param>
		/// <returns>The length to use, buffer length if between boundaries, 0 otherwise</returns>
		public static int LenToUse(byte[] buffer, ref int minlen, ref int maxlen)
		{
			AdjustMinMax1N(ref minlen, ref maxlen);
			if (null == buffer || 0 == buffer.Length) return 0;
			int len = buffer.Length;
			if (len >= minlen && len <= maxlen)
				return len;
			else
				return 0;
		}
		/// <summary>
		/// Test whether a string is composed according to a specified regular expression
		/// This function does not pre-process the mask with start and end line
		/// </summary>
		/// <param name="value">The value to test</param>
		/// <param name="pattern">The regular expression to match. The regular expression must be complete and well formatted</param>
		/// <param name="validIfEmpty">TRUE if an empty string bypasses the verification (en empty string is always valid), FALSE otherwise</param>
		/// <param name="confidential">Indicates whether the passed value is confidential or not, thuis displayable in the log file</param>
		/// <returns>TRUE if the value complies with the regular expression (or is empty if allowed), FALSE otherwise</returns>
		public static bool IsValidFormat(string value, string pattern, bool validIfEmpty = false, bool confidential = false)
		{
			if (string.IsNullOrEmpty(value) && validIfEmpty)
				return true;
			else if (string.IsNullOrEmpty(value))
				return false;
			pattern = AsString(pattern);
			CLog.DEBUG($"{(confidential ? "<VALUE HIDDEN>" : $"Input data: {value}")} - Pattern: {pattern}");
			Match match = Regex.Match(value, pattern);
			return match.Success;
		}
		/// <summary>
		/// Test whether a string is composed according to a specified character set and length complies with specified bounds
		/// This function processes the regular expression as a whole word adding "start line" (^) and "end line" ($)
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <param name="characterSet">The character set the value must comply with</param>
		/// <param name="minlen">The minimum length the value must comply with</param>
		/// <param name="maxlen">The maximum length the value must comply with</param>
		/// <param name="validIfEmpty">TRUE if an empty string bypasses the verification (en empty string is always valid), FALSE otherwise</param>
		/// <param name="confidential">Indicates whether the passed value is confidential or not, thuis displayable in the log file</param>
		/// <returns>TRUE if the value complies with the regular expression (or is empty if allowed), FALSE otherwise</returns>
		public static bool IsValidFormat(string value, string characterSet, int minlen, int maxlen, bool validIfEmpty = false, bool confidential = false)
		{
			if (string.IsNullOrEmpty(value) && validIfEmpty)
				return true;
			else if (string.IsNullOrEmpty(value))
				return false;
			if (string.IsNullOrEmpty(characterSet)) return false;
			// build regular expression to check against
			string count = "{" + (minlen == maxlen ? minlen.ToString() + "}" : minlen.ToString() + "," + maxlen.ToString() + "}");
			string pattern = $"^{characterSet}{count}$";
			CLog.DEBUG($"{(confidential ? "<VALUE HIDDEN>" : $"Input data: {value}")} - Pattern: {pattern}");
			Match match = Regex.Match(value, pattern);
			return match.Success;
		}
		/// <summary>
		/// Converts a char holding an hexadecimal value to its binary value (A=10,...).
		/// A <see cref="EInvalidFormat"/> Exception is raised if:
		///  - The character is not a valid hexadecimal one
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
			if (string.IsNullOrEmpty(s)) return 0;
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
		/// Converts a string representing an hexadecimal value to an array of bytes
		/// If the string length is odd ot is padded with a 0 at the beginning or the end of the string, according to <paramref name="padRight"/>
		/// </summary>
		/// <param name="s">Hexadecimal string to convert</param>
		/// <param name="padded">[OUT] true if a padding was made (according to <paramref name="padRight"/>), false otherwise</param>
		/// <param name="padRight">True is padding should be made on the right of the passed string (<paramref name="s"/>), false if it must be made on the left</param>
		/// <returns></returns>
		public static byte[] HexToBin(string s, out bool padded, bool padRight = true)
		{
			padded = false;
			if (string.IsNullOrEmpty(s)) return new byte[0];
			if (0 != s.Length % 2)
			{
				padded = true;
				s = (padRight ? s + "0" : "0" + s);
			}
			byte[] ab = new byte[(s.Length / 2)];
			for (int i = 0; i < ab.Length; i++)
			{
				// key has arrived under the form of a 64 chars hexadecimal string, we convert it to 32 bytes binary chars
				ab[i] = TwoHexToBin(s.Substring(i * 2, 2).ToUpper());
			}
			return ab;
		}
		///// <summary>
		///// Converts an array of bytes to an hexadecimal string
		///// Each byte gives a 2 chars hexadecimal value
		///// </summary>
		///// <param name="buffer">The array of bytes to convert</param>
		///// <returns>The converted array into a string if successful, an empty string if any error occured</returns>
		//public static string BinToHex(byte[] buffer)
		//{
		//	if (null == buffer || 0 == buffer.Length) return null;
		//	string res = string.Empty;
		//	try
		//	{
		//		foreach (byte b in buffer)
		//			res += b.ToString("X2");
		//	}
		//	catch (Exception) { res = null; }
		//	return res;
		//}
		/// <summary>
		/// Converts a numric value to it hexadecimal representation
		/// THIS FUNCTION MAY THROW AN EXCEPTION
		/// </summary>
		/// <param name="v">Value to convert</param>
		/// <param name="minlen">The minimum number of characters inside the string (completed with 0 on the left if necessary), 0 means no minimum length</param>
		/// <param name="oddLengthAllowed">True if an odd number of characters can represent the hexadecimal string, false otherwise (default)</param>
		/// <returns>A string with the hexadecimal representation of the passed value or an exception if an error occurs</returns>
		public static string ValueToHex(decimal v, int minlen = 0, bool oddLengthAllowed = false)
		{
			string s = string.Empty;
			while (0 != v)
			{
				int f = (int)(v % 16);
				s = f.ToString("X") + s;
				v = (v - f) / 16;
			}
			if (0 != minlen && minlen > s.Length) s = new string('0', minlen - s.Length) + s;
			if (!oddLengthAllowed && 0 != s.Length % 2) s = "0" + s;
			return s;
		}
		/// <summary>
		/// Converts an hexadecimal representation to its decimal value
		/// THIS FUNCTION MAY THROW AN EXCEPTION <see cref="EInvalidFormat"/>
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or an exception if out of range or not a valid hexadecimal string</returns>
		public static decimal HexToDecimal(string s)
		{
			if (string.IsNullOrEmpty(s)) return 0M;
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
		/// THIS FUNCTION MAY THROW AN EXCEPTION <see cref="EOutOfRange"/> or <see cref="EInvalidFormat"/>
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static double HexToDouble(string s)
		{
			return (double)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its long value
		/// THIS FUNCTION MAY THROW AN EXCEPTION <see cref="EOutOfRange"/> or <see cref="EInvalidFormat"/>
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static long HexToLong(string s)
		{
			return (long)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its int value
		/// THIS FUNCTION MAY THROW AN EXCEPTION <see cref="EOutOfRange"/> or <see cref="EInvalidFormat"/>
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static int HexToInt(string s)
		{
			return (int)HexToDecimal(s);
		}
		/// <summary>
		/// Converts an hexadecimal representation to its short value
		/// THIS FUNCTION MAY THROW AN EXCEPTION <see cref="EOutOfRange"/> or <see cref="EInvalidFormat"/>
		/// </summary>
		/// <param name="s">hexadecimal string</param>
		/// <returns>Expected value or <see cref="EOutOfRange"/> exception or <see cref="EInvalidFormat"/> exception if not a valid hexadecimal string</returns>
		public static short HexToShort(string s)
		{
			return (short)HexToDecimal(s);
		}
		/// <summary>
		/// Indicates whether a value is contained inside an enum type
		/// </summary>
		/// <param name="T">enum type to consider</param>
		/// <param name="value">value to search inside the enum</param>
		/// <returns>true if the value is contained inside the enum type, false otherwise</returns>
		public static bool IsEnumValue(Type T, object value)
		{
			if (null != value)
				try
				{
					return Enum.IsDefined(T, value);
				}
				catch (Exception) { }
			// arrived here the value isn't inside the enum
			return false;
		}
		/// <summary>
		/// Get the litteral name of an enum value
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to find</param>
		/// <returns>The litteral string describing the value inside the enum, null if it does not exist</returns>
		public static string GetEnumName(Type T, object value)
		{
			if (null != value)
				try
				{
					return Enum.GetName(T, value);
				}
				catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Get the litteral name of an enum value
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to find</param>
		/// <returns>The litteral string describing the value inside the enum, null if it does not exist</returns>
		public static string EnumGetName(Type T, object value) { return GetEnumName(T, value); }
		/// <summary>
		/// Verify whether a value is valid inside an enumeration
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to verify</param>
		/// <param name="defv">Default value to use if <paramref name="value"/> to search is not found; if null then <see cref="CMisc.UNKNOWN"/> will be used as the default value</param>
		/// <returns><paramref name="value"/> if it is inside the enumeration, <paramref name="defv"/> is not inside the enumeration</returns>
		public static object GetEnumValue(Type T, string value, object defv = null)
		{
			try
			{
				//Array array = Enum.GetValues(T);
				//foreach (object i in array)
				//	if (0 == string.Compare(value, i.ToString(), true))
				//		return i;
				return Enum.Parse(T, value, true);
			}
			catch (Exception) { }
			return (defv ?? UNKNOWN);
		}
		/// <summary>
		/// Verify whether a value is valid inside an enumeration
		/// </summary>
		/// <param name="T">The enum type to check against</param>
		/// <param name="value">The value to verify</param>
		/// <param name="defv">Default value to use if <paramref name="value"/> to search is not found; if null then <see cref="CMisc.UNKNOWN"/> will be used as the default value</param>
		/// <returns><paramref name="value"/> if it is inside the enumeration, <paramref name="defv"/> is not inside the enumeration</returns>
		public static object EnumGetValue(Type T, string value, object defv = null) { return GetEnumValue(T, value, defv); }
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
			s = Trimmed(s);
			if (string.IsNullOrEmpty(s))
				return null;
			return s.ToLower();
		}
		/// <summary>
		/// Makes sure a string is always returned and safe
		/// </summary>
		/// <param name="s">The string to test</param>
		/// <returns>The string  or an empty string</returns>
		public static string AsString(string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : s;
		}
		/// <summary>
		/// Returns the string inside an array of bytes
		/// </summary>
		/// <param name="ab">Arry of bytes to llok a string for</param>
		/// <param name="asUtf8">True to generate a UTF8 string (default), false to generate an ASCII string</param>
		/// <returns>The string contained inside the array of bytes or an empty string</returns>
		public static string AsString(byte[] ab, bool asUtf8 = true)
		{
			string s = string.Empty;
			try
			{
				s = asUtf8 ? Encoding.UTF8.GetString(ab) : Encoding.ASCII.GetString(ab);
			}
			catch (Exception) { }
			return s;
		}
		/// <summary>
		/// Converts an array of bytes to an hexadecimal string
		/// Each byte gives a 2 chars hexadecimal value
		/// </summary>
		/// <param name="buffer">The array of bytes to convert</param>
		/// <returns>The converted array into a string if successful, an empty string if any error occured</returns>
		public static string AsHexString(byte[] buffer)
		{
			if (null == buffer || 0 == buffer.Length) return null;
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
		///// <summary>
		///// Get the value associated to an enum entry from its name
		///// </summary>
		///// <param name="T">The enum type to check against</param>
		///// <param name="s">The name of the value</param>
		///// <param name="def">Default value to use, if null <see cref="CMisc.UNKNOWN"/> will be used</param>
		///// <returns>The value represented by the string inside the enum, UNKNOWN if the string does not exist inside the enum</returns>
		//public static object GetEnumValue(Type T, string s, object def = null)
		//{
		//	if (null == def)
		//		def = (object)UNKNOWN;
		//	if (!string.IsNullOrEmpty(s))
		//		try
		//		{
		//			if (Enum.IsDefined(T, s))
		//				return Enum.Parse(T, s);
		//		}
		//		catch (Exception) { }
		//	return def;
		//}
		/// <summary>
		/// Allows verifying a folder exists, eventually with write privileges if required
		/// </summary>
		/// <param name="dir">Folder to verify existence; it can contain a file name; if null or empty the current folder is assumed</param>
		/// <param name="addtrailer">Indicates whether the returned value must contain a final "\" or not</param>
		/// <param name="writeaccess">Indicates whether write privilege is required or not</param>
		/// <returns>The folder path (eventually with a #\" trailer if required) if exists with the requested privileges, null otherwise</returns>
		public static string VerifyDirectory(string dir, bool addtrailer, bool writeaccess = true)
		{
			//string final = dir, fullfinal;
			//try
			//{
			//	final = Path.GetDirectoryName(dir);
			//}
			//catch (Exception)
			//{
			//	final = null;
			//}
			//// chech whether directory exists or not
			//if (Directory.Exists(final))
			//{
			//	fullfinal = final;
			//	// add "\" if required
			//	string sep = new string(Path.DirectorySeparatorChar, 1);
			//	if (!final.EndsWith(sep))
			//		fullfinal += sep;
			//	if (addtrailer)
			//		final = fullfinal;

			//	// test write access if required
			//	if (!writeaccess)
			//		return final;
			//	// try creating a temp file with write access (then delete it)
			//	string s = Path.GetRandomFileName();
			//	try
			//	{
			//		string sdir = fullfinal + s;
			//		FileStream fs = File.Open(sdir, FileMode.CreateNew);
			//		// arrived here write access is granted
			//		fs.Close();
			//		File.Delete(sdir);
			//		return final;
			//	}
			//	catch (Exception) { }
			//	// arrived here write access is not granted
			//	return null;
			//}
			//return null;

			string final, fullfinal;
			try
			{
				final = Path.GetFullPath(dir.IsNullOrEmpty() ? "." : dir);
				if (File.Exists(final))
				{
					FileInfo fi = new FileInfo(final);
					final = fi.DirectoryName;
				}
				else if (!Directory.Exists(final))
				{
					final = string.Empty;
				}
			}
			catch (Exception)
			{
				final = string.Empty;
			}
			// try to determine if a file name or directory name
			if (!final.IsNullOrEmpty())
			{
				fullfinal = final;
				// add "\" if required
				string sep = new string(Path.DirectorySeparatorChar, 1);
				if (!final.EndsWith(sep))
					fullfinal += sep;
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
		/// <summary>
		/// [CONSOLE ONLY] Request a YES/NO answer.
		/// Once requested the naswer entered by the user is a 1 character long string.
		/// This function can also being used to request a digit among 2 (1 or 2 for instance).
		/// </summary>
		/// <param name="msg">The message to display requesting a YES/NO answer without ? at the end.</param>
		/// <param name="useDefault">Indicate whether a default (Y or N) must be proposed</param>
		/// <param name="theDefault">Only if <paramref name="useDefault"/> is true, the default to propose. Beware this value must be consistent with <paramref name="yesvalues"/> and <paramref name="novalues"/> otherwise no default will apply</param>
		/// <param name="useESC">True if ESC can be considered an answer (in that case it will be NO)</param>
		/// <param name="yesvalues">A set of 2 strings indicating the string to display for "YES" and the character meaning YES (i.e. {"Yes", "Y"}, {"Oui", "O"},... If not or partially set the function will use{"YES", "Y"}</param>
		/// <param name="novalues">A set of 2 strings indicating the string to display for "NO" and the character meaning NO (i.e. {"No", "N"}, {"Non", "N"},... If not or partially set the function will use {"NO", "N"}</param>
		/// <param name="displayYesNo">If true the function will display "question (YES=Y/NO=N)", if false it will display  "question (Y/N)", according to <paramref name="yesvalues"/> and <paramref name="novalues"/> values</param>
		/// <returns>True if YES, false if NO</returns>
		public static bool YesNo(string msg, bool useDefault = false, bool theDefault = true, bool useESC = false, string[] yesvalues = null, string[] novalues = null, bool displayYesNo = false)
		{
			const string YES = "YES";
			const string NO = "NO";
			const string Y = "Y";
			const string N = "N";
			string lyes = null == yesvalues || 0 == yesvalues.Length || 0 == yesvalues[0].Length ? YES : yesvalues[0];
			string syes = null == yesvalues || 1 > yesvalues.Length || 0 == yesvalues[1].Length ? Y : yesvalues[1].Substring(0, 1);
			string lno = null == novalues || 0 == novalues.Length || 0 == novalues[0].Length ? NO : novalues[0];
			string sno = null == novalues || 1 > novalues.Length || 0 == novalues[1].Length ? N : novalues[1].Substring(0, 1);
			if (0 == string.Compare(lyes, lno, true))
			{
				lyes = YES;
				lno = NO;
				displayYesNo = true;
			}
			if (0 == string.Compare(syes, sno, true))
			{
				syes = Y;
				sno = N;
				displayYesNo = true;
			}
			string defaultA = theDefault ? syes : sno;
			string confirm = syes + sno;
			string answer = defaultA;
			Console.WriteLine();
			Console.Write(msg + $" ({(displayYesNo ? lyes + "=" : null)}{syes}/{(displayYesNo ? lno + "=" : null)}{sno})" + (useDefault ? $" [{defaultA}]" : null) + " ? ");
			do
			{
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);
				if (useDefault && ConsoleKey.Enter == keyInfo.Key)
				{
					answer = defaultA;
				}
				else if (useESC && ConsoleKey.Escape == keyInfo.Key)
				{
					answer = sno;
				}
				else
				{
					answer = keyInfo.KeyChar.ToString().ToUpper();
				}
			} while (!confirm.Contains(answer));
			return 0 == string.Compare(syes, answer, true);
		}
		/// <summary>
		/// [CONSOLE ONLY] Request an entry with the possibility of a default value
		/// </summary>
		/// <param name="msg">Message to display to request the entry</param>
		/// <param name="defv">Default value (in case of direct ENTER)</param>
		/// <param name="isdef">True if the default value has been chosen</param>
		/// <param name="invite">If something has been entered, the function displays it precedeed by this invite</param>
		/// <returns></returns>
		public static string Input(string msg, string defv, out bool isdef, string invite = null)
		{
			Console.WriteLine();
			Console.Write(msg + (!defv.IsNullOrEmpty() ? $" [{defv}]" : null) + ": ");
			string s = Console.ReadLine();
			if (isdef = (!defv.IsNullOrEmpty() && s.IsNullOrEmpty()))
			{
				s = defv;
				Console.WriteLine(invite + s);
			}
			else if (string.IsNullOrEmpty(s))
				s = null;
			return s;
		}
		/// <summary>
		/// Look for a string option (format can be either "-option" or "/option" in a list of arguments passed to an application
		/// </summary>
		/// <param name="args">List of arguments passed to an application</param>
		/// <param name="option">The option to look for</param>
		/// <param name="index">Index of the option inside the list of arguments, -1 if not found</param>
		/// <param name="occurrence">Occurrence (1 based) of the option in the list of arguments, if the option can be present more than once</param>
		/// <returns>The value of the option if present with the indicated occurrence (it could be an empty string <see cref="string.Empty"/>), null if not present</returns>
		public static string SearchInArgs(string[] args, string option, out int index, int occurrence = 1)
		{
			index = -1;
			int k = 0;
			for (int i = 0; i < args.Length; i++)
			{
				try
				{
					string fulloption;
					if (args[i].StartsWith(fulloption = $"-{option}", true, null) || args[i].StartsWith(fulloption = $"/{option}", true, null))
					{
						// the option has been found, update the occurrence
						k++;
						// is it the occurrence we are looking for ?
						if (occurrence == k)
						{
							// that is th eoption we're looking for
							index = i;
							return args[i].Substring(fulloption.Length);
						}
					}
				}
				catch (Exception) { }
			}
			return null;
		}
		/// <summary>
		/// Build a date to a specied format
		/// </summary>
		/// <param name="fmt">format to use to build the date</param>
		/// <param name="dt">date to use to build the date</param>
		/// <returns>A string representing the date in the desired format</returns>
		public static string BuildDate(DateFormat fmt, DateTime dt = default)
		{
			// if no date was specified, use the current date
			if (default == dt)
				dt = DateTime.Now;
			dt = DateFormat.GMT == fmt ? dt.ToUniversalTime() : dt;
			// build the date with requested format
			switch (fmt)
			{
				case DateFormat.GMT:
					return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
				case DateFormat.Local:
					return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
				case DateFormat.YYYYMMDD:
					return dt.ToString("yyyyMMdd");
				case DateFormat.YYYYMMDDEx:
					return dt.ToString("yyyy-MM-dd");
				case DateFormat.YYYYMMDDhhmmss:
					return dt.ToString("yyyyMMddHHmmssfff");
				case DateFormat.YYYYMMDDhhmmssEx:
					return dt.ToString("yyyy-MM-dd HH:mm:ss");
				case DateFormat.YYYYMMDDhhmmssfff:
					return dt.ToString("yyyyMMddHHmmssfff");
				case DateFormat.YYYYMMDDhhmmssfffEx:
					return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
				case DateFormat.hhmmss:
					return dt.ToString("HHmmss");
				case DateFormat.hhmmssEx:
					return dt.ToString("HH:mm:ss");
				case DateFormat.hhmmssmmm:
					return dt.ToString("HHmmssfff");
				case DateFormat.hhmmssmmmEx:
					return dt.ToString("HH:mm:ss.fff");
				default:
					return string.Empty;
			}
		}
		[ComVisible(true)]
		public enum DateFormat
		{
			GMT,
			Local,
			YYYYMMDD,
			YYYYMMDDEx,
			YYYYMMDDhhmmss,
			YYYYMMDDhhmmssEx,
			YYYYMMDDhhmmssfff,
			YYYYMMDDhhmmssfffEx,
			hhmmss,
			hhmmssEx,
			hhmmssmmm,
			hhmmssmmmEx,
		}
	}
}
