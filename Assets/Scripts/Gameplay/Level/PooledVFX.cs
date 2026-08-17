using UnityEngine;
using VRunner.Core;

namespace VRunner.Gameplay.Level
{
    public class PooledVFX : MonoBehaviour
    {
        private ParticleSystem ps;
        public int PoolIndex { get; set; } = -1;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Callback;
            main.loop = false; // VFX one-shot phải tắt loop
        }

        private void OnEnable()
        {
            ps.Clear();
            ps.Play();
        }

        // Unity gọi khi particle dừng (Stop Action = Callback)
        private void OnParticleSystemStopped()
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnVFX(this);
            else
                gameObject.SetActive(false);
        }
    }
}
