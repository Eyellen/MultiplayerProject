using UnityEngine;
using GameEngine.User;

namespace GameEngine.UI
{
    public class NetworkManagerHUD : Mirror.NetworkManagerHUD
    {
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
            GUILayout.Label($"<b>Username:</b> {UserInfo.Username}");

            base.StatusLabels();
        }
    }
}
