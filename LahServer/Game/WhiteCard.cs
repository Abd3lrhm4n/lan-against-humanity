using System.Globalization;

namespace LahServer.Game
{
	public sealed class WhiteCard : Card
    {

        public override string ToString() => GetContent(CultureInfo.CurrentCulture.IetfLanguageTag) ?? GetContent("en-US") ?? "???";
    }
}
