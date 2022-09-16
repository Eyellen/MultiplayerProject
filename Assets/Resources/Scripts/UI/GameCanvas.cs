using System.Collections;
using UnityEngine;
using TMPro;
using GameEngine.Core;
using GameEngine.Patterns;
using Mirror;

namespace GameEngine.UI
{
    public class GameCanvas : MonoBehaviour
    {
        public static Singleton<GameCanvas> Singleton { get; private set; } = new Singleton<GameCanvas>();

        [SerializeField]
        private TextMeshProUGUI _scoreCountText;

        private IEnumerator Start()
        {
            Singleton.Initialize(this);

            while (NetworkClient.localPlayer == null)
                yield return null;

            NetworkClient.localPlayer.gameObject.GetComponent<PlayerStats>().OnScoreUpdate += ScorePoint;
        }

        private void ScorePoint(byte hitCount)
        {
            _scoreCountText.text = hitCount.ToString();
        }
    }
}
