using System.Collections.Generic;
using UnityEngine;
using VRunner.Core;
using VRunner.Data;
using System;
using Random = UnityEngine.Random;

namespace VRunner.Gameplay.Player
{
    /// <summary>
    /// Spawn orb VFX từ prefab, bay xoay quanh player khi buff active.
    /// </summary>
    public class PowerupOrbitVFX : MonoBehaviour
    {
        [Serializable]
        public class OrbitConfig
        {
            public PowerupType type;
            [Range(1, 8)] public int orbCount = 3;
            public float radius = 1.2f;
            public float heightOffset = 1f;
            public float angularSpeed = 180f;
            public float bobAmplitude = 0.2f;
            public float bobFrequency = 2.5f;

            [NonSerialized] public GameObject orbPrefab;
        }

        [SerializeField] private Transform followTarget;
        [SerializeField] private OrbitConfig[] configs;

        private readonly Dictionary<PowerupType, OrbitRing> activeRings = new Dictionary<PowerupType, OrbitRing>();
        private readonly Dictionary<PowerupType, OrbitConfig> configLookup = new Dictionary<PowerupType, OrbitConfig>();

        private class OrbitRing
        {
            public OrbitConfig config;
            public Transform root;
            public Transform[] orbs;
            public float angle;
        }

        private void Awake()
        {
            if (followTarget == null)
                followTarget = transform;

            configLookup.Clear();
            if (configs == null) return;

            foreach (var cfg in configs)
            {
                if (cfg == null) continue;
                string path = GetOrbResourcePath(cfg.type);
                if (path == null) continue;

                cfg.orbPrefab = AssetProvider.Load<GameObject>(path);
                if (cfg.orbPrefab == null) continue;
                if (!configLookup.ContainsKey(cfg.type))
                    configLookup[cfg.type] = cfg;
            }
        }

        private static string GetOrbResourcePath(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Magnet: return "VFX/VFX_OrbitOrb_Magnet";
                case PowerupType.Shield: return "VFX/VFX_OrbitOrb_Shield";
                case PowerupType.SpeedBoost: return "VFX/VFX_OrbitOrb_Speed";
                default: return null;
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnPowerupActivated -= HandleActivated;
            EventManager.Instance.OnPowerupExpired -= HandleExpired;
            EventManager.Instance.OnPowerupActivated += HandleActivated;
            EventManager.Instance.OnPowerupExpired += HandleExpired;
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnPowerupActivated -= HandleActivated;
                EventManager.Instance.OnPowerupExpired -= HandleExpired;
            }

            ClearAllRings();
        }

        private void LateUpdate()
        {
            if (activeRings.Count == 0 || followTarget == null) return;

            Vector3 center = followTarget.position;
            foreach (var pair in activeRings)
            {
                OrbitRing ring = pair.Value;
                OrbitConfig cfg = ring.config;
                ring.angle += cfg.angularSpeed * Time.deltaTime;
                ring.root.position = center + Vector3.up * cfg.heightOffset;

                int count = ring.orbs.Length;
                for (int i = 0; i < count; i++)
                {
                    float a = (ring.angle + i * (360f / count)) * Mathf.Deg2Rad;
                    float bob = Mathf.Sin(Time.time * cfg.bobFrequency + i) * cfg.bobAmplitude;
                    ring.orbs[i].localPosition = new Vector3(
                        Mathf.Cos(a) * cfg.radius,
                        bob,
                        Mathf.Sin(a) * cfg.radius
                    );
                }
            }
        }

        private void HandleActivated(PowerupType type, float duration)
        {
            if (!configLookup.TryGetValue(type, out OrbitConfig cfg))
                return;

            if (activeRings.ContainsKey(type))
                DestroyRing(type);

            activeRings[type] = CreateRing(cfg);
        }

        private void HandleExpired(PowerupType type)
        {
            DestroyRing(type);
        }

        private OrbitRing CreateRing(OrbitConfig cfg)
        {
            var rootGo = new GameObject($"Orbit_{cfg.type}");
            rootGo.transform.SetParent(null, true);
            rootGo.transform.position = followTarget.position + Vector3.up * cfg.heightOffset;

            var orbs = new Transform[cfg.orbCount];
            for (int i = 0; i < cfg.orbCount; i++)
            {
                GameObject instance = Instantiate(cfg.orbPrefab, rootGo.transform);
                instance.name = $"{cfg.orbPrefab.name}_{i}";
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = cfg.orbPrefab.transform.localScale;
                orbs[i] = instance.transform;
            }

            return new OrbitRing
            {
                config = cfg,
                root = rootGo.transform,
                orbs = orbs,
                angle = Random.Range(0f, 360f)
            };
        }

        private void DestroyRing(PowerupType type)
        {
            if (!activeRings.TryGetValue(type, out OrbitRing ring))
                return;

            if (ring.root != null)
                Destroy(ring.root.gameObject);

            activeRings.Remove(type);
        }

        private void ClearAllRings()
        {
            var types = new List<PowerupType>(activeRings.Keys);
            foreach (var type in types)
                DestroyRing(type);
        }
    }
}
