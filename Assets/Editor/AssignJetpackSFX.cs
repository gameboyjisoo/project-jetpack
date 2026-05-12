using UnityEditor;
using UnityEngine;

public static class AssignJetpackSFX
{
    [MenuItem("Project Jetpack/Assign Jetpack SFX")]
    public static void Execute()
    {
        var feedback = Object.FindFirstObjectByType<JetpackAudioFeedback>();
        if (feedback == null)
        {
            Debug.LogError("JetpackAudioFeedback not found in scene.");
            return;
        }

        var so = new SerializedObject(feedback);

        // Engine burst clip
        SetClip(so, "burstClip", "Assets/Audio/Cave Story/SFX more/SE_14_2E.wav");

        // Empty click: disabled for now
        so.FindProperty("emptyClickClip").objectReferenceValue = null;

        // Wire engineSource to the AudioSource on the same GameObject
        var engineSrc = feedback.GetComponent<AudioSource>();
        if (engineSrc != null)
        {
            so.FindProperty("engineSource").objectReferenceValue = engineSrc;
            Debug.Log($"  engineSource -> {engineSrc.gameObject.name}");
        }

        // Wire clickSource to JetpackClick sibling's AudioSource
        var clickObj = feedback.transform.parent?.Find("JetpackClick");
        if (clickObj != null)
        {
            var clickSrc = clickObj.GetComponent<AudioSource>();
            if (clickSrc != null)
            {
                so.FindProperty("clickSource").objectReferenceValue = clickSrc;
                Debug.Log($"  clickSource -> {clickSrc.gameObject.name}");
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(feedback);
        Debug.Log("Jetpack SFX assigned successfully.");
    }

    private static void SetClip(SerializedObject so, string propName, string assetPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"Clip not found at {assetPath}");
            return;
        }

        so.FindProperty(propName).objectReferenceValue = clip;
        Debug.Log($"  {propName} -> {clip.name}");
    }
}
