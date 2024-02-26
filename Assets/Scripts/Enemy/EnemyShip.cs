using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Hex.Data;
using Hex.Grid.Cell;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Hex.Enemy
{
	public class EnemyShip
	{
		[field: SerializeField] public EnemyShipInstance ShipInstance { get; }
		[field: SerializeField] public VisualEffect TargetEffectInstance { get; private set; }
		[field: SerializeField] public int CurrentHealth { get; private set; }
		[field: SerializeField] public int CurrentAttackDamage { get; private set; }
		[field: SerializeField] public int3 CurrentPosition { get; private set; }
		[field: SerializeField] public Vector3 CurrentWorldSpacePosition { get; private set; }
		[field: SerializeField] public HexCell TargetingCell { get; private set; }

		private readonly List<int> _attackPattern;
		private int _lifeTimeCount;

		public void DealDamage(int amount) => CurrentHealth -= amount;

		public bool IsAttacking => CurrentAttackDamage > 0;

		public EnemyShip(EnemyShipInstance shipInstance, HexCell targetingCell, Vector3 startingWorldSpacePos, BattleData.BattleEnemy data, List<int> attackPattern)
		{
			ShipInstance = shipInstance;
			TargetingCell = targetingCell;
			_attackPattern = attackPattern;

			CurrentHealth = data.StartingHealth;
			CurrentPosition = data.StartingPosition;
			CurrentWorldSpacePosition = startingWorldSpacePos;
			CurrentAttackDamage = _attackPattern[0];
		}

		public void Dispose() => ClearTargetInstance();
		
		public void SetTargetInstance(VisualEffect instance)
		{
			ClearTargetInstance();
			TargetEffectInstance = instance;
		}

		public void ClearTargetInstance()
		{
			if (TargetEffectInstance != null)
			{
				Object.Destroy(TargetEffectInstance.gameObject);
			}
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

		public Task MoveTo(HexCell newTarget, Vector3 newWorldSpacePos)
		{
			TargetingCell.InfoHolder.ClearEnemyAttack();
			TargetingCell = newTarget;
			TargetingCell.InfoHolder.HoldEnemyAttack(CurrentAttackDamage, false);

			CurrentPosition = newTarget.Coordinates;
			
			CurrentWorldSpacePosition = newWorldSpacePos;

			return ShipInstance.transform.DOMove(CurrentWorldSpacePosition, 1f).SetEase(Ease.InOutCubic).AsyncWaitForCompletion();
		}
	}
}