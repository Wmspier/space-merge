using UnityEngine;

namespace Hex.Data
{
	[CreateAssetMenu(menuName = "Hex/Unit", fileName = "New_Unit")]
	public class UnitData : CardData
	{
		[field: SerializeField] public int BasePower { get; private set; }
		[field: SerializeField] public int BaseShield { get; private set; }
		[field: SerializeField] public int BaseRarity { get; private set; }
		[field: SerializeField] public GameObject Prefab { get; private set; }
		[field: SerializeField] public UnitData NextRarity { get; private set; }
	}
}