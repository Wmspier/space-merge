using UnityEngine;

namespace Hex.Data
{
	[CreateAssetMenu(menuName = "Hex/Unit", fileName = "New_Unit")]
	public class UnitData : CardData
	{
		[field: SerializeField] public int BasePower { get; private set; }
		[field: SerializeField] public GameObject Prefab { get; private set; }
	}
}