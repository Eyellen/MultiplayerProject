using Mirror;
using GameEngine.User;

namespace GameEngine.Core
{
    public class PlayerInfo : NetworkBehaviour
    {
        [field: SyncVar(hook = nameof(OnUsernameUpdateHook))]
        public string Username { get; private set; }

        private void Start()
        {
            if (!isLocalPlayer) return;

            CmdSetUsername(UserInfo.Username);
        }

        private void OnUsernameUpdateHook(string oldValue, string newValue)
        {
            Username = newValue;
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
