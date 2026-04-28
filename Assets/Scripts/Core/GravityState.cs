using UnityEngine;

/// <summary>
/// Static gravity direction state. All player physics read from here
/// instead of assuming Y-down. GravitySwitch triggers change this.
/// </summary>
public enum GravityDir { Down, Up, Left, Right }

public static class GravityState
{
    public static GravityDir Current { get; private set; } = GravityDir.Down;

    /// <summary>The direction things fall (unit vector).</summary>
    public static Vector2 Down => Current switch
    {
        GravityDir.Down  => Vector2.down,
        GravityDir.Up    => Vector2.up,
        GravityDir.Left  => Vector2.left,
        GravityDir.Right => Vector2.right,
        _ => Vector2.down
    };

    /// <summary>Opposite of gravity — the direction jumps go.</summary>
    public static Vector2 Up => -Down;

    /// <summary>The axis the player walks along (perpendicular to gravity).</summary>
    public static Vector2 MoveAxis => Current switch
    {
        GravityDir.Down  => Vector2.right,
        GravityDir.Up    => Vector2.right,
        GravityDir.Left  => Vector2.up,
        GravityDir.Right => Vector2.up,
        _ => Vector2.right
    };

    /// <summary>Project input vector onto the walk axis. Returns signed scalar.</summary>
    public static float GetMoveInput(Vector2 input) => Vector2.Dot(input, MoveAxis);

    /// <summary>
    /// Get velocity component along the "up" axis (positive = rising, negative = falling).
    /// Same sign convention as the old rb.linearVelocity.y with normal gravity.
    /// </summary>
    public static float GetUpSpeed(Vector2 velocity) => Vector2.Dot(velocity, Up);

    /// <summary>Get velocity component along the walk axis.</summary>
    public static float GetMoveSpeed(Vector2 velocity) => Vector2.Dot(velocity, MoveAxis);

    /// <summary>Compose a velocity from move and up components.</summary>
    public static Vector2 ComposeVelocity(float moveSpeed, float upSpeed)
        => MoveAxis * moveSpeed + Up * upSpeed;

    /// <summary>
    /// Set gravity direction. Updates Physics2D.gravity to match.
    /// </summary>
    public static void Set(GravityDir dir)
    {
        Current = dir;
        Physics2D.gravity = Down * 20f;
    }

    /// <summary>Reset to default (down). Call on scene load or respawn if desired.</summary>
    public static void Reset()
    {
        Set(GravityDir.Down);
    }
}
