using System;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class PlayerStats : NetworkBehaviour
    {
        [field: SyncVar(hook = nameof(OnScoreUpdateHook))]
        public byte Score { get; private set; }

        public event Action<byte> OnScoreUpdate;

        public static event Action<GameObject, byte> StaticOnScorePoint;

        private void Start()
        {
            GetComponent<CharacterHitter>().OnCharacterHit += ScorePoint;
        }

        private void OnDestroy()
        {
            // Reset static event because otherwise it will cause errors
            // Resets only if there is no instances of PlayerStats on scene
            //      Because otherwise it will unsubscribe existing instances
            if (FindObjectsOfType<PlayerStats>().Length == 0)
                StaticOnScorePoint = null;
        }

        [ServerCallback]
        public void ScorePoint()
        {
            Score++;
            StaticOnScorePoint?.Invoke(gameObject, Score);
        }

        private void OnScoreUpdateHook(byte oldValue, byte newValue)
        {
            OnScoreUpdate?.Invoke(newValue);
        }

        /// <summary>
        /// Server method. Resets scores for every PlayerStats
        /// </summary>
        [Server]
        public static void ResetAllScores()
        {
            PlayerStats[] allStats = FindObjectsOfType<PlayerStats>();

            foreach (var stats in allStats)
            {
                stats.Score = default(byte);
            }
        }
    }
}
