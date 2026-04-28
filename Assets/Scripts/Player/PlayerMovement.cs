using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float groundAcceleration = 120f;
    [SerializeField] private float groundDeceleration = 120f;
    [SerializeField] private float airMult = 0.65f;

    private Rigidbody2D rb;
    private bool facingRight = true;

    public bool FacingRight => facingRight;
    public float MoveSpeed => moveSpeed;

    public void Init(Rigidbody2D rigidbody) => rb = rigidbody;

    public void Tick(Vector2 moveInput, bool isGrounded, bool isJetpacking, bool isSecondaryBoosting, bool isWavedashing = false)
    {
        if (isJetpacking) return;
        if (isSecondaryBoosting) return;
        if (isWavedashing) return;

        float inputVal = GravityState.GetMoveInput(moveInput);
        float targetSpeed = inputVal * moveSpeed;
        float currentSpeed = GravityState.GetMoveSpeed(rb.linearVelocity);

        float mult = isGrounded ? 1f : airMult;
        float rate = Mathf.Abs(inputVal) > 0.01f ? groundAcceleration : groundDeceleration;
        rate *= mult;

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        float upSpeed = GravityState.GetUpSpeed(rb.linearVelocity);
        rb.linearVelocity = GravityState.ComposeVelocity(newSpeed, upSpeed);

        if (inputVal > 0.01f && !facingRight) Flip();
        else if (inputVal < -0.01f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
