using Hex.Enemy;

namespace Hex.Grid
{
	public static class GridUtility
	{
		public static int GetTotalPlayerDamageTaken(HexGrid grid, EnemyAttackManager attackManager)
		{
			var totalDamage = 0;

			foreach (var (_, cell) in grid.Registry)
			{
				var enemyDamage = attackManager.GetEnemyDamageForCoord(cell.Coordinates);
				
				// Not enemy attack on cell
				if (enemyDamage < 0) continue;

				var powerDiff = enemyDamage - cell.InfoHolder.PlayerPower;
				if (powerDiff > 0) totalDamage += powerDiff;
			}
			
			return totalDamage;
		}
	}
}