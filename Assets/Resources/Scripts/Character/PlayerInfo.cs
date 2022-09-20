using System;
using UnityEngine;
using Mirror;
using GameEngine.User;

namespace GameEngine.Core
{
    public class PlayerInfo : NetworkBehaviour
    {
        [field: SyncVar(hook = nameof(OnUsernameUpdateHook))]
        public string Username { get; private set; }

        public static event Action<PlayerInfo> OnPlayerStart;
        public static event Action<PlayerInfo> OnPlayerDestroy;

        public event Action<string> OnUsernameUpdate;

        private void Start()
        {
            OnPlayerStart?.Invoke(this);

            if(isLocalPlayer)
            {
                CmdSetUsername(UserInfo.Username);
            }
        }

        private void OnDestroy()
        {
            OnPlayerDestroy?.Invoke(this);

            // Clear static event from all subscriptions
            if (FindObjectsOfType<PlayerInfo>().Length == 0)
            {
                OnPlayerStart = null;
                OnPlayerDestroy = null;
            }
        }

        private void OnUsernameUpdateHook(string oldValue, string newValue)
        {
            Username = newValue;
            OnUsernameUpdate?.Invoke(Username);
        }

        [Command]
        private void CmdSetUsername(string username)
        {
            PlayerInfo[] playerInfos = FindObjectsOfType<PlayerInfo>();
            string[] otherNames = new string[playerInfos.Length];

            for (int i = 0; i < playerInfos.Length; i++)
            {
                otherNames[i] = playerInfos[i].Username;
            }

            Username = ServerUsernameRequirements.GetUniqueUsername(username, otherNames);
        }
    }
}
