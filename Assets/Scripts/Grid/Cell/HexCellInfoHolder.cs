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
		public int CurrentPower { get; private set; }
		public int CurrentRarity { get; private set; }

		private Vector3 _unitAnchorOrigin;
		
		private void Awake()
		{
			_ui.SetPower(0);
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
			_ui.SetPower(CurrentPower);
			_ui.SetRarityBaseZero(CurrentRarity);
		}

		public void ResolveCombine(int newPower, int finalRarity, bool resultsInUpgrade, UnitData finalUnitData)
		{
			Debug.Log($"Resolving Combine: NewPower={newPower} | FinalRarity={finalRarity} | ResultsInUpgrade={resultsInUpgrade}");
			
			Clear();
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
				
				Clear();
				SpawnUnit(nextRarity, newPower);
			}
			
			CurrentRarity = finalRarity;
			_ui.SetRarityBaseZero(CurrentRarity);
		}

		public void Clear()
		{
			if (HeldUnit == null)
			{
				return;
			}

			HeldUnit = null;
			CurrentPower = 0;
			CurrentRarity = -1;

			UnitAnchor.DestroyAllChildGameObjects();
			UnitAnchor.localPosition = _unitAnchorOrigin;
			
			_ui.SetPower(CurrentPower);
			_ui.SetRarityBaseZero(CurrentRarity);
		}
	}
}