using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.Enemy
{
	public class EnemyAttackUI : MonoBehaviour
	{
		[SerializeField] private Button _resolveAttackButton;

		public Action ResolveAttackPressed;

		private int _elapsedTurns;

		private void Awake()
		{
			_resolveAttackButton.onClick.AddListener(() => ResolveAttackPressed?.Invoke());
		}
	}
}