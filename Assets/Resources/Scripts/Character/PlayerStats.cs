using System;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class PlayerStats : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnScoreUpdateHook))]
        private byte _score;

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
            _score++;
            StaticOnScorePoint?.Invoke(gameObject, _score);
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
                stats._score = default(byte);
            }
        }
    }
}
