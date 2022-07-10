using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Visuals.Environment
{
	public class LightningEffectsController : MonoBehaviour
	{
		public Color FogColorEffect = Color.white;
		[Tooltip("Adds FogDensity curve value multiplied by this to the skybox exposure.")]
		public float SkyboxExposureMultiplier = 50f;
		[Tooltip("Adds FogDensity curve value multiplied by this and fog color to the blocks base color.")]
		public float BlocksBrightUpMultiplier = 20f;
		public AnimationCurve[] FogDensityCurves;

		public float EffectsDuration = 1.5f;

		public GameObject[] LightningStrikeSpawners;

		private class LightningEffects
		{
			public float StartTime;
			public GameObject Spawner;
			public AnimationCurve FogDensityCurve;
		}

		private float m_InitialFogDensity;
		private Color m_InitialFogColor;
		private Material m_InitialSkybox;
		private Material m_EffectsSkybox;

		private List<LightningEffects> m_Effects = new List<LightningEffects>();

		void Awake()
		{
			if (LightningStrikeSpawners.Length == 0) {
				LightningStrikeSpawners = new GameObject[transform.childCount];

				for (int i = 0; i < transform.childCount; ++i) {
					LightningStrikeSpawners[i] = transform.GetChild(i).gameObject;
				}
			}


			foreach(var spawner in LightningStrikeSpawners) {
				spawner.SetActive(false);
			}

		}

		void Start()
		{
			m_InitialFogDensity = RenderSettings.fogDensity;
			m_InitialFogColor = RenderSettings.fogColor;
			m_InitialSkybox = RenderSettings.skybox;
			if (RenderSettings.skybox) {
				m_EffectsSkybox = Instantiate(m_InitialSkybox);
			}
		}

		void OnDestroy()
		{
			RestoreInitialEnvironment();
			if (m_EffectsSkybox) {
				Destroy(m_EffectsSkybox);
			}
		}

		public void SpawnLightningStrike()
		{
			List<GameObject> inactiveSpawners = LightningStrikeSpawners.Where(s => m_Effects.All(e => s != e.Spawner)).ToList();
			if (inactiveSpawners.Count == 0) {
				Debug.LogWarning("Trying to spawn lightning strike, but no available spawners are left.", this);
				return;
			}

			GameObject selectedSpawner = inactiveSpawners[Random.Range(0, inactiveSpawners.Count)];
			selectedSpawner.SetActive(true);

			m_Effects.Add(new LightningEffects() {
				StartTime = Time.time,
				Spawner = selectedSpawner,
				FogDensityCurve = FogDensityCurves[Random.Range(0, FogDensityCurves.Length)]
			});
		}

		void UpdateEffects()
		{
			if (m_Effects.Count == 0)
				return;

			float highestDensity = 0;

			for(int i = 0; i < m_Effects.Count; ++i) {
				LightningEffects effect = m_Effects[i];
				float progress = (Time.time - effect.StartTime) / EffectsDuration;

				if (progress > 1f) {
					effect.Spawner.SetActive(false);

					m_Effects.RemoveAt(i);
					i--;

					continue;
				}

				float density = effect.FogDensityCurve.Evaluate(progress);
				if (highestDensity < density) {
					highestDensity = density;
				}
			}

			if (m_Effects.Count != 0) {
				RenderSettings.fogColor = FogColorEffect;
				RenderSettings.fogDensity = m_InitialFogDensity + highestDensity;
				Shader.SetGlobalColor("_Lightning_Color", FogColorEffect * highestDensity * BlocksBrightUpMultiplier);

				if (m_EffectsSkybox) {
					RenderSettings.skybox = m_EffectsSkybox;
					m_EffectsSkybox.SetFloat("_Exposure", highestDensity * SkyboxExposureMultiplier + m_InitialSkybox.GetFloat("_Exposure"));
				}
			} else {
				// All effects just stopped, back to normal.
				RestoreInitialEnvironment();
			}
		}

		private void RestoreInitialEnvironment()
		{
			RenderSettings.fogColor = m_InitialFogColor;
			RenderSettings.fogDensity = m_InitialFogDensity;
			RenderSettings.skybox = m_InitialSkybox;
			Shader.SetGlobalColor("_Lightning_Color", Color.clear);
		}

		void Update()
		{
			UpdateEffects();

#if UNITY_EDITOR
			if (Keyboard.current == null || Keyboard.current.altKey.isPressed || Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed)
				return;

			if (Keyboard.current.lKey.wasPressedThisFrame) {
				SpawnLightningStrike();
			}
#endif
		}
	}

}