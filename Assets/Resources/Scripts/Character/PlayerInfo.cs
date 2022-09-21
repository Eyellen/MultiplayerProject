using System;
using UnityEngine.SceneManagement;
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
            SceneManager.sceneUnloaded += ResetStaticEventsOnSceneUnload;

            if(isLocalPlayer)
            {
                CmdSetUsername(UserInfo.Username);
            }
        }

        private void OnDestroy()
        {
            OnPlayerDestroy?.Invoke(this);
        }

        private void ResetStaticEventsOnSceneUnload(Scene unloadedScene)
        {
            // Reset static events when scene is unloaded
            // Because otherwise it will cause errors
            OnPlayerStart = null;
            OnPlayerDestroy = null;

            // Unsubscribe this method from static event SceneManager.sceneUnloaded after work is done
            SceneManager.sceneUnloaded -= ResetStaticEventsOnSceneUnload;
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
