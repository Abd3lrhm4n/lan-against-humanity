using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;

namespace LahServer.Game
{
	public sealed class BlackCard : Card
	{
		private int _blankCount = 1;

		[JsonProperty("blanks", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(1)]
		public int BlankCount
		{
			get => _blankCount;
			private set
			{
				_blankCount = value < 1 ? 1 : value;
			}
		}

		public override string ToString() => GetContent(CultureInfo.CurrentCulture.IetfLanguageTag) ?? GetContent("en-US") ?? "???";
	}
}
