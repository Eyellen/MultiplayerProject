using System.Collections;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterHitable : NetworkBehaviour, IHitable
    {
        private Material _normalMaterial;

        [SerializeField]
        private Material _invincibleMaterial;

        [SerializeField]
        private float _invincibleRollbackTime = 3f;

        [SyncVar(hook = nameof(OnIsInvincibleUpdate))]
        private bool _isInvincible;

        private void Start()
        {
            _normalMaterial = gameObject.GetComponent<Renderer>().material;
        }

        public bool Hit()
        {
            if (_isInvincible) return false;
            SetInvincible(true);

            StartCoroutine(InvincibleRollbackCoroutine());
            return true;
        }

        private IEnumerator InvincibleRollbackCoroutine()
        {
            yield return new WaitForSeconds(_invincibleRollbackTime);

            SetInvincible(false);
        }

        private void SetInvincible(bool isInvincible)
        {
            _isInvincible = isInvincible;
        }

        private void OnIsInvincibleUpdate(bool oldValue, bool newValue)
        {
            gameObject.GetComponent<Renderer>().material = newValue ? _invincibleMaterial : _normalMaterial;
        }
    }
}
