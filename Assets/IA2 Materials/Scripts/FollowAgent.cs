using UnityEngine;

public class FollowAgent : MonoBehaviour
{

    public Transform player; // Trascina qui il Taxi

    // Variabili private per memorizzare la posizione relativa iniziale
    private Vector3 _offsetPosizione;
    private Quaternion _offsetRotazione;

    void Start()
    {
        // 1. Calcola la differenza di posizione INIZIALE nello spazio locale del player
        // InverseTransformPoint converte da Mondo -> Locale
        // Esempio: "La camera è a -5 metri su Z (dietro) e +3 su Y (alto) rispetto al player"
        _offsetPosizione = player.InverseTransformPoint(transform.position);

        // 2. Calcola la differenza di rotazione INIZIALE
        // InverseTransformRotation (se non esiste nativamente, usiamo la matematica dei quaternioni)
        // La rotazione relativa è: (Inverso rotazione player) * (Rotazione camera)
        _offsetRotazione = Quaternion.Inverse(player.rotation) * transform.rotation;
    }

    void LateUpdate()
    {
        // 3. Applica la posizione mantenendo l'offset relativo
        // TransformPoint converte da Locale (il valore salvato) -> Mondo
        // Se il player ruota, questo punto ruota con lui.
        transform.position = player.TransformPoint(_offsetPosizione);

        // 4. Applica la rotazione mantenendo l'offset relativo
        // Moltiplichiamo la rotazione attuale del player per l'offset originale
        transform.rotation = player.rotation * _offsetRotazione;
    }
}
