using Hex.Data;
using TMPro;
using UnityEngine;

namespace Hex.Grid.Cell
{
	public class HexCellInfoHolder : MonoBehaviour
	{
		[SerializeField] private HexCellUI _ui;
		
		[field: SerializeField] public Transform UnitAnchor { get; private set; }
		public UnitData HeldUnit { get; private set; }
		public int CurrentPower { get; private set; }

		private void Awake()
		{
			_ui.SetPower(0);
		}

		public void SpawnUnit(UnitData unitData)
		{
			if (HeldUnit != null)
			{
				Debug.LogWarning("Trying to spawn unit on occupied cell");
				return;
			}

			HeldUnit = unitData;
			Instantiate(unitData.Prefab, UnitAnchor);
			CurrentPower = HeldUnit.BasePower;
			_ui.SetPower(CurrentPower);
		}

		public void Clear()
		{
			if (HeldUnit == null) return;

			HeldUnit = null;
			CurrentPower = 0;
			
			Destroy(UnitAnchor.GetChild(0));
			_ui.SetPower(0);
		}
	}
}