using UnityEngine;

namespace GameEngine.Debugging
{
    public static class Gizmos
    {
        /// <summary>
        /// Draws a wireframe capsule with start, end and radius.
        /// </summary>
        /// <param name="start">Center of top sphere of capsule.</param>
        /// <param name="end">Center of bottom sphere of capsule.</param>
        /// <param name="radius"></param>
        public static void DrawWireCapsule(Vector3 start, Vector3 end, float radius)
        {
            // Draw capsule top sphere
            UnityEngine.Gizmos.DrawWireSphere(start, radius);
            // Draw capsule bottom sphere
            UnityEngine.Gizmos.DrawWireSphere(end, radius);

            // Draw edges of capsule
            UnityEngine.Gizmos.DrawLine(start + Vector3.forward * radius, end + Vector3.forward * radius);
            UnityEngine.Gizmos.DrawLine(start - Vector3.forward * radius, end - Vector3.forward * radius);
            UnityEngine.Gizmos.DrawLine(start + Vector3.right * radius, end + Vector3.right * radius);
            UnityEngine.Gizmos.DrawLine(start - Vector3.right * radius, end - Vector3.right * radius);
        }
    }
}
