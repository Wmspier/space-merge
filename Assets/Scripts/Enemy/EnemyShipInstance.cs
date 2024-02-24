using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyShipInstance : MonoBehaviour
	{
		[field: SerializeField] public Transform HealthBarTarget { get; private set; }
	}
}