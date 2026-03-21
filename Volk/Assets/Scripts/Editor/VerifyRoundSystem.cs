using UnityEngine;
using UnityEditor;

public class VerifyRoundSystem
{
    [MenuItem("Tools/Verify Round System")]
    public static void Verify()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");
        Debug.Log("=== ROUND SYSTEM VERIFICATION ===");

        // 1. RoundCanvas
        var roundCanvas = GameObject.Find("RoundCanvas");
        Debug.Log($"1. RoundCanvas: {(roundCanvas != null ? "FOUND" : "MISSING")}");
        if (roundCanvas != null)
        {
            var roundUI = roundCanvas.GetComponent<RoundUI>();
            Debug.Log($"   RoundUI component: {(roundUI != null ? "FOUND" : "MISSING")}");
            if (roundUI != null)
            {
                Debug.Log($"     roundText: {(roundUI.roundText != null ? roundUI.roundText.name : "NULL")}");
                Debug.Log($"     fightText: {(roundUI.fightText != null ? roundUI.fightText.name : "NULL")}");
                Debug.Log($"     introGroup: {(roundUI.introGroup != null ? roundUI.introGroup.name : "NULL")}");
                Debug.Log($"     timerText: {(roundUI.timerText != null ? roundUI.timerText.name : "NULL")}");
                Debug.Log($"     resultText: {(roundUI.resultText != null ? roundUI.resultText.name : "NULL")}");
                Debug.Log($"     resultGroup: {(roundUI.resultGroup != null ? roundUI.resultGroup.name : "NULL")}");
                Debug.Log($"     matchResultPanel: {(roundUI.matchResultPanel != null ? roundUI.matchResultPanel.name : "NULL")}");
                Debug.Log($"     matchResultText: {(roundUI.matchResultText != null ? roundUI.matchResultText.name : "NULL")}");
                Debug.Log($"     restartText: {(roundUI.restartText != null ? roundUI.restartText.name : "NULL")}");
                Debug.Log($"     playerRoundDots: {(roundUI.playerRoundDots != null ? roundUI.playerRoundDots.Length.ToString() : "NULL")}");
                Debug.Log($"     enemyRoundDots: {(roundUI.enemyRoundDots != null ? roundUI.enemyRoundDots.Length.ToString() : "NULL")}");
            }
        }

        // 2-3. GameManager
        var gmGO = GameObject.Find("GameManager");
        Debug.Log($"2. GameManager GO: {(gmGO != null ? "FOUND" : "MISSING")}");
        if (gmGO != null)
        {
            var gm = gmGO.GetComponent<GameManager>();
            Debug.Log($"   GameManager component: {(gm != null ? "FOUND" : "MISSING")}");
            if (gm != null)
            {
                Debug.Log($"   roundUI: {(gm.roundUI != null ? gm.roundUI.gameObject.name : "NULL")}");
                Debug.Log($"   playerFighter: {(gm.playerFighter != null ? gm.playerFighter.gameObject.name : "NULL")}");
                Debug.Log($"   enemyFighter: {(gm.enemyFighter != null ? gm.enemyFighter.gameObject.name : "NULL")}");
                Debug.Log($"   totalRounds: {gm.totalRounds}");
                Debug.Log($"   roundDuration: {gm.roundDuration}");
            }
        }

        // 4. Check all RoundUI instances
        var allRoundUI = Object.FindObjectsByType<RoundUI>(FindObjectsSortMode.None);
        Debug.Log($"4. RoundUI instances in scene: {allRoundUI.Length}");
        foreach (var r in allRoundUI)
            Debug.Log($"   - on: {r.gameObject.name}");
    }
}
