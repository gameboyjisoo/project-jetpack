using UnityEditor;
using UnityEngine;

public static class DiagnoseJetpackAudio
{
    [MenuItem("Project Jetpack/Diagnose Jetpack Audio")]
    public static void Execute()
    {
        // Check AudioListener
        var listener = Object.FindFirstObjectByType<AudioListener>();
        Debug.Log(listener != null
            ? $"[OK] AudioListener on: {listener.gameObject.name}"
            : "[FAIL] No AudioListener in scene!");

        // Check JetpackAudioFeedback
        var feedback = Object.FindFirstObjectByType<JetpackAudioFeedback>();
        if (feedback == null)
        {
            Debug.LogError("[FAIL] JetpackAudioFeedback not found in scene.");
            return;
        }
        Debug.Log($"[OK] JetpackAudioFeedback on: {feedback.gameObject.name}");

        // Check references via SerializedObject
        var so = new SerializedObject(feedback);

        var engineSrc = so.FindProperty("engineSource").objectReferenceValue;
        Debug.Log(engineSrc != null
            ? $"[OK] engineSource assigned: {engineSrc.name}"
            : "[FAIL] engineSource is null!");

        var burstClip = so.FindProperty("burstClip").objectReferenceValue;
        Debug.Log(burstClip != null
            ? $"[OK] burstClip assigned: {burstClip.name}"
            : "[FAIL] burstClip is null!");

        var clickSrc = so.FindProperty("clickSource").objectReferenceValue;
        Debug.Log(clickSrc != null
            ? $"[OK] clickSource assigned: {clickSrc.name}"
            : "[FAIL] clickSource is null!");

        var emptyClip = so.FindProperty("emptyClickClip").objectReferenceValue;
        Debug.Log(emptyClip != null
            ? $"[OK] emptyClickClip assigned: {emptyClip.name}"
            : "[FAIL] emptyClickClip is null!");

        // Check PlayerController reachable
        var pc = feedback.GetComponentInParent<PlayerController>();
        Debug.Log(pc != null
            ? $"[OK] PlayerController found on: {pc.gameObject.name}"
            : "[FAIL] PlayerController not found in parent!");

        var gas = feedback.GetComponentInParent<JetpackGas>();
        Debug.Log(gas != null
            ? $"[OK] JetpackGas found on: {gas.gameObject.name}"
            : "[FAIL] JetpackGas not found in parent!");

        // Try playing the clip directly
        if (burstClip != null && engineSrc is AudioSource src)
        {
            src.PlayOneShot((AudioClip)burstClip, 1f);
            Debug.Log("[TEST] Played burstClip once — did you hear it?");
        }
    }
}
