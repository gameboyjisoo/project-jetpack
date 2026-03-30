using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerController player;

    private static readonly int AnimIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimIsJetpacking = Animator.StringToHash("IsJetpacking");
    private static readonly int AnimVelocityY = Animator.StringToHash("VelocityY");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (animator.runtimeAnimatorController == null) return;

        animator.SetBool(AnimIsRunning, Mathf.Abs(player.Velocity.x) > 0.1f);
        animator.SetBool(AnimIsGrounded, player.IsGrounded);
        animator.SetBool(AnimIsJetpacking, player.IsJetpacking);
        animator.SetFloat(AnimVelocityY, player.Velocity.y);
    }
}
