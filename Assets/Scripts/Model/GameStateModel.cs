using System.Collections.Generic;
using Hex.Enemy;
using Hex.Grid;
using Unity.Mathematics;

namespace Hex.Model
{
	public class GameStateModel
	{
		public readonly HexGrid Grid;
		public readonly Dictionary<int3, EnemyAttackInfo> AttacksByCoord = new();

		public GameStateModel(HexGrid grid)
		{
			Grid = grid;
		}
	}
}