using System;
using Mirror;

namespace GameEngine.Core
{
    public class PlayerStats : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnScoreUpdateHook))]
        private byte _score;

        public event Action<byte> OnScoreUpdate;

        private void Start()
        {
            GetComponent<CharacterHitter>().OnCharacterHit += ScorePoint;
        }

        [ServerCallback]
        public void ScorePoint()
        {
            _score++;
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

        private void OnScoreUpdateHook(byte oldValue, byte newValue)
        {
            OnScoreUpdate?.Invoke(newValue);
        }
    }
}
