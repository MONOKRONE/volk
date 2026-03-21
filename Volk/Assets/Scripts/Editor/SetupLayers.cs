using UnityEngine;
using UnityEditor;

public class SetupLayers
{
    [MenuItem("Tools/Setup Hitbox Hurtbox Layers")]
    public static void Setup()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");

        // Set layer 8 = Hitbox, layer 9 = Hurtbox
        SetLayer(layersProp, 8, "Hitbox");
        SetLayer(layersProp, 9, "Hurtbox");
        tagManager.ApplyModifiedProperties();

        // Physics collision matrix: Hitbox only collides with Hurtbox
        // Disable Hitbox vs everything except Hurtbox
        for (int i = 0; i < 32; i++)
        {
            // Hitbox (8) vs layer i
            if (i == 9) // Hurtbox
                Physics.IgnoreLayerCollision(8, i, false); // enable
            else
                Physics.IgnoreLayerCollision(8, i, true); // disable

            // Hurtbox (9) vs layer i
            if (i == 8) // Hitbox
                Physics.IgnoreLayerCollision(9, i, false); // enable
            else
                Physics.IgnoreLayerCollision(9, i, true); // disable
        }

        Debug.Log("Layers set: 8=Hitbox, 9=Hurtbox");
        Debug.Log("Physics: Hitbox only collides with Hurtbox");
    }

    static void SetLayer(SerializedProperty layers, int index, string name)
    {
        var prop = layers.GetArrayElementAtIndex(index);
        prop.stringValue = name;
        Debug.Log($"Layer {index} set to '{name}'");
    }
}
