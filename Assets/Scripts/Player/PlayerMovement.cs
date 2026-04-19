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

    public void Tick(Vector2 moveInput, bool isGrounded, bool isJetpacking, bool isSecondaryBoosting)
    {
        if (isJetpacking) return;
        if (isSecondaryBoosting) return;

        float targetSpeed = moveInput.x * moveSpeed;
        float currentSpeed = rb.linearVelocity.x;

        float mult = isGrounded ? 1f : airMult;
        float rate = Mathf.Abs(moveInput.x) > 0.01f ? groundAcceleration : groundDeceleration;
        rate *= mult;

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0.01f && !facingRight) Flip();
        else if (moveInput.x < -0.01f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
