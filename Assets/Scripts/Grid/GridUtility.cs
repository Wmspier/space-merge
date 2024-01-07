namespace Hex.Grid
{
	public static class GridUtility
	{
		public static int GetTotalEnemyDamageTaken(HexGrid grid)
		{
			var totalDamage = 0;

			foreach (var (_, cell) in grid.Registry)
			{
				// cell will not resolve damage
				if (!cell.HoldingEnemyAttack) continue;

				var powerDiff = cell.InfoHolder.PlayerPower - cell.InfoHolder.EnemyPower;
				if (powerDiff > 0) totalDamage += powerDiff;
			}
			
			return totalDamage;
		}
		
		public static int GetTotalPlayerDamageTaken(HexGrid grid)
		{
			var totalDamage = 0;

			foreach (var (_, cell) in grid.Registry)
			{
				// cell will not resolve damage
				if (!cell.HoldingEnemyAttack) continue;

				var powerDiff = cell.InfoHolder.EnemyPower - cell.InfoHolder.PlayerPower;
				if (powerDiff > 0) totalDamage += powerDiff;
			}
			
			return totalDamage;
		}
	}
}