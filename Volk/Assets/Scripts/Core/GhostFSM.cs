using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    /// <summary>
    /// Ghost AI FSM that uses PlayerBehaviorTracker data to make weighted decisions.
    /// Attach to AI-controlled fighters in Ghost mode.
    /// </summary>
    public class GhostFSM : MonoBehaviour
    {
        [Header("Profile")]
        public string profileMatchup; // e.g. "YILDIZ_vs_KAYA"

        [Header("Tuning")]
        public float decisionInterval = 0.3f;
        public float randomWeight = 0.2f; // 20% random for unpredictability

        private Fighter fighter;
        private PlayerBehaviorTracker tracker;
        private float decisionTimer;
        private Dictionary<string, float[]> actionWeights = new Dictionary<string, float[]>();

        // PlayerAction indices for weight array
        static readonly PlayerAction[] AllActions = {
            PlayerAction.Punch, PlayerAction.Kick, PlayerAction.HeavyPunch,
            PlayerAction.Skill1, PlayerAction.Skill2, PlayerAction.Block,
            PlayerAction.Parry, PlayerAction.Retreat, PlayerAction.Approach, PlayerAction.Wait
        };

        void Start()
        {
            fighter = GetComponent<Fighter>();
            tracker = PlayerBehaviorTracker.Instance;

            if (tracker != null)
                BuildWeightTable();
        }

        void BuildWeightTable()
        {
            actionWeights.Clear();

            foreach (GameSituation sit in System.Enum.GetValues(typeof(GameSituation)))
            {
                string key = $"{profileMatchup}_{sit}";
                float[] weights = new float[AllActions.Length];

                // Default uniform weights
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = 1f;

                // Override with tracked data
                for (int i = 0; i < AllActions.Length; i++)
                {
                    var mostLikely = tracker.GetMostLikelyAction(profileMatchup, sit);
                    if (mostLikely.HasValue && mostLikely.Value == AllActions[i])
                        weights[i] = 5f; // Heavily favor the player's most common action
                }

                // Normalize
                float total = 0;
                foreach (float w in weights) total += w;
                if (total > 0)
                    for (int i = 0; i < weights.Length; i++)
                        weights[i] /= total;

                actionWeights[sit.ToString()] = weights;
            }
        }

        void Update()
        {
            if (fighter == null || !fighter.isAI) return;

            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0) return;
            decisionTimer = decisionInterval;

            var situation = GetSituation();
            var action = PickAction(situation);
            ExecuteAction(action);
        }

        GameSituation GetSituation()
        {
            if (fighter.currentHP / fighter.maxHP < 0.3f)
                return GameSituation.LowHP;
            if (fighter.currentHP / fighter.maxHP > 0.8f)
                return GameSituation.HighHP;

            if (fighter.aiTarget != null)
            {
                float dist = Vector3.Distance(transform.position, fighter.aiTarget.position);
                if (dist > 4f) return GameSituation.NeutralFar;
                if (dist > 2f) return GameSituation.NeutralMid;
                return GameSituation.NeutralClose;
            }
            return GameSituation.NeutralMid;
        }

        PlayerAction PickAction(GameSituation situation)
        {
            string key = situation.ToString();
            if (!actionWeights.ContainsKey(key))
                return PlayerAction.Wait;

            float[] weights = actionWeights[key];

            // Add randomness for unpredictability
            float[] adjusted = new float[weights.Length];
            float total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                adjusted[i] = weights[i] * (1f - randomWeight) + (1f / weights.Length) * randomWeight;
                total += adjusted[i];
            }

            // Weighted random selection
            float roll = Random.value * total;
            float cumulative = 0;
            for (int i = 0; i < adjusted.Length; i++)
            {
                cumulative += adjusted[i];
                if (roll <= cumulative)
                    return AllActions[i];
            }
            return PlayerAction.Wait;
        }

        void ExecuteAction(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Punch:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Normal);
                    break;
                case PlayerAction.Kick:
                    fighter.DoAttack(AttackType.Kick, AttackVariant.Normal);
                    break;
                case PlayerAction.HeavyPunch:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Heavy);
                    break;
                case PlayerAction.Skill1:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Special);
                    break;
                case PlayerAction.Skill2:
                    fighter.DoAttack(AttackType.Kick, AttackVariant.Special);
                    break;
                case PlayerAction.Block:
                    // Block handled by AI state machine
                    break;
                case PlayerAction.Retreat:
                    // Trigger retreat in AI
                    break;
                case PlayerAction.Approach:
                    // Default AI behavior
                    break;
                case PlayerAction.Wait:
                    break;
            }
        }

        /// <summary>
        /// Load a ghost profile from JSON and rebuild weight table.
        /// </summary>
        public void LoadProfile(string matchup)
        {
            profileMatchup = matchup;
            if (tracker != null)
            {
                tracker.LoadProfile();
                BuildWeightTable();
            }
        }
    }
}
