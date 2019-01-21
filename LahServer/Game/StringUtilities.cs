using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LahServer.Game
{
	internal static class StringUtilities
	{
		public static string SanitizeClientString(string rawClientString, int truncateLength, ref bool isXssProbable, Func<char, bool> validCharPredicate)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(rawClientString);

			isXssProbable |= doc.DocumentNode.ChildNodes.Count > 1 || !(doc.DocumentNode.FirstChild is HtmlTextNode);

			var sanitizedString = Regex.Replace(
				new string(doc.DocumentNode.InnerText.Truncate(truncateLength).Where(c => validCharPredicate(c)).ToArray()),
				@"\s\s+", " ").Trim();

			return sanitizedString;
		}
	}
}
