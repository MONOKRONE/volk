using UnityEngine;
using UnityEditor;

public class SetupCamera
{
    [MenuItem("Tools/Setup Camera Follow")]
    public static void Setup()
    {
        var cam = GameObject.Find("Main Camera");
        if (cam == null) { Debug.LogError("Main Camera not found!"); return; }

        var cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.AddComponent<CameraFollow>();

        var player = GameObject.Find("Player_Maria");
        if (player == null) { Debug.LogError("Player_Maria not found!"); return; }

        cf.target = player.transform;
        EditorUtility.SetDirty(cf);
        EditorUtility.SetDirty(cam);

        Debug.Log($"CameraFollow target set to: {cf.target.name}");
        Debug.Log($"CameraFollow offset: {cf.offset}, smoothSpeed: {cf.smoothSpeed}");
    }
}
