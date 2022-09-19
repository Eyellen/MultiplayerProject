using System.Collections;
using UnityEngine;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterHitable : NetworkBehaviour, IHitable
    {
        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _normalColor = new Color(255, 255, 255);

        [SerializeField]
        private Color _invincibleColor = new Color(255, 0, 0);

        [SerializeField]
        private float _invincibleRollbackTime = 3f;

        [SyncVar(hook = nameof(OnIsInvincibleUpdate))]
        private bool _isInvincible;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_renderer != null)
            {
                _normalColor = _renderer.sharedMaterial.color;
            }
        }
#endif

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
            _renderer.material.color = newValue ? _invincibleColor : _normalColor;
        }
    }
}
