using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterHitter : NetworkBehaviour
    {
        private CharacterBase _characterBase;

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
        }
    }
}
