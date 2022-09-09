using UnityEngine;

namespace GameEngine.UserInput
{
    public static class Input
    {
        public static float VerticalAxis { get => UnityEngine.Input.GetAxisRaw("Vertical"); }
        public static float HorizontalAxis { get => UnityEngine.Input.GetAxisRaw("Horizontal"); }
        public static Vector3 MovementVector { get => new Vector3(HorizontalAxis, 0, VerticalAxis).normalized; }
        public static bool IsDashPressed { get => UnityEngine.Input.GetMouseButtonDown(0); }

        public static float MouseHorizontalAxis { get => UnityEngine.Input.GetAxisRaw("Mouse X"); }
        public static float MouseVerticalAxis { get => UnityEngine.Input.GetAxisRaw("Mouse Y"); }
    }
}