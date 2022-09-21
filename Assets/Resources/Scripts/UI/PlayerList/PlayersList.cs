using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine.Core;

namespace GameEngine.UI
{
    public class PlayersList : MonoBehaviour
    {
        [SerializeField]
        private Transform _container;

        [SerializeField]
        private GameObject _playerBarPrefab;

        private Dictionary<PlayerInfo, PlayerListBar> _playerBars = new Dictionary<PlayerInfo, PlayerListBar>();

        private void Start()
        {
            PlayerInfo.OnPlayerStart += OnPlayerStart;
            PlayerInfo.OnPlayerDestroy += OnPlayerDestroy;
        }

        private void InitializePlayerList()
        {
            PlayerInfo[] allPlayerInfos = FindObjectsOfType<PlayerInfo>();

            foreach (PlayerInfo playerInfo in allPlayerInfos)
            {
                AddPlayerToList(playerInfo);
            }
        }

        private void ClearPlayerList()
        {
            foreach (PlayerListBar playerBar in _playerBars.Values)
            {
                Destroy(playerBar.gameObject);
            }
        }

        private void AddPlayerToList(PlayerInfo playerInfo)
        {
            PlayerListBar playerBar = Instantiate(_playerBarPrefab, _container).GetComponent<PlayerListBar>();
            playerBar.Player = playerInfo.gameObject;

            _playerBars[playerInfo] = playerBar;
        }

        private void RemovePlayerFromList(PlayerInfo playerInfo)
        {
            if (_playerBars[playerInfo] != null)
                Destroy(_playerBars[playerInfo].gameObject);
            _playerBars.Remove(playerInfo);
        }

        private void OnPlayerStart(PlayerInfo playerInfo)
        {
            AddPlayerToList(playerInfo);
        }

        private void OnPlayerDestroy(PlayerInfo playerInfo)
        {
            RemovePlayerFromList(playerInfo);
        }
    }
}
