using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyShipInstance : MonoBehaviour
	{
		[field: SerializeField] public Transform HealthBarTarget { get; private set; }
		[field: SerializeField] public List<int3> OccupiedRelativeCoordinates { get; private set; }
	}
}