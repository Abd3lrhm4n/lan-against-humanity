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
	}
}
