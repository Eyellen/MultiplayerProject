using UnityEngine;
using TMPro;
using GameEngine.Core;
using GameEngine.Patterns;

namespace GameEngine.UI
{
    public class GameCanvas : MonoBehaviour
    {
        public static Singleton<GameCanvas> Singleton { get; private set; } = new Singleton<GameCanvas>();

        [SerializeField]
        private TextMeshProUGUI _scoreCountText;

        private void Start()
        {
            Singleton.Initialize(this);

            PlayerInfo.OnPlayerStart += OnPlayerStart; 
        }

        private void ScorePoint(byte hitCount)
        {
            _scoreCountText.text = hitCount.ToString();
        }

        private void OnPlayerStart(PlayerInfo playerInfo)
        {
            // Do nothing if this is not local player
            if (!playerInfo.isLocalPlayer) return;

            PlayerStats playerStats = playerInfo.gameObject.GetComponent<PlayerStats>();

            // Initialize event to display player's scores
            playerStats.OnScoreUpdate += ScorePoint;

            // Set current player's score
            ScorePoint(playerStats.Score);
        }
    }
}
