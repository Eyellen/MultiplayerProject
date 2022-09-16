using System;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterHitter : NetworkBehaviour
    {
        private CharacterBase _characterBase;

        [field: SyncVar]
        public int TotalHits { get; private set; }

        public event Action<int> OnCharacterHit;

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

            TotalHits++;
            TargetOnCharacterHit(connectionToClient, TotalHits);
        }

        [TargetRpc]
        private void TargetOnCharacterHit(NetworkConnection connection, int hitCount)
        {
            TotalHits = hitCount;
            OnCharacterHit?.Invoke(hitCount);
        }
    }
}
