using UnityEngine;

namespace Hex.Enemy
{
	public class EnemyAttackHandler : MonoBehaviour
	{
		[SerializeField] private EnemyAttackUI _ui;

		public bool IsAttackPhase => _ui.TurnsBeforeAttack == 0;
		
		public bool ElapseTurn() => _ui.ElapseTurn();
		public void ResetTurns() => _ui.ResetTurns();
	}
}