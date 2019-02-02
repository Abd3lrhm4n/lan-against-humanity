using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class Trophy
	{
		public string ID { get; private set; }


	}
}
