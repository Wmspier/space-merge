using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.Enemy
{
	public class EnemyAttackUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _turnsBeforeAttackPhaseText;
		[SerializeField] private List<Transform> _turnTrackerDots;
		[SerializeField] private Transform _resolveAttackRoot;
		[SerializeField] private Button _resolveAttackButton;

		public Action ResolveAttackPressed;

		private int _elapsedTurns;
		public int TurnsBeforeAttack => _turnTrackerDots.Count - _elapsedTurns;

		private void Awake()
		{
			_resolveAttackButton.onClick.AddListener(() => ResolveAttackPressed?.Invoke());
		}

		public bool ElapseTurn()
		{
			return true;
			
			if (TurnsBeforeAttack == 0)
			{
				Debug.LogWarning("Elapsed turns already at max");
				return true;
			}

			_elapsedTurns++;

			// Fill dot
			var dotToFill = _turnTrackerDots[_elapsedTurns-1];
			DOTween.Sequence()
				.Append(dotToFill.DOScale(Vector3.one, .25f))
				.Append(dotToFill.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), .15f))
				.Play();
			
			// Update text
			_turnsBeforeAttackPhaseText.text = TurnsBeforeAttack == 0
				? "Enemy is Attacking!"
				: $"Enemy will Attack in {TurnsBeforeAttack} Turns";
			_turnsBeforeAttackPhaseText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), .25f);
				
			// If it's the attack phase, show icon
			if (TurnsBeforeAttack == 0)
			{
				DOTween.Sequence()
					.Append(_resolveAttackRoot.DOScale(Vector3.one, .25f))
					.Append(_resolveAttackRoot.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), .15f))
					.Play();
			}

			return TurnsBeforeAttack == 0;
		}

		public void ResetTurns()
		{
			foreach (var dot in _turnTrackerDots)
			{
				dot.localScale = Vector3.zero;
			}
			//_resolveAttackRoot.localScale = Vector3.zero;

			_elapsedTurns = 0;
			_turnsBeforeAttackPhaseText.text = $"Enemy Attack in {TurnsBeforeAttack} Turns";
		}
	}
}