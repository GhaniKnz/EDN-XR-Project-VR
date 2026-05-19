using UnityEngine;

/// <summary>
/// Attache au root du XR rig.
/// Copie position ET rotation complète du vaisseau (toutes directions)
/// pour que le vaisseau reste toujours centré devant le joueur.
/// Le head tracking du casque s'applique en plus de cette rotation de base.
/// </summary>
public class XRShipFollow : MonoBehaviour
{
    private Transform _ship;
    private static readonly Vector3 Offset = new Vector3(0f, 2.5f, -7f);

    [Tooltip("Vitesse de lissage de la rotation (plus élevé = plus réactif).")]
    public float yawSmoothSpeed = 6f;

    public void Init(Transform ship) => _ship = ship;

    private void LateUpdate()
    {
        if (_ship == null) return;

        // Position dans l'espace local du vaisseau → toujours derrière lui
        transform.position = _ship.TransformPoint(Offset);

        // Rotation complète lissée du vaisseau → le vaisseau reste au centre
        // quelle que soit la direction prise (gauche, droite, haut, bas, diagonale)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _ship.rotation,
            yawSmoothSpeed * Time.deltaTime);
    }
}
