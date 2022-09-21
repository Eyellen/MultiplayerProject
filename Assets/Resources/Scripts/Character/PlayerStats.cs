using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            SceneManager.sceneUnloaded += ResetStaticEventsOnSceneUnload;
        }

        private void ResetStaticEventsOnSceneUnload(Scene loadedScene)
        {
            // Reset static events when scene is unloaded
            // Because otherwise it will cause errors
            StaticOnScorePoint = null;

            // Unsubscribe this method from static event SceneManager.sceneUnloaded after work is done
            SceneManager.sceneUnloaded -= ResetStaticEventsOnSceneUnload;
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
