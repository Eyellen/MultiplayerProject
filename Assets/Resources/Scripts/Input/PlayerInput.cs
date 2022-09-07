using UnityEngine;

public static class PlayerInput
{
    public static float VerticalAxis { get => Input.GetAxisRaw("Vertical"); }
    public static float HorizontalAxis { get => Input.GetAxisRaw("Horizontal"); }
    public static bool IsDashPressed { get => Input.GetMouseButtonDown(0); }
}
