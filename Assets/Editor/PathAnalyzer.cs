// PathAnalyzer.cs
// Developer utility: record a player path through the scene, then analyze it for
// jump gaps, jetpack gaps, and unreachable segments relative to tuned physics values.
//
// Implements: developer workflow tooling (no GDD requirement — utility only).
// Not a gameplay system. No ADR required.
//
// Usage:
//   1. Open via  Project Jetpack > Path Analyzer  (or Ctrl+Shift+P).
//   2. Enter Play Mode — the window starts recording the player's position every frame.
//   3. Exit Play Mode — recording stops automatically.
//   4. Click "Analyze" to classify each segment and highlight potential issues.
//   5. Waypoints are displayed as Scene View gizmos (green = ok, yellow = jump gap,
//      cyan = jetpack gap, red = unreachable).
//   6. Click "Clear" to wipe the recorded path. Data persists across Unity restarts
//      via EditorPrefs (JSON, no asset files created).
//
// Coordinate conventions (match project):
//   1 tile = 1 Unity unit, 16 PPU.
//   Waypoints snap to integer tile positions.
//   Room name auto-detected from nearest Room component in the scene.
//
// Jump thresholds (derived from current tuning — see CLAUDE.md):
//   Vertical:   jumpForce=8, gravity=-20. Max height ≈ v²/2g = 64/40 ≈ 1.6u (short tap)
//               Full hold (varJumpTime=0.2s) raises the ceiling; ~2.5u is the practical
//               "definitely needs a jump" threshold below which walk-up ramps suffice.
//   Horizontal: boostSpeed(dash)=32, boostDuration=0.15s → ~4.8u per dash.
//               Used as the "definitely needs a dash or jetpack" threshold.
//
// Physics reference values kept as named constants so they stay in sync with tuning.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that records the player path during Play Mode and classifies
/// each segment as walkable, jump-required, jetpack-required, or unreachable.
/// Data persists via EditorPrefs as JSON across Unity sessions.
/// </summary>
public class PathAnalyzer : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────────────
    // Segment classification
    // ─────────────────────────────────────────────────────────────────────────

    public enum SegmentKind
    {
        Walk,       // flat or gentle slope — no special move needed
        Jump,       // vertical gap within jump range (~2.5u)
        Jetpack,    // gap requiring jetpack (vertical OR combined with horizontal)
        Dash,       // horizontal gap within dash range (~4.8u) but not needing jetpack
        Unreachable // exceeds all known movement capabilities
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tuning constants (match CLAUDE.md — update here if values change)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Vertical height below which a step can be climbed by jumping.</summary>
    private const float JumpVerticalThreshold = 2.5f;

    /// <summary>Horizontal distance a single dash covers (dash speed × dash duration).</summary>
    private const float DashHorizontalThreshold = 4.8f;

    /// <summary>
    /// Approximate max horizontal reach per jetpack burst (boostSpeed=11 × ~0.9s effective,
    /// capped by the 1-second fuel budget minus activation overhead).
    /// </summary>
    private const float JetpackHorizontalThreshold = 9.5f;

    /// <summary>Max upward height jetpack can reach (boostSpeed=11 × ~0.9s).</summary>
    private const float JetpackVerticalThreshold = 9.0f;

    /// <summary>Distance (tiles) within which a hazard is considered "nearby" a waypoint.</summary>
    private const float HazardProximityRadius = 3.0f;

    // ─────────────────────────────────────────────────────────────────────────
    // Persistence keys
    // ─────────────────────────────────────────────────────────────────────────

    private const string PrefsKey_Waypoints  = "PathAnalyzer_Waypoints_v2";
    private const string PrefsKey_ScrollPos  = "PathAnalyzer_ScrollPos";
    private const string PrefsKey_RecordRate = "PathAnalyzer_RecordRate";
    private const string PrefsKey_MinDist    = "PathAnalyzer_MinDist";

    // ─────────────────────────────────────────────────────────────────────────
    // Serializable data (stored via EditorPrefs as JSON)
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    private struct WaypointData
    {
        public float x;
        public float y;
        public string roomName;
        public float timeStamp;
    }

    [Serializable]
    private struct WaypointList
    {
        public WaypointData[] items;
    }

    [Serializable]
    private struct Segment
    {
        public int fromIndex;
        public int toIndex;
        public SegmentKind kind;
        public float deltaX;
        public float deltaY;
        public bool nearHazard;       // hazard detected within proximity of this segment
        public float hazardDistance;   // distance to nearest hazard (tiles)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Runtime state
    // ─────────────────────────────────────────────────────────────────────────

    private List<WaypointData> _waypoints = new List<WaypointData>();
    private List<Segment>      _segments  = new List<Segment>();
    private Vector2            _scrollPos;

    // Recording settings
    private float _recordRateHz   = 10f;   // samples per second
    private float _minDistUnits   = 0.5f;  // min movement before a new waypoint is added

    private float _nextSampleTime = 0f;
    private bool  _isRecording    = false;

    // Scene gizmo colours
    private static readonly Color ColorWalk       = new Color(0.2f, 1.0f, 0.2f, 0.9f);
    private static readonly Color ColorJump       = new Color(1.0f, 1.0f, 0.2f, 0.9f);
    private static readonly Color ColorJetpack    = new Color(0.0f, 0.9f, 1.0f, 0.9f);
    private static readonly Color ColorDash       = new Color(1.0f, 0.5f, 0.0f, 0.9f);
    private static readonly Color ColorUnreachable= new Color(1.0f, 0.15f, 0.15f, 0.9f);
    private static readonly Color ColorWaypoint   = new Color(1.0f, 1.0f, 1.0f, 0.5f);

    // Fold-out state
    private bool _showSettings   = false;
    private bool _showWaypointList = false;

    // ─────────────────────────────────────────────────────────────────────────
    // Window lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Project Jetpack/Path Analyzer %#p")]  // Ctrl+Shift+P
    public static void ShowWindow()
    {
        var win = GetWindow<PathAnalyzer>("Path Analyzer");
        win.minSize = new Vector2(300f, 420f);
    }

    private void OnEnable()
    {
        LoadFromPrefs();
        SceneView.duringSceneGui  += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update  += OnEditorUpdate;
        _scrollPos = new Vector2(
            EditorPrefs.GetFloat(PrefsKey_ScrollPos + "_x", 0f),
            EditorPrefs.GetFloat(PrefsKey_ScrollPos + "_y", 0f));
    }

    private void OnDisable()
    {
        SaveToPrefs();
        SceneView.duringSceneGui  -= OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update  -= OnEditorUpdate;
        EditorPrefs.SetFloat(PrefsKey_ScrollPos + "_x", _scrollPos.x);
        EditorPrefs.SetFloat(PrefsKey_ScrollPos + "_y", _scrollPos.y);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Play mode hooks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                _isRecording   = true;
                _nextSampleTime = 0f;
                Debug.Log("[PathAnalyzer] Recording started.");
                break;

            case PlayModeStateChange.ExitingPlayMode:
                _isRecording = false;
                SaveToPrefs();
                Debug.Log($"[PathAnalyzer] Recording stopped. {_waypoints.Count} waypoints captured.");
                Repaint();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-frame recording (runs in editor update loop while in Play Mode)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEditorUpdate()
    {
        if (!_isRecording || !EditorApplication.isPlaying) return;

        float now = (float)EditorApplication.timeSinceStartup;
        if (now < _nextSampleTime) return;
        _nextSampleTime = now + (1f / Mathf.Max(_recordRateHz, 1f));

        // Find the player by tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // Snap position to tile grid (integer units)
        Vector2 rawPos  = player.transform.position;
        Vector2 snapped = new Vector2(Mathf.RoundToInt(rawPos.x), Mathf.RoundToInt(rawPos.y));

        // Skip if the player hasn't moved enough
        if (_waypoints.Count > 0)
        {
            var last = _waypoints[_waypoints.Count - 1];
            float dist = Vector2.Distance(new Vector2(last.x, last.y), snapped);
            if (dist < _minDistUnits) return;
        }

        string roomName = DetectRoomName(snapped);

        _waypoints.Add(new WaypointData
        {
            x          = snapped.x,
            y          = snapped.y,
            roomName   = roomName,
            timeStamp  = now
        });

        // Invalidate segment cache whenever a new waypoint arrives
        _segments.Clear();

        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Room detection
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the Room component whose bounds contain the given position.
    /// Falls back to the nearest room by center distance if none contains the point.
    /// Returns an empty string if no rooms exist in the scene.
    /// </summary>
    private static string DetectRoomName(Vector2 position)
    {
        // FindObjectsByType is the Unity 6 non-deprecated equivalent of FindObjectsOfType.
        Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        if (rooms == null || rooms.Length == 0) return string.Empty;

        // Prefer a room whose bounds contain the position
        foreach (var room in rooms)
        {
            if (room.GetBounds().Contains(new Vector3(position.x, position.y, 0f)))
                return room.RoomId;
        }

        // Fallback: nearest room by center distance
        Room nearest = null;
        float bestDist = float.MaxValue;
        foreach (var room in rooms)
        {
            float d = Vector2.Distance(room.RoomCenter, position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest  = room;
            }
        }
        return nearest != null ? nearest.RoomId : string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Segment analysis
    // ─────────────────────────────────────────────────────────────────────────

    private void Analyze()
    {
        _segments.Clear();
        if (_waypoints.Count < 2) return;

        // Collect all hazard positions in the scene (Hazard components + SpawnTiles that spawn hazards)
        var hazardPositions = CollectHazardPositions();

        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            var a = _waypoints[i];
            var b = _waypoints[i + 1];

            float dx = b.x - a.x;
            float dy = b.y - a.y;

            // Check hazard proximity along this segment (check both endpoints and midpoint)
            Vector2 posA = new Vector2(a.x, a.y);
            Vector2 posB = new Vector2(b.x, b.y);
            Vector2 mid  = (posA + posB) * 0.5f;

            float nearestHazardDist = float.MaxValue;
            foreach (var hp in hazardPositions)
            {
                float dA = Vector2.Distance(posA, hp);
                float dB = Vector2.Distance(posB, hp);
                float dM = Vector2.Distance(mid, hp);
                float closest = Mathf.Min(dA, Mathf.Min(dB, dM));
                if (closest < nearestHazardDist)
                    nearestHazardDist = closest;
            }

            _segments.Add(new Segment
            {
                fromIndex      = i,
                toIndex        = i + 1,
                kind           = ClassifySegment(dx, dy),
                deltaX         = dx,
                deltaY         = dy,
                nearHazard     = nearestHazardDist <= HazardProximityRadius,
                hazardDistance  = nearestHazardDist
            });
        }

        int hazardSegments = 0;
        foreach (var seg in _segments)
            if (seg.nearHazard) hazardSegments++;

        Debug.Log($"[PathAnalyzer] Analysis complete: {_segments.Count} segments classified, {hazardSegments} near hazards.");
    }

    /// <summary>
    /// Collects world positions of all hazards in the scene:
    /// - Hazard components (runtime objects)
    /// - SpawnTile tiles on Interactables tilemaps that reference the Hazard prefab
    /// </summary>
    private static List<Vector2> CollectHazardPositions()
    {
        var positions = new List<Vector2>();

        // 1. Find Hazard GameObjects already in the scene
        var hazards = FindObjectsByType<Hazard>(FindObjectsSortMode.None);
        foreach (var h in hazards)
            positions.Add(h.transform.position);

        // 2. Find SpawnTiles on Interactables tilemaps that reference the Hazard prefab
        var tilemaps = FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
        foreach (var tilemap in tilemaps)
        {
            // Only check tilemaps that have SpawnTileManager (Interactables layer)
            if (tilemap.GetComponent<SpawnTileManager>() == null) continue;

            var bounds = tilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(pos) as SpawnTile;
                if (tile == null) continue;

                // Check if this SpawnTile's prefab has a Hazard component
                if (tile.prefab != null && tile.prefab.GetComponent<Hazard>() != null)
                {
                    Vector3 worldPos = tilemap.CellToWorld(pos) + tilemap.cellSize * 0.5f;
                    positions.Add(worldPos);
                }
            }
        }

        // 3. Also check for objects on Layer 10 (Hazard layer) that might not have the Hazard component
        var allObjects = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var col in allObjects)
        {
            if (col.gameObject.layer == 10 && !positions.Contains((Vector2)col.transform.position))
                positions.Add(col.transform.position);
        }

        return positions;
    }

    /// <summary>
    /// Classifies a single (dx, dy) movement vector into a SegmentKind.
    ///
    /// Logic follows the design axiom that:
    ///   - Walk covers flat movement or very small steps.
    ///   - Jump covers upward steps within the jump arc's reach.
    ///   - Dash covers horizontal gaps within dash range (no significant upward component).
    ///   - Jetpack covers large gaps or combined vertical+horizontal distances.
    ///   - Unreachable is anything that exceeds all of the above.
    ///
    /// Downward movement (falling) is always Walk — gravity handles it.
    /// </summary>
    private static SegmentKind ClassifySegment(float dx, float dy)
    {
        float absDx = Mathf.Abs(dx);
        float absDy = Mathf.Abs(dy);
        bool  isFalling = dy < -0.5f;  // descending more than half a tile

        // Falling or flat: always walkable (gravity + normal movement)
        if (isFalling || absDy <= 0.5f)
        {
            // Large horizontal gap while falling/flat → needs dash
            if (absDx > DashHorizontalThreshold && absDx <= JetpackHorizontalThreshold)
                return SegmentKind.Dash;
            if (absDx > JetpackHorizontalThreshold)
                return SegmentKind.Unreachable;
            return SegmentKind.Walk;
        }

        // Rising: check vertical component first
        if (absDy <= JumpVerticalThreshold)
        {
            // Moderate horizontal reach while jumping → dash might help, but jump covers it
            if (absDx <= DashHorizontalThreshold)
                return SegmentKind.Jump;
            if (absDx <= JetpackHorizontalThreshold)
                return SegmentKind.Jetpack;
            return SegmentKind.Unreachable;
        }

        // Significant upward component beyond jump range → needs jetpack
        if (absDy <= JetpackVerticalThreshold && absDx <= JetpackHorizontalThreshold)
            return SegmentKind.Jetpack;

        // Exceeds jetpack capability
        return SegmentKind.Unreachable;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persistence (EditorPrefs + JSON)
    // ─────────────────────────────────────────────────────────────────────────

    private void SaveToPrefs()
    {
        var list = new WaypointList { items = _waypoints.ToArray() };
        string json = JsonUtility.ToJson(list);
        EditorPrefs.SetString(PrefsKey_Waypoints,  json);
        EditorPrefs.SetFloat(PrefsKey_RecordRate, _recordRateHz);
        EditorPrefs.SetFloat(PrefsKey_MinDist,    _minDistUnits);
    }

    private void LoadFromPrefs()
    {
        _recordRateHz = EditorPrefs.GetFloat(PrefsKey_RecordRate, 10f);
        _minDistUnits = EditorPrefs.GetFloat(PrefsKey_MinDist,    0.5f);

        string json = EditorPrefs.GetString(PrefsKey_Waypoints, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            _waypoints = new List<WaypointData>();
            return;
        }

        try
        {
            var list = JsonUtility.FromJson<WaypointList>(json);
            _waypoints = list.items != null
                ? new List<WaypointData>(list.items)
                : new List<WaypointData>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PathAnalyzer] Failed to deserialize saved waypoints: {e.Message}");
            _waypoints = new List<WaypointData>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Editor GUI
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        DrawStatusBar();
        EditorGUILayout.Space(4f);
        DrawSettings();
        EditorGUILayout.Space(4f);
        DrawActions();
        EditorGUILayout.Space(4f);
        DrawAnalysisSummary();
        EditorGUILayout.Space(4f);
        DrawWaypointList();
    }

    private void DrawHeader()
    {
        GUILayout.Label("Path Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Record the player's path during Play Mode, then classify each segment.",
            EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.Space(4f);

        Color prevBg = GUI.backgroundColor;
        if (_isRecording)
            GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
        else if (EditorApplication.isPlaying)
            GUI.backgroundColor = new Color(1f, 1f, 0.3f);

        string statusText = _isRecording
            ? $"RECORDING — {_waypoints.Count} waypoints"
            : EditorApplication.isPlaying
                ? "In Play Mode (not recording)"
                : _waypoints.Count > 0
                    ? $"Ready — {_waypoints.Count} waypoints, {_segments.Count} segments analyzed"
                    : "No data. Enter Play Mode to record.";

        EditorGUILayout.HelpBox(statusText,
            _isRecording ? MessageType.Info :
            EditorApplication.isPlaying ? MessageType.Warning :
            _waypoints.Count > 0 ? MessageType.None : MessageType.Info);

        GUI.backgroundColor = prevBg;
    }

    private void DrawSettings()
    {
        _showSettings = EditorGUILayout.Foldout(_showSettings, "Recording Settings", true);
        if (!_showSettings) return;

        EditorGUI.indentLevel++;

        float newRate = EditorGUILayout.FloatField(
            new GUIContent("Sample Rate (Hz)", "Waypoints captured per second during Play Mode."),
            _recordRateHz);
        _recordRateHz = Mathf.Clamp(newRate, 1f, 60f);

        float newDist = EditorGUILayout.FloatField(
            new GUIContent("Min Move Distance (u)", "Minimum tiles moved before a new waypoint is recorded. Prevents duplicate snapped positions."),
            _minDistUnits);
        _minDistUnits = Mathf.Clamp(newDist, 0.1f, 5f);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Jump thresholds (read-only):", EditorStyles.miniLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField(
            new GUIContent("  Jump vertical (u)", "Segments with dy in this range are classified as Jump."),
            JumpVerticalThreshold);
        EditorGUILayout.FloatField(
            new GUIContent("  Dash horizontal (u)", "Segments with dx in this range are classified as Dash."),
            DashHorizontalThreshold);
        EditorGUILayout.FloatField(
            new GUIContent("  Jetpack horizontal (u)", "Segments exceeding this are Unreachable."),
            JetpackHorizontalThreshold);
        EditorGUILayout.FloatField(
            new GUIContent("  Jetpack vertical (u)", "Segments with dy exceeding this are Unreachable."),
            JetpackVerticalThreshold);
        EditorGUI.EndDisabledGroup();

        EditorGUI.indentLevel--;

        if (GUI.changed) SaveToPrefs();
    }

    private void DrawActions()
    {
        GUILayout.Label("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        bool canAnalyze = _waypoints.Count >= 2 && !_isRecording;
        EditorGUI.BeginDisabledGroup(!canAnalyze);
        if (GUILayout.Button("Analyze", GUILayout.Height(28f)))
        {
            Analyze();
            SceneView.RepaintAll();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_waypoints.Count == 0 || _isRecording);
        if (GUILayout.Button("Clear", GUILayout.Height(28f)))
        {
            if (EditorUtility.DisplayDialog("Clear Path",
                $"Delete all {_waypoints.Count} recorded waypoints?", "Clear", "Cancel"))
            {
                _waypoints.Clear();
                _segments.Clear();
                SaveToPrefs();
                SceneView.RepaintAll();
                Repaint();
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(_segments.Count == 0);
        if (GUILayout.Button("Export Summary to Clipboard", GUILayout.Height(28f)))
            ExportSummary();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(2f);

        if (GUILayout.Button("Focus Scene View on Path"))
            FocusSceneOnPath();
    }

    private void ExportSummary()
    {
        if (_segments.Count == 0 || _waypoints.Count < 2) return;

        string roomName = _waypoints.Count > 0 ? _waypoints[0].roomName : "Unknown";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Path Analysis: {roomName} ===");
        sb.AppendLine($"Waypoints: {_waypoints.Count} | Segments: {_segments.Count}");
        sb.AppendLine();

        // Gate detection
        FuelGate[] gates = FindObjectsByType<FuelGate>(FindObjectsSortMode.None);

        for (int i = 0; i < _waypoints.Count; i++)
        {
            var wp = _waypoints[i];
            var pos = new Vector2(wp.x, wp.y);

            // Check for nearby gate
            string gateNote = "";
            foreach (var gate in gates)
            {
                if (Vector2.Distance(pos, (Vector2)gate.transform.position) < 2f)
                {
                    var so = new SerializedObject(gate);
                    var prop = so.FindProperty("requiredTier");
                    if (prop != null)
                    {
                        string tierName = ((FuelTier)prop.enumValueIndex).ToString();
                        gateNote = $" [GATE: {tierName} tier required]";
                    }
                    break;
                }
            }

            // Check for ground
            bool onGround = Physics2D.Raycast(pos, Vector2.down, 1.5f, 1 << 8).collider != null;
            string groundNote = onGround ? " [GROUND - fuel recharges]" : "";

            sb.AppendLine($"Waypoint {i} ({wp.x:F0}, {wp.y:F0}){groundNote}{gateNote}");

            // Segment after this waypoint
            if (i < _segments.Count)
            {
                var seg = _segments[i];
                float dist = Mathf.Sqrt(seg.deltaX * seg.deltaX + seg.deltaY * seg.deltaY);
                string hazardNote = seg.nearHazard ? $" ⚠ HAZARD {seg.hazardDistance:F1}u away" : "";
                sb.AppendLine($"  → {seg.kind} | {dist:F1} tiles (dx={seg.deltaX:+0.#;-0.#;0}, dy={seg.deltaY:+0.#;-0.#;0}){hazardNote}");
            }
        }

        // Totals
        int walk = 0, jump = 0, dash = 0, jetpack = 0, unreachable = 0;
        foreach (var seg in _segments)
        {
            switch (seg.kind)
            {
                case SegmentKind.Walk:        walk++;        break;
                case SegmentKind.Jump:        jump++;        break;
                case SegmentKind.Dash:        dash++;        break;
                case SegmentKind.Jetpack:     jetpack++;     break;
                case SegmentKind.Unreachable: unreachable++; break;
            }
        }
        int nearHazardCount = 0;
        foreach (var seg in _segments)
            if (seg.nearHazard) nearHazardCount++;

        sb.AppendLine();
        sb.AppendLine($"Total: {_segments.Count} segments — Walk:{walk} Jump:{jump} Dash:{dash} Jetpack:{jetpack} Unreachable:{unreachable}");
        sb.AppendLine($"Segments near hazards: {nearHazardCount} ({(nearHazardCount * 100f / _segments.Count):F0}% of path is dangerous)");

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[PathAnalyzer] Summary copied to clipboard.");
    }

    private void DrawAnalysisSummary()
    {
        if (_segments.Count == 0) return;

        GUILayout.Label("Segment Summary", EditorStyles.boldLabel);

        // Count each kind
        int walk = 0, jump = 0, dash = 0, jetpack = 0, unreachable = 0;
        foreach (var seg in _segments)
        {
            switch (seg.kind)
            {
                case SegmentKind.Walk:        walk++;        break;
                case SegmentKind.Jump:        jump++;        break;
                case SegmentKind.Dash:        dash++;        break;
                case SegmentKind.Jetpack:     jetpack++;     break;
                case SegmentKind.Unreachable: unreachable++; break;
            }
        }

        int total = _segments.Count;

        // Draw colored bars
        DrawSegmentBar("Walk",        walk,        total, ColorWalk);
        DrawSegmentBar("Jump",        jump,        total, ColorJump);
        DrawSegmentBar("Dash",        dash,        total, ColorDash);
        DrawSegmentBar("Jetpack",     jetpack,     total, ColorJetpack);
        DrawSegmentBar("Unreachable", unreachable, total, ColorUnreachable);

        // Hazard proximity count
        int nearHazard = 0;
        foreach (var seg in _segments)
            if (seg.nearHazard) nearHazard++;
        if (nearHazard > 0)
            DrawSegmentBar("Near Hazard", nearHazard, total, new Color(1f, 0.3f, 0f));

        if (unreachable > 0)
        {
            EditorGUILayout.HelpBox(
                $"{unreachable} segment(s) classified as Unreachable — " +
                "check for teleport-style respawns or very large gaps in the recorded path.",
                MessageType.Warning);
        }
    }

    private static void DrawSegmentBar(string label, int count, int total, Color color)
    {
        if (count == 0) return;

        EditorGUILayout.BeginHorizontal();

        // Label + count
        GUILayout.Label($"{label}: {count}", GUILayout.Width(120f));

        // Filled bar
        Rect barRect = GUILayoutUtility.GetRect(0f, 14f, GUILayout.ExpandWidth(true));
        float fraction = total > 0 ? (float)count / total : 0f;
        EditorGUI.DrawRect(barRect, new Color(0.25f, 0.25f, 0.25f));
        Rect filled = new Rect(barRect.x, barRect.y, barRect.width * fraction, barRect.height);
        EditorGUI.DrawRect(filled, color);

        // Percentage
        GUILayout.Label($"{(fraction * 100f):F0}%", GUILayout.Width(36f));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaypointList()
    {
        if (_waypoints.Count == 0) return;

        _showWaypointList = EditorGUILayout.Foldout(_showWaypointList,
            $"Waypoints ({_waypoints.Count})", true);
        if (!_showWaypointList) return;

        // Build a map from waypoint index → segment for quick lookup
        var segmentByFrom = new Dictionary<int, Segment>();
        foreach (var seg in _segments)
            segmentByFrom[seg.fromIndex] = seg;

        float lineH = EditorGUIUtility.singleLineHeight;
        int   visibleRows = Mathf.Min(_waypoints.Count + _segments.Count, 200);
        float contentH = visibleRows * lineH;

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
            GUILayout.Height(Mathf.Min(contentH + 8f, 260f)));

        for (int i = 0; i < _waypoints.Count; i++)
        {
            var wp = _waypoints[i];

            // Waypoint row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"[{i}]", GUILayout.Width(30f));
            GUILayout.Label($"({wp.x:F0}, {wp.y:F0})", GUILayout.Width(70f));
            GUILayout.Label(string.IsNullOrEmpty(wp.roomName) ? "—" : wp.roomName,
                GUILayout.Width(100f));

            if (GUILayout.Button("Ping", GUILayout.Width(40f)))
                PingWaypointInScene(i);

            EditorGUILayout.EndHorizontal();

            // Segment row (between this waypoint and the next)
            if (segmentByFrom.TryGetValue(i, out Segment seg))
            {
                Color prevColor = GUI.contentColor;
                GUI.contentColor = SegmentColor(seg.kind);

                string hazardTag = seg.nearHazard ? $"  ⚠ hazard {seg.hazardDistance:F1}u" : "";
                EditorGUI.indentLevel++;
                GUILayout.Label(
                    $"  → {seg.kind}  dx={seg.deltaX:+0.#;-0.#;0}  dy={seg.deltaY:+0.#;-0.#;0}{hazardTag}",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;

                GUI.contentColor = prevColor;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scene View gizmos
    // ─────────────────────────────────────────────────────────────────────────

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_waypoints.Count == 0) return;

        Handles.BeginGUI();
        Handles.EndGUI();

        // Draw waypoint dots
        for (int i = 0; i < _waypoints.Count; i++)
        {
            var wp = _waypoints[i];
            Vector3 pos = new Vector3(wp.x, wp.y, 0f);

            Handles.color = ColorWaypoint;
            Handles.DrawSolidDisc(pos, Vector3.forward, 0.18f);

            // Index label every 5th point to avoid clutter
            if (i % 5 == 0)
            {
                Handles.Label(pos + new Vector3(0.25f, 0.25f, 0f),
                    i.ToString(), EditorStyles.miniLabel);
            }
        }

        // Draw segment lines
        if (_segments.Count > 0)
        {
            foreach (var seg in _segments)
            {
                var a = _waypoints[seg.fromIndex];
                var b = _waypoints[seg.toIndex];

                Handles.color = SegmentColor(seg.kind);
                Handles.DrawLine(
                    new Vector3(a.x, a.y, 0f),
                    new Vector3(b.x, b.y, 0f),
                    2f);

                // Draw classification label at midpoint for problem segments
                if (seg.kind != SegmentKind.Walk)
                {
                    Vector3 mid = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
                    Handles.Label(mid + new Vector3(0f, 0.3f, 0f),
                        seg.kind.ToString(), EditorStyles.miniLabel);
                }

                // Draw hazard proximity warning
                if (seg.nearHazard)
                {
                    Vector3 mid = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
                    Handles.color = new Color(1f, 0.3f, 0f, 0.6f);
                    Handles.DrawSolidDisc(mid, Vector3.forward, 0.35f);
                    Handles.Label(mid + new Vector3(0.4f, -0.2f, 0f),
                        $"⚠ {seg.hazardDistance:F1}u", EditorStyles.miniLabel);
                }
            }
        }
        else
        {
            // No analysis yet: draw raw path in neutral white
            Handles.color = new Color(1f, 1f, 1f, 0.4f);
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(_waypoints[i].x,     _waypoints[i].y,     0f),
                    new Vector3(_waypoints[i + 1].x, _waypoints[i + 1].y, 0f),
                    1.5f);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Color SegmentColor(SegmentKind kind)
    {
        return kind switch
        {
            SegmentKind.Walk        => ColorWalk,
            SegmentKind.Jump        => ColorJump,
            SegmentKind.Dash        => ColorDash,
            SegmentKind.Jetpack     => ColorJetpack,
            SegmentKind.Unreachable => ColorUnreachable,
            _                       => Color.white
        };
    }

    private void PingWaypointInScene(int index)
    {
        if (index < 0 || index >= _waypoints.Count) return;

        var wp = _waypoints[index];
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;

        sv.pivot = new Vector3(wp.x, wp.y, 0f);
        sv.Repaint();
    }

    private void FocusSceneOnPath()
    {
        if (_waypoints.Count == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var wp in _waypoints)
        {
            if (wp.x < minX) minX = wp.x;
            if (wp.y < minY) minY = wp.y;
            if (wp.x > maxX) maxX = wp.x;
            if (wp.y > maxY) maxY = wp.y;
        }

        Vector3 center  = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        float   extentX = Mathf.Max(maxX - minX, 10f);
        float   extentY = Mathf.Max(maxY - minY, 10f);
        float   size    = Mathf.Max(extentX, extentY) * 0.6f;

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) sv = SceneView.sceneViews.Count > 0
            ? (SceneView)SceneView.sceneViews[0] : null;
        if (sv == null) return;

        sv.pivot    = center;
        sv.size     = size;
        sv.Repaint();
    }
}
#endif
