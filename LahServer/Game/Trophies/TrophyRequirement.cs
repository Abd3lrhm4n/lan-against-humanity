using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Trophies
{
	public abstract class TrophyRequirement
	{
		public abstract bool CheckPlay(RoundPlay play);
	}
}
