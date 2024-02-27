using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hex.Enemy;
using UnityEngine;
using UnityEngine.VFX;

namespace Hex.Sequencers
{
	public class AttackSequencer : MonoBehaviour
	{
		[SerializeField] private Transform _vfxAnchor;
		
		[SerializeField] private VisualEffect _attackContestedEnemyWinEffect;
		[SerializeField] private VisualEffect _attackContestedPlayerWinEffect;
		[SerializeField] private VisualEffect _attackContestedTie;
		[SerializeField] private VisualEffect _attackSoloEnemyEffect;
		[SerializeField] private VisualEffect _attackSoloPlayerEffect;
		[SerializeField] private VisualEffect _attackMissPlayerEffect;
		
		[SerializeField] private ParticleSystem _impactEnemyEffect;
		[SerializeField] private ParticleSystem _impactPlayerEffect;
		
		[SerializeField] private List<float> _beamScaleByCellRow;
		
		public async Task PlayBeamSequence(EnemyAttackInfo attackInfo, AttackResultType result, Action<EnemyAttackInfo> onImpact)
		{
			var effectPrefab = result switch
			{
				AttackResultType.ContestedEnemyWin => _attackContestedEnemyWinEffect,
				AttackResultType.ContestedPlayerWin => _attackContestedPlayerWinEffect,
				AttackResultType.ContestedTie => _attackContestedTie,
				AttackResultType.SoloEnemy => _attackSoloEnemyEffect,
				AttackResultType.SoloPlayer => _attackSoloPlayerEffect,
				AttackResultType.MissPlayer => _attackMissPlayerEffect,
				_ => null
			};

			if (effectPrefab == null)
			{
				Debug.Log($"Unsupported attack result: {result}");
				return;
			}
			
			var scale = _beamScaleByCellRow[Mathf.Min(_beamScaleByCellRow.Count - 1, attackInfo.TargetCell.Coordinates.x-1)];

			var beamEffectInstance = Instantiate(effectPrefab, _vfxAnchor);
			var beamTransform = beamEffectInstance.transform;
			beamTransform.position = attackInfo.OriginPosition;
			beamTransform.localScale = new Vector3(1, scale, 1);
			
			await Task.Delay((int)beamEffectInstance.GetFloat("Duration") * 1000);
			Destroy(beamEffectInstance.gameObject);

			ParticleSystem impactEffectInstance = null;
			if (result is AttackResultType.ContestedEnemyWin or AttackResultType.SoloEnemy)
			{
				impactEffectInstance = Instantiate(_impactPlayerEffect, transform);
				impactEffectInstance.transform.position = attackInfo.TargetCell.SurfaceAnchor.position;
			}
			else if (result is AttackResultType.ContestedPlayerWin or AttackResultType.SoloPlayer)
			{
				impactEffectInstance = Instantiate(_impactEnemyEffect, _vfxAnchor);
				impactEffectInstance.transform.position = attackInfo.OriginPosition;
			}
			
			onImpact?.Invoke(attackInfo);
			
			if (impactEffectInstance != null)
			{
				WaitThenDestroyImpact(impactEffectInstance, (int)(impactEffectInstance.main.duration * 1000));
			}
		}

		private static async void WaitThenDestroyImpact(ParticleSystem impactEffectInstance, int delayMS)
		{
			await Task.Delay((int)(impactEffectInstance.main.duration * 1000));
			Destroy(impactEffectInstance.gameObject);
		}
	}
}