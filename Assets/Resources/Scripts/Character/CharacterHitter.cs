using System;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterHitter : NetworkBehaviour
    {
        private CharacterBase _characterBase;

        public event Action OnCharacterHit;

        private void Start()
        {
            _characterBase = GetComponent<CharacterBase>();
        }

        [ServerCallback]
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_characterBase.CurrentState != CharacterState.Dash) return;

            if (!hit.gameObject.TryGetComponent(out IHitable hitable)) return;

            bool isSuccessful = hitable.Hit();
            if (!isSuccessful) return;

            // Calling event on server
            OnCharacterHit?.Invoke();
            // Calling event on client that owns this character
            // Don't call if this is local player because this player is also a server
            //  And it can cause double calling event
            if (!isLocalPlayer)
                TargetOnCharacterHit(connectionToClient);
        }

        [TargetRpc]
        private void TargetOnCharacterHit(NetworkConnection connection)
        {
            OnCharacterHit?.Invoke();
        }
    }
}
