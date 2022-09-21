using System.Collections;
using UnityEngine;
using TMPro;
using GameEngine.Core;

namespace GameEngine.UI
{
    public class PlayerListBar : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _usernameText;
        [SerializeField]
        private TextMeshProUGUI _scoreText;

        private PlayerInfo _playerInfo;
        private PlayerStats _playerStats;

        private GameObject _player;
        public GameObject Player
        {
            get => _player;
            set
            {
                _player = value;
                _playerInfo = _player.GetComponent<PlayerInfo>();
                _playerStats = _player.GetComponent<PlayerStats>();
            }
        }
        private string Username { get => _usernameText.text; set => _usernameText.text = value; }
        private byte Score { get => byte.Parse(_scoreText.text); set => _scoreText.text = value.ToString(); }

        private IEnumerator _updateCoroutine;

        private void OnEnable()
        {
            if (_updateCoroutine != null)
                StopCoroutine(_updateCoroutine);
            _updateCoroutine = UpdateCoroutine();
            StartCoroutine(_updateCoroutine);
        }

        private void OnDisable()
        {
            if (_updateCoroutine != null)
                StopCoroutine(_updateCoroutine);
        }

        private IEnumerator UpdateCoroutine()
        {
            while(true)
            {
                if (_player != null)
                {
                    Username = _playerInfo.Username;
                    Score = _playerStats.Score;
                }

                // Wait for 200 ms, i.e. this method executes once per 200 ms
                yield return new WaitForSeconds(0.2f);
                yield return null;
            }
        }
    }
}
