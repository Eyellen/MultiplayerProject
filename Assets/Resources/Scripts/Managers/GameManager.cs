using System.Collections;
using UnityEngine;
using GameEngine.Patterns;
using Mirror;

namespace GameEngine.Core
{
    public class GameManager : NetworkBehaviour
    {
        public static Singleton<GameManager> Singleton { get; private set; } = new Singleton<GameManager>();

        [field: SyncVar]
        public bool IsGameOn { get; private set; }

        private void Start()
        {
            Singleton.Initialize(this);

            if(isServer)
            {
                IsGameOn = true;
                PlayerStats.StaticOnScorePoint += OnPlayerScoredPoint;
            }
        }

        [Server]
        private void OnPlayerScoredPoint(GameObject player, byte score)
        {
            // Don't execute if game is not running
            if (!IsGameOn) return;

            // First player to get 3 points wins
            if (score < 3) return;

            IsGameOn = false;
            string winnerName = player.GetComponent<PlayerInfo>().Username;
            MessageManager.Singleton.Instance.RpcShowTopMessage(winnerName + " won the game !");
            StartCoroutine(RestartGameCoroutine(5));
        }

        [Server]
        private IEnumerator RestartGameCoroutine(ushort inSeconds)
        {
            MessageManager.Singleton.Instance.RpcShowBottomMessage($"The game will restart in {inSeconds}");

            while (inSeconds > 0)
            {
                yield return new WaitForSeconds(1);
                inSeconds--;

                MessageManager.Singleton.Instance.RpcShowBottomMessage($"The game will restart in {inSeconds}");
            }

            MessageManager.Singleton.Instance.RpcHideAllMessages();
            RestartGame();
        }

        [Server]
        private void RestartGame()
        {
            StartCoroutine(RespawnAllPlayers());
            IsGameOn = true;
        }

        [Server]
        private IEnumerator RespawnAllPlayers()
        {
            // Destroying all characters
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in allPlayers)
            {
                NetworkServer.Destroy(player);
            }

            // Waiting 1 frame till all player instance will be destroyed
            yield return null;

            // Spawning characters for each connection
            foreach (var connection in NetworkServer.connections.Values)
            {
                NetworkManager.singleton.OnServerAddPlayer(connection);
            }
        }
    }
}
