using UnityEngine;

namespace GameEngine.Core
{
    public class ThirdPersonCamera : CameraBase
    {
        [field: Header("Third Person Camera Settings")]

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private Vector3 _cameraOffset = new Vector3(0, 0.4f, -5);
        private Vector3 _currentCameraOffset;

        [SerializeField]
        private float _cameraThickness = 0.25f;

        private int _layer;

        protected override void Awake()
        {
            base.Awake();

            _layer = 1 << LayerMask.NameToLayer("Player");
        }

        public override void OnStartClient()
        {
            // Disabling camera if it's not local player's camera
            if (!isLocalPlayer)
            {
                _cameraTransform.tag = "Untagged";
                _cameraTransform.gameObject.SetActive(false);
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            HandleOffsetMagnitude();
            HandleFollowing();
        }

        protected override void HandleRotation()
        {
            base.HandleRotation();

            _currentCameraOffset = _cameraTransform.rotation * _cameraOffset;
        }

        private void HandleOffsetMagnitude()
        {
            if (!Physics.SphereCast(_target.position, radius: _cameraThickness, _currentCameraOffset, out RaycastHit hitInfo,
                _currentCameraOffset.magnitude, ~_layer, QueryTriggerInteraction.Ignore)) return;

            Vector3 newOffset = hitInfo.point - _target.position;

            _currentCameraOffset = _currentCameraOffset.normalized * newOffset.magnitude;
        }

        private void HandleFollowing()
        {
            _cameraTransform.position = _target.position + _currentCameraOffset;

#if UNITY_EDITOR || DEBUG_BUILD
            if (_debugging)
            {
                Debug.DrawLine(_target.position, _target.position + _currentCameraOffset);
            }
#endif
        }
    }
}