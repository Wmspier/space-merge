using System;
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
		[SerializeField] private VisualEffect _attackSoloEnemyEffect;
		[SerializeField] private VisualEffect _attackSoloPlayerEffect;
		[SerializeField] private VisualEffect _attackMissPlayerEffect;
		
		[SerializeField] private ParticleSystem _impactEnemyEffect;
		[SerializeField] private ParticleSystem _impactPlayerEffect;
		
		
		public async Task PlayBeamSequence(EnemyAttackInfo attackInfo, AttackResultType result, Action<EnemyAttackInfo> onImpact)
		{
			var effectPrefab = result switch
			{
				AttackResultType.ContestedEnemyWin => _attackContestedEnemyWinEffect,
				AttackResultType.ContestedPlayerWin => _attackContestedPlayerWinEffect,
				AttackResultType.SoloEnemy => _attackSoloEnemyEffect,
				AttackResultType.SoloPlayer => _attackSoloPlayerEffect,
				AttackResultType.MissPlayer => _attackMissPlayerEffect,
				_ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
			};
			
			var beamEffectInstance = Instantiate(effectPrefab, _vfxAnchor);
			beamEffectInstance.transform.position = attackInfo.OriginPosition;
			
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