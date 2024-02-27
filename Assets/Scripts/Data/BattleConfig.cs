using UnityEngine;

namespace Hex.Data
{
	[CreateAssetMenu(menuName = "Hex/Config/Battle", fileName = "New_Config_Battle")]
	public class BattleConfig : ScriptableObject
	{
		[field: SerializeField] public int UnitMovesPerBattle { get; private set; }
		[field: SerializeField] public int MaxMergeCount { get; private set; }
		[field: SerializeField] public int PlayerStartingHealth { get; private set; }
	}
}