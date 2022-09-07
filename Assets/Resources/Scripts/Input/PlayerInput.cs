using UnityEngine;

public static class PlayerInput
{
    public static float VerticalAxis { get => Input.GetAxisRaw("Vertical"); }
    public static float HorizontalAxis { get => Input.GetAxisRaw("Horizontal"); }
    public static Vector3 MovementVector { get => new Vector3(HorizontalAxis, 0, VerticalAxis).normalized; }
    public static bool IsDashPressed { get => Input.GetMouseButtonDown(0); }
}
