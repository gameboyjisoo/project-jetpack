using UnityEngine;

/// <summary>
/// All game event structs in one file for discoverability.
/// Add new events here as systems are built.
/// </summary>

// ── Player → World ──────────────────────────────────────

public struct PlayerLanded
{
    public Vector2 Position;
    public float FallSpeed;
}

public struct PlayerDied
{
    public Vector2 Position;
}

public struct PlayerJumped
{
    public Vector2 Position;
    public Vector2 Velocity;
}

public struct PlayerDashed
{
    public Vector2 Direction;
    public float Speed;
}

public struct PlayerJetpackActivated
{
    public Vector2 Direction;
    public int BoostMode; // 1=horizontal, 2=up, 3=down
}

public struct PlayerJetpackDeactivated
{
    public int BoostMode;
}

public struct PlayerFuelEmpty { }

public struct PlayerFuelRecharged { }

// ── Secondary Mode Events (mode-agnostic) ───────────────

public struct SecondaryUsed
{
    public Vector2 Direction;
    public string ModeName; // "dash", "gun", etc.
}

public struct SecondaryModeChanged
{
    public string OldMode;
    public string NewMode;
}

// ── World → Player ──────────────────────────────────────

public struct RoomEntered
{
    public Vector2 SpawnPoint;
    public int RoomId;
}

public struct RoomTransitionStarted
{
    public int FromRoomId;
    public int ToRoomId;
}

public struct RoomTransitionCompleted
{
    public int RoomId;
}

public struct GravityZoneEntered
{
    public float GravityMultiplier;
}

public struct GravityZoneExited { }

public struct FuelPickupCollected
{
    public float Amount;
}

public struct DashPickupCollected { }

// ── World → World ───────────────────────────────────────

public struct GimmickActivated
{
    public string GimmickId;
}

public struct GimmickDeactivated
{
    public string GimmickId;
}

public struct ChapterStarted
{
    public int ChapterIndex;
}

public struct CheckpointReached
{
    public Vector2 Position;
    public int RoomId;
}
