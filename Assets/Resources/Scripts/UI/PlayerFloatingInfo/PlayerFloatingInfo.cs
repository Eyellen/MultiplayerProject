using UnityEngine;
using TMPro;
using GameEngine.Core;

namespace GameEngine.UI
{
    public class PlayerFloatingInfo : MonoBehaviour
    {
        private Transform _transform;
        private Camera _camera;
        private Transform _playerTransform;

        [SerializeField]
        private TextMeshProUGUI _usernameText;

        public GameObject Player { get; set; }
        public float VerticalOffset { get; set; }

        private void Start()
        {
            _transform = GetComponent<Transform>();
            _camera = Camera.main;

            _usernameText.text = Player.GetComponent<PlayerInfo>().Username;

            _playerTransform = Player.transform;
            Player.GetComponent<PlayerInfo>().OnUsernameUpdate += OnUsernameUpdate;
        }

        private void LateUpdate()
        {
            UpdatePositionOnScreen();
        }

        private void UpdatePositionOnScreen()
        {
            _transform.position = _camera.WorldToScreenPoint(_playerTransform.position + Vector3.up * VerticalOffset);
        }

        private void OnUsernameUpdate(string newUsername)
        {
            _usernameText.text = newUsername;
        }
    }
}