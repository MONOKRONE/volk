using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class VerifyHealthBars
{
    [MenuItem("Tools/Verify And Fix Health Bars")]
    public static void Verify()
    {
        FixBar("PlayerHealthBar", "Player_Root");
        FixBar("EnemyHealthBar", "Enemy_Root");
    }

    static void FixBar(string barName, string rootName)
    {
        var bar = GameObject.Find(barName);
        if (bar == null) { Debug.LogError(barName + " NOT FOUND"); return; }

        var hbui = bar.GetComponent<HealthBarUI>();
        if (hbui == null) { Debug.LogError(barName + " has no HealthBarUI!"); return; }

        var root = GameObject.Find(rootName);
        if (root == null) { Debug.LogError(rootName + " NOT FOUND"); return; }

        var fighter = root.GetComponent<Fighter>();

        // Fix target
        if (hbui.target == null || hbui.target != fighter)
        {
            hbui.target = fighter;
            EditorUtility.SetDirty(hbui);
            Debug.Log($"  {barName}: target SET to {rootName}");
        }
        else
        {
            Debug.Log($"  {barName}: target already = {hbui.target.gameObject.name}");
        }

        // Fix slider
        if (hbui.slider == null)
        {
            hbui.slider = bar.GetComponent<Slider>();
            EditorUtility.SetDirty(hbui);
            Debug.Log($"  {barName}: slider SET");
        }

        Debug.Log($"  {barName}: target={hbui.target?.gameObject.name ?? "NULL"}, slider={hbui.slider != null}");
    }
}
