using UnityEngine;

namespace Hex.Data
{
	public abstract class CardData : ScriptableObject
	{
		[field: SerializeField] public string UniqueId { get; private set; }
	}
}