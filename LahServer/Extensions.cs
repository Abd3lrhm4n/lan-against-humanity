using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer
{
	internal static class Extensions
	{
		public static T Do<T>(this T value, Func<T, T> action) => action(value);

		public static string Truncate(this string value, int maxLength)
		{
			if (value == null) return null;
			if (maxLength <= 0) return value;
			return value.Substring(0, Math.Min(value.Length, maxLength));
		}

		public static string LimitedConcat(this string[] substrings, int limit = -1, string separator = "")
		{
			if (substrings == null)
			{
				throw new ArgumentNullException(nameof(substrings));
			}

			var sb = new StringBuilder();
			int n = limit >= 0 ? Math.Min(limit, substrings.Length) : substrings.Length;
			for(int i = 0; i < n; i++)
			{
				if (i > 0) sb.Append(separator);
				sb.Append(substrings[i]);
			}

			return sb.ToString();
		}

		public static string[] SplitTrim(this string value, char[] separators, StringSplitOptions options)
		{
			return value.Split(separators, options).Select(s => s.Trim()).Where(s => options == StringSplitOptions.RemoveEmptyEntries ? !String.IsNullOrWhiteSpace(s) : true).ToArray();
		}
	}
}
