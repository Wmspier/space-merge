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
		public UnitData HeldPlayerUnit { get; private set; }
		public int EnemyPower { get; private set; } = -1;
		public int PlayerPower { get; private set; }
		public int PlayerRarity { get; private set; }

		private Vector3 _unitAnchorOrigin;

		public void ToggleInfo(bool visible)
		{
			UnitAnchor.gameObject.SetActive(visible);
			_ui.ToggleUnitInfoCanvas(visible);
		}
		
		private void Awake()
		{
			_unitAnchorOrigin = UnitAnchor.localPosition;
		}

		public void SpawnUnit(UnitData unitData, int? withPower = null)
		{
			if (HeldPlayerUnit != null)
			{
				Debug.LogWarning("Trying to spawn unit on occupied cell");
				return;
			}

			if (unitData == null)
			{
				Debug.LogError("Trying to spawn unit with null data");
				return;
			}

			HeldPlayerUnit = unitData;
			Instantiate(unitData.Prefab, UnitAnchor);
			
			PlayerPower = withPower ?? HeldPlayerUnit.BasePower;
			PlayerRarity = HeldPlayerUnit.BaseRarity;
			
			_ui.ToggleUnitInfoCanvas(true);
			_ui.SetPlayerPower(PlayerPower);
			_ui.SetRarityBaseZero(PlayerRarity);
		}

		public void HoldEnemyAttack(int attackPower, bool andShow = true)
		{
			EnemyPower = attackPower;
			_ui.ToggleAttackCanvas(andShow);
			_ui.SetEnemyAttackPower(attackPower);
		}
		
		public void ResolveCombine(int newPower, int finalRarity, bool resultsInUpgrade, UnitData finalUnitData)
		{
			Debug.Log($"Resolving Combine: NewPower={newPower} | FinalRarity={finalRarity} | ResultsInUpgrade={resultsInUpgrade}");
			
			ClearUnit();
			SpawnUnit(finalUnitData, newPower);

			if (!resultsInUpgrade || PlayerRarity == HexGameUtil.MaxRarityZeroBased)
			{
				// Unit is at max rarity, toggle the anchor to play the spawn anim
				UnitAnchor.gameObject.SetActive(false);
				UnitAnchor.gameObject.SetActive(true);	
				return;
			}

			while (finalRarity > PlayerRarity)
			{
				var nextRarity = HeldPlayerUnit.NextRarity;
				
				ClearUnit();
				SpawnUnit(nextRarity, newPower);
			}
			
			PlayerRarity = finalRarity;
			_ui.SetRarityBaseZero(PlayerRarity);
		}

		public int ResolveAttack()
		{
			var powerDifference = PlayerPower - EnemyPower;
			
			if (powerDifference <= 0)
			{
				// Unit is destroyed
				ClearUnit();
			}
			else
			{
				PlayerPower -= EnemyPower;
				_ui.SetPlayerPower(PlayerPower);
			}
			
			return powerDifference;
		}

		public void ClearUnit()
		{
			HeldPlayerUnit = null;
			PlayerPower = 0;
			PlayerRarity = -1;

			UnitAnchor.DestroyAllChildGameObjects();
			UnitAnchor.localPosition = _unitAnchorOrigin;
			
			_ui.SetPlayerPower(PlayerPower);
			_ui.SetRarityBaseZero(PlayerRarity);
		}

		public void ClearEnemyAttack()
		{
			EnemyPower = 0;
			_ui.SetEnemyAttackPower(EnemyPower);
			_ui.ToggleAttackCanvas(false);
		}
		
		public void Clear()
		{
			ClearUnit();
			ClearEnemyAttack();
		}
	}
}