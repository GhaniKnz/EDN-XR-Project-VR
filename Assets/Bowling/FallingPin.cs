using UnityEngine;

public class FallingPin : MonoBehaviour
{
    public float fallAngleThreshold = 60f;
    public bool isFallen = false;

    void Update()
    {
        if (isFallen) return;

        // IMPORTANT: pour tes quilles (rotation -90), forward correspond à "l'axe vertical"
        float angle = Vector3.Angle(transform.up, Vector3.down);
        Debug.Log($"Pin {gameObject.name} angle: {angle}");

        if (angle < fallAngleThreshold)
            isFallen = true;
    }
}