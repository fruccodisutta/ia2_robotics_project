using UnityEngine;

public class OrbitalCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Il Taxi
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Per guardare il tetto, non le ruote

    [Header("Settings")]
    public float distance = 10.0f; // Distanza iniziale
    public float xSpeed = 120.0f; // Sensibilità Mouse X
    public float ySpeed = 120.0f; // Sensibilità Mouse Y

    [Header("Limits")]
    public float yMinLimit = -20f; // Non andare sotto terra
    public float yMaxLimit = 80f;  // Non ribaltarti sopra

    [Header("Zoom")]
    public float minDistance = 2f;
    public float maxDistance = 20f;
    public float zoomSpeed = 5f;

    // Angoli interni (Coordinate Sferiche)
    private float x = 0.0f;
    private float y = 0.0f;

    void Start()
    {
        // Inizializza gli angoli basandosi sulla rotazione attuale della camera
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Opzionale: Se vuoi che il cursore sparisca mentre giochi
        // Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1. INPUT: Ruota solo se premiamo il tasto destro del mouse (Input 1)
        // Se vuoi che ruoti sempre, togli l'if.
        if (Input.GetMouseButton(1)) 
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }

        // 2. INPUT: Zoom con la rotellina
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 3. CLAMP: Limita l'angolo verticale per evitare capriole
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // 4. CALCOLO MATEMATICO (La parte ingegneristica)
        // Creiamo una rotazione basata sugli angoli Euleriani accumulati
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // Calcoliamo la posizione: Partiamo dal target, applichiamo la rotazione a un vettore "all'indietro" lungo 'distance'
        // Formula: Pos = Target + (Rot * (0, 0, -dist))
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position + targetOffset;

        // 5. APPLICAZIONE
        transform.rotation = rotation;
        transform.position = position;
    }

    // Funzione helper per gestire i 360 gradi puliti
    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}