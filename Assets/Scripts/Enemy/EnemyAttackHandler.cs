using System.Collections.Generic;
using System.Linq;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyAttackHandler : MonoBehaviour
	{
		private static readonly int[] AttackPowerList = { 3, 5, 8, 12, 17, 23, 30, 38, 47 };

		[SerializeField] private EnemyAttackUI _ui;

		private int _attackPhaseCount;
		private int _staggeredPhaseCount;
		private HexGrid _grid;
		
		public bool IsAttackPhase => _ui.TurnsBeforeAttack == 0;
		
		public bool ElapseTurn() => _ui.ElapseTurn();
		public void ResetTurns() => _ui.ResetTurns();

		public void Initialize(HexGrid grid)
		{
			_grid = grid;
			_ui.ResolveAttackPressed = ResolveAttacks;
		}
		
		public void AssignAttacksToGrid()
		{
			var isEvenAttack = _attackPhaseCount % 2 == 0;
			var numToSpawn = isEvenAttack ? 2 : 3;
			var attackList = new List<int>().FillWithDefault(numToSpawn);
			
			// Always assign at least two attacks using the current attack index
			for (var i = 0; i < 2; i++)
			{
				attackList[i] = AttackPowerList[Mathf.Min(AttackPowerList.Length - 1, Mathf.Max(0, isEvenAttack ? _staggeredPhaseCount : _staggeredPhaseCount-1))];
			}
			
			// If we're on a turn on which 3 attacks spawn, spawn one at a higher index
			if (!isEvenAttack)
			{
				attackList[2] = AttackPowerList[Mathf.Min(AttackPowerList.Length - 1, _staggeredPhaseCount)];
			}

			attackList.Shuffle();

			var cellsByRowWithNoAttack = _grid.GetCellsByRow()
				.Where(cells => cells.All(c => !c.HoldingEnemyAttack))
				.ToList()
				.Shuffle();

			while (attackList.Count > 0)
			{
				HexCell randomCell;
				if (cellsByRowWithNoAttack.Count == 0)
				{
					randomCell = _grid.GetRandomCell();
				}
				else
				{
					// Pick the first row (list should be shuffled)
					var cellsInRow = cellsByRowWithNoAttack[0];
					// Pick a random cell in this row
					randomCell = cellsInRow.Shuffle().First();
					// Remove row
					cellsByRowWithNoAttack.RemoveAt(0);
				}
				
				// Assign attack 
				randomCell.HoldEnemyAttack(attackList[0]);
				// Remove attack
				attackList.RemoveAt(0);
			}

			_attackPhaseCount++;
			if (isEvenAttack) _staggeredPhaseCount++;
		}

		public void ResolveAttacks()
		{
			
		}
	}
}