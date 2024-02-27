
using System.Collections.Generic;
using Hex.Data;

namespace Hex.Model
{
	public class BattleModel
	{
		public readonly List<UnitData> Hand = new();
		public readonly List<UnitData> Deck = new();
		public readonly List<UnitData> Discard = new();
		
		public bool IsHandEmpty => Hand.Count == 0;

		public int RemainingUnitMoves;
	}
}