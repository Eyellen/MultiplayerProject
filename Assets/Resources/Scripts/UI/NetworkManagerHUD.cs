using UnityEngine;
using GameEngine.User;
using GameEngine.Core;
using Mirror;

namespace GameEngine.UI
{
    public class NetworkManagerHUD : Mirror.NetworkManagerHUD
    {
        private string _currentUsername = string.Empty;

        protected override void StartButtons()
        {
            base.StartButtons();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Username</b>", GUILayout.Width(70));
            UserInfo.Username = GUILayout.TextField(UserInfo.Username);
            GUILayout.EndHorizontal();
        }

        protected override void StatusLabels()
        {
            _currentUsername = GetCurrentServerUsername();

            GUILayout.Label($"<b>Username:</b> {_currentUsername}");

            base.StatusLabels();
        }

        private string GetCurrentServerUsername()
        {
            if (NetworkClient.localPlayer == null) return string.Empty;

            return NetworkClient.localPlayer.gameObject.GetComponent<PlayerInfo>().Username;
        }
    }
}
