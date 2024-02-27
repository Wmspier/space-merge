using UnityEngine;

namespace Hex.Data
{
	[CreateAssetMenu(menuName = "Hex/Config/Battle", fileName = "New_Config_Battle")]
	public class BattleConfig : ScriptableObject
	{
		[field: SerializeField] public int UnitMovesPerBattle { get; private set; }
	}
}