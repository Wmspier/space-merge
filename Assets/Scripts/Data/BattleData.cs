using System;
using System.Collections.Generic;
using Hex.Enemy;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Data
{
	[CreateAssetMenu(menuName = "Hex/Battle", fileName = "New_Battle")]
	public class BattleData : ScriptableObject
	{
		[Serializable]
		public class BattleEnemy
		{
			[field: SerializeField] public int StartingHealth { get; private set; }
			[field: SerializeField] public int3 StartingPosition { get; private set; }
			[field: SerializeField] public EnemyShipInstance ShipPrefab { get; private set; }
		}

		[field: SerializeField] public List<BattleEnemy> Enemies { get; private set; } = new();
		[field: SerializeField] public List<int> AttackPattern { get; private set; }
	}
}