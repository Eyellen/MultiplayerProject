using UnityEngine;
using Mirror;

namespace GameEngine.UI
{
    public class PlayerFloatingInfoManager : NetworkBehaviour
    {
        private Transform _transform;
        private Transform _cameraTransform;
        private Camera _camera;
        private PlayerFloatingInfo _playerFloatingInfo;

        private Transform _container;

        [Header("Floating Info Settings")]
        [SerializeField]
        private GameObject _playerFloatingInfoPrefab;

        [SerializeField]
        private float _visibleDistance = 10f;

        [SerializeField]
        private float _verticalOffset = 1.5f;

        [SerializeField]
        private bool _isShowOnSelf;

        private int _layer;

        private bool _isFloatingInfoActive;

        private void Start()
        {
            _layer = 1 << LayerMask.NameToLayer("Player");

            _transform = GetComponent<Transform>();
            _cameraTransform = Camera.main.transform;
            _camera = Camera.main;

            _container = GameObject.Find("Canvas/PlayerFloatingInfos").transform;

            InitializeFloatingInfo();
        }

        private void Update()
        {
            if (!_isShowOnSelf && isLocalPlayer)
            {
                HideFloatingInfo();
                return;
            }

            CheckIfNeedToShowOrHide();
        }

        private void OnDestroy()
        {
            Destroy(_playerFloatingInfo.gameObject);
        }

        private void CheckIfNeedToShowOrHide()
        {
            Vector3 player = _camera.WorldToViewportPoint(_transform.position);
            bool onScreen = player.z > 0 && (player.x > -0.1 && player.x < 1.1) && (player.y > -0.1 && player.y < 1.1);

            if (Vector3.Distance(_cameraTransform.position, _transform.position) > _visibleDistance || !onScreen)
            {
                HideFloatingInfo();
                return;
            }

            // Check if there is some object between player and camera
            if (Physics.Linecast(_cameraTransform.position, _transform.position + Vector3.up * _verticalOffset, ~_layer))
            {
                HideFloatingInfo();
                return;
            }

            ShowFloatingInfo();
        }

        private void ShowFloatingInfo()
        {
            if (_isFloatingInfoActive) return;

            _playerFloatingInfo.gameObject.SetActive(true);
            _isFloatingInfoActive = true;
        }

        private void HideFloatingInfo()
        {
            if (!_isFloatingInfoActive) return;

            _playerFloatingInfo.gameObject.SetActive(false);
            _isFloatingInfoActive = false;
        }

        private void InitializeFloatingInfo()
        {
            _playerFloatingInfo = Instantiate(_playerFloatingInfoPrefab, _container).GetComponent<PlayerFloatingInfo>();
            _isFloatingInfoActive = true;

            _playerFloatingInfo.Player = gameObject;
            _playerFloatingInfo.VerticalOffset = _verticalOffset;

            HideFloatingInfo();
        }
    }
}
