using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Hex.Data;
using Hex.Grid.Cell;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyShip
	{
		[field: SerializeField] public GameObject ShipInstance { get; private set; }
		[field: SerializeField] public int CurrentHealth { get; private set; }
		[field: SerializeField] public int CurrentAttackDamage { get; private set; }
		[field: SerializeField] public int3 CurrentPosition { get; private set; }
		[field: SerializeField] public Vector3 CurrentWorldSpacePosition { get; private set; }
		[field: SerializeField] public HexCell TargetingCell { get; private set; }

		private readonly List<int> _attackPattern;
		private int _lifeTimeCount;

		public EnemyShip(GameObject shipInstance, HexCell targetingCell, Vector3 startingWorldSpacePos, BattleData.BattleEnemy data, List<int> attackPattern)
		{
			ShipInstance = shipInstance;
			TargetingCell = targetingCell;
			_attackPattern = attackPattern;

			CurrentHealth = data.StartingHealth;
			CurrentPosition = data.StartingPosition;
			CurrentWorldSpacePosition = startingWorldSpacePos;
			CurrentAttackDamage = _attackPattern[0];
		}

		public IEnumerator PlayEnter()
		{
			var enterOrigin = CurrentWorldSpacePosition;
			enterOrigin.y += 50f;

			ShipInstance.transform.position = enterOrigin;
			var tween = ShipInstance.transform.DOMove(CurrentWorldSpacePosition, 1.5f).SetEase(Ease.InOutCubic);

			while (tween.IsActive() && tween.IsPlaying())
			{
				yield return null;
			}
		}

		public void ElapseTurn()
		{
			_lifeTimeCount++;
			CurrentAttackDamage = _attackPattern[Mathf.Min(_attackPattern.Count - 1, _lifeTimeCount)];
		}

		public void MoveTo(HexCell newTarget, Vector3 newWorldSpacePos)
		{
			TargetingCell.InfoHolder.ClearEnemyAttack();
			TargetingCell = newTarget;

			CurrentPosition = newTarget.Coordinates;
			
			CurrentWorldSpacePosition = newWorldSpacePos;
			
			ShipInstance.transform.DOMove(CurrentWorldSpacePosition, 1f).SetEase(Ease.InOutCubic).OnComplete(() =>
			{
				TargetingCell.InfoHolder.HoldEnemyAttack(CurrentAttackDamage);
			});
		}
	}
}