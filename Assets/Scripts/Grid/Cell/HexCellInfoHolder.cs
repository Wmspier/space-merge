using Hex.Data;
using Hex.Extensions;
using Hex.Util;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellInfoHolder : MonoBehaviour
	{
		[SerializeField] private HexCellUI _ui;
		
		[field: SerializeField] public Transform UnitAnchor { get; private set; }
		public UnitData HeldUnit { get; private set; }
		public int HeldEnemyAttack { get; private set; } = -1;
		public int CurrentPower { get; private set; }
		public int CurrentRarity { get; private set; }

		private Vector3 _unitAnchorOrigin;
		
		private void Awake()
		{
			_ui.SetPlayerPower(0);
			_ui.SetRarityBaseZero(-1);
			_unitAnchorOrigin = UnitAnchor.localPosition;
		}

		public void SpawnUnit(UnitData unitData, int? withPower = null)
		{
			if (HeldUnit != null)
			{
				Debug.LogWarning("Trying to spawn unit on occupied cell");
				return;
			}

			HeldUnit = unitData;
			Instantiate(unitData.Prefab, UnitAnchor);
			
			CurrentPower = withPower ?? HeldUnit.BasePower;
			CurrentRarity = HeldUnit.BaseRarity;
			
			_ui.ToggleUnitInfoCanvas(true);
			_ui.SetPlayerPower(CurrentPower);
			_ui.SetRarityBaseZero(CurrentRarity);
		}

		public void HoldEnemyAttack(int attackPower)
		{
			HeldEnemyAttack = attackPower;
			_ui.ToggleAttackCanvas(true);
			_ui.SetEnemyAttackPower(attackPower);
		}
		
		public void ResolveCombine(int newPower, int finalRarity, bool resultsInUpgrade, UnitData finalUnitData)
		{
			Debug.Log($"Resolving Combine: NewPower={newPower} | FinalRarity={finalRarity} | ResultsInUpgrade={resultsInUpgrade}");
			
			ClearUnit();
			SpawnUnit(finalUnitData, newPower);

			if (!resultsInUpgrade || CurrentRarity == HexGameUtil.MaxRarityZeroBased)
			{
				// Unit is at max rarity, toggle the anchor to play the spawn anim
				UnitAnchor.gameObject.SetActive(false);
				UnitAnchor.gameObject.SetActive(true);	
				return;
			}

			while (finalRarity > CurrentRarity)
			{
				var nextRarity = HeldUnit.NextRarity;
				
				ClearUnit();
				SpawnUnit(nextRarity, newPower);
			}
			
			CurrentRarity = finalRarity;
			_ui.SetRarityBaseZero(CurrentRarity);
		}

		public int ResolveAttack()
		{
			var powerDifference = CurrentPower - HeldEnemyAttack;
			
			if (powerDifference <= 0)
			{
				// Unit is destroyed
				ClearUnit();
			}
			else
			{
				CurrentPower -= HeldEnemyAttack;
				_ui.SetPlayerPower(CurrentPower);
			}
			
			return powerDifference;
		}

		public void ClearUnit()
		{
			HeldUnit = null;
			CurrentPower = 0;
			CurrentRarity = -1;

			UnitAnchor.DestroyAllChildGameObjects();
			UnitAnchor.localPosition = _unitAnchorOrigin;
			
			_ui.SetPlayerPower(CurrentPower);
			_ui.SetRarityBaseZero(CurrentRarity);
		}

		public void ClearEnemyAttack()
		{
			HeldEnemyAttack = 0;
			_ui.SetEnemyAttackPower(HeldEnemyAttack);
			_ui.ToggleAttackCanvas(false);
		}
		
		public void Clear()
		{
			ClearUnit();
			ClearEnemyAttack();
		}
	}
}