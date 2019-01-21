using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class NameAttribute : Attribute
	{
		public string Name { get; }

		public NameAttribute(string name)
		{
			Name = name;
		}
	}
}
