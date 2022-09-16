using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

            NetworkClient.localPlayer.gameObject.GetComponent<CharacterHitter>().OnCharacterHit += ScorePoint;
        }

        private void ScorePoint(int hitCount)
        {
            _scoreCountText.text = hitCount.ToString();
        }
    }
}
