using UnityEngine;
using GameEngine.Patterns;

namespace GameEngine.Core
{
    public class GameManager : MonoBehaviour
    {
        public static Singleton<GameManager> Singleton { get; private set; } = new Singleton<GameManager>();

        private void Start()
        {
            Singleton.Initialize(this);
            PlayerStats.StaticOnScorePoint += OnPlayerScoredPoint;
        }

        private void OnPlayerScoredPoint(GameObject player, byte score)
        {
            if (score < 3) return;

            string winnerName = player.GetComponent<PlayerInfo>().Username;
            MessageManager.Singleton.Instance.RpcShowTopMessage(winnerName + " won the game !");
        }
    }
}
