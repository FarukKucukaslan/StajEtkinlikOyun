using UnityEngine;

// Drop this on any enemy that tips over (e.g. from animation root motion).
// Runs after everything else each frame and forces X/Z rotation back to zero,
// leaving only the Y-axis (facing direction) free.
public class KeepUpright : MonoBehaviour
{
    private void LateUpdate()
    {
        Vector3 eulerAngles = transform.eulerAngles;

        if (Mathf.Approximately(eulerAngles.x, 0f) && Mathf.Approximately(eulerAngles.z, 0f))
        {
            return;
        }

        transform.rotation = Quaternion.Euler(0f, eulerAngles.y, 0f);
    }
}
