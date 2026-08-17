using UnityEngine;

namespace VRunner.Gameplay.Player
{
    public class SpeedLinesVFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem speedLines;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float maxEmissionRate = 50f;
        [SerializeField] private float baseSpeed = 10f;

        private ParticleSystem.EmissionModule emission;

        private void Start()
        {
            emission = speedLines.emission;
        }

        private void Update()
        {
            // if (playerController != null)
            // {
            //     float speedRatio = playerController.CurrentSpeed / baseSpeed;
            //     emission.rateOverTime = maxEmissionRate * speedRatio;
            // }
        }

    }
}
