using UnityEngine;
using Mirror;
using Input = GameEngine.UserInput.Input;

namespace GameEngine.Core
{
    public class CameraBase : NetworkBehaviour
    {
#if UNITY_EDITOR || DEBUG_BUILD
        [SerializeField]
        protected bool _debugging = false;
#endif

        [SerializeField]
        protected Transform _cameraTransform;

        [SerializeField]
        protected Vector2 _sensitivity = new Vector2(1, 1);

        [field: SerializeField]
        protected float yMaxRotation { get; set; } = -90;
        [field: SerializeField]
        protected float yMinRotation { get; set; } = 90;

        protected float XRotation { get; set; }
        protected float YRotation { get; set; }

        protected virtual void Awake()
        {
            XRotation = transform.rotation.eulerAngles.y;
            YRotation = -transform.rotation.eulerAngles.x;
        }

        protected virtual void LateUpdate()
        {
            HandleRotation();
        }

        protected virtual void HandleRotation()
        {
            XRotation += Input.MouseHorizontalAxis * _sensitivity.x;
            YRotation += Input.MouseVerticalAxis * _sensitivity.y;

            YRotation = Mathf.Clamp(YRotation, yMaxRotation, yMinRotation);
            Quaternion rotation = Quaternion.Euler(-YRotation, XRotation, 0);

            _cameraTransform.rotation = rotation;
        }
    }
}