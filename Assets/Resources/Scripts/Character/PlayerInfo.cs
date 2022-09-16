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
            Username = username;
        }
    }
}
