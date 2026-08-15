using UnityEngine;

namespace VRunner.Gameplay.Player
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController playerController;

        // Animation parameters
        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int Death = Animator.StringToHash("Death");

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // animator.SetBool(IsRunning, true);
        }

        public void PlayRunning()
        {
            animator.SetBool(IsRunning, true);
        }

        public void PlayJump()
        {
            animator.SetTrigger(Jump);
            animator.SetBool(IsGrounded, false);
        }

        public void PlayLand()
        {
            animator.SetBool(IsGrounded, true);
        }

        public void PlayDeath()
        {
            animator.SetTrigger(Death);
            animator.SetBool(IsRunning, false);
        }
    }
}
