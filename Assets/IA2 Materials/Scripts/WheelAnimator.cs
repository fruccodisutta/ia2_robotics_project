using UnityEngine;

public class WheelAnimator : MonoBehaviour
{
    [Header("Setup Ruote")]
    public Transform[] frontWheels; // Trascina qui le 2 ruote davanti
    public Transform[] rearWheels;  // Trascina qui le 2 ruote dietro

    [Header("Settings")]
    public float wheelRadius = 0.33f;
    public float maxSteerAngle = 30f;
    
    // IMPORTANTE: Se le ruote girano "storte" (es. di taglio), cambia questo asse.
    // (1, 0, 0) è l'asse X (Rosso). (0, 0, 1) è l'asse Z (Blu).
    public Vector3 spinAxis = Vector3.right; 

    [Header("References")]
    public Rigidbody rb;
    public CarController controller;

    // Struttura dati per memorizzare la rotazione iniziale di OGNI ruota
    private Quaternion[] frontOriginalRots;
    private Quaternion[] rearOriginalRots;
    
    // Accumulatore per quanto hanno girato le ruote (altrimenti resettano ogni frame)
    private float totalRoll = 0f;

    void Start()
    {
        // 1. MEMORIZZAZIONE
        // Salviamo esattamente come sono ruotate le ruote quando premi Play.
        // Lo script userà questo come "Zero".
        
        frontOriginalRots = new Quaternion[frontWheels.Length];
        for (int i = 0; i < frontWheels.Length; i++)
            frontOriginalRots[i] = frontWheels[i].localRotation;

        rearOriginalRots = new Quaternion[rearWheels.Length];
        for (int i = 0; i < rearWheels.Length; i++)
            rearOriginalRots[i] = rearWheels[i].localRotation;
    }

    /* void Update()
    {
        if (rb == null || controller == null) return;

        // --- CALCOLI FISICI ---
        
        // 1. Calcolo quanto devono ruotare (Rotolamento)
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity); 
        // Calcoliamo l'incremento di angolo per questo frame
        float rollStep = (localVelocity.z * Time.deltaTime / wheelRadius) * Mathf.Rad2Deg;
        totalRoll += rollStep; // Accumuliamo l'angolo totale

        // 2. Calcolo quanto devono sterzare (Yaw)
        float steerAngle = 0f;
        if (controller.waypoints.Count > 0) 
        {
            Transform currentTarget = controller.waypoints[controller.GetCurrentIndex()];
            Vector3 targetDir = currentTarget.position - transform.position;
            steerAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
            steerAngle = Mathf.Clamp(steerAngle, -maxSteerAngle, maxSteerAngle);
        }

        // --- APPLICAZIONE ALLE RUOTE ---

        // Ruote Anteriori (Sterzano + Rotolano)
        for (int i = 0; i < frontWheels.Length; i++)
        {
            ApplyRotation(frontWheels[i], frontOriginalRots[i], steerAngle, totalRoll);
        }

        // Ruote Posteriori (Solo Rotolano)
        for (int i = 0; i < rearWheels.Length; i++)
        {
            ApplyRotation(rearWheels[i], rearOriginalRots[i], 0f, totalRoll);
        }
    } */

    void Update()
    {
        if (rb == null || controller == null) return;

        // --- CALCOLI FISICI ---
        
        // 1. Rotolamento (Resta uguale)
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity); 
        float rollStep = (localVelocity.z * Time.deltaTime / wheelRadius) * Mathf.Rad2Deg;
        totalRoll += rollStep;

        // 2. STERZATA (MODIFICATO)
        // Non calcoliamo più l'angolo qui. Lo leggiamo dal controller fisico.
        // Così se il Lidar fa sterzare l'auto, le ruote lo seguono.
        float steerAngle = controller.FinalSteerAngle;

        // Limitiamo solo per estetica (per non farle compenetrare nel passaruota)
        steerAngle = Mathf.Clamp(steerAngle, -maxSteerAngle, maxSteerAngle);
        
        // Nota: Se per caso girano ancora al contrario (es. sinistra invece di destra),
        // cambia la riga sopra in: steerAngle = -steerAngle;


        // --- APPLICAZIONE ALLE RUOTE (Resta uguale) ---
        for (int i = 0; i < frontWheels.Length; i++)
            ApplyRotation(frontWheels[i], frontOriginalRots[i], steerAngle, totalRoll);

        for (int i = 0; i < rearWheels.Length; i++)
            ApplyRotation(rearWheels[i], rearOriginalRots[i], 0f, totalRoll);
    }

    // La funzione magica che combina tutto senza rompere l'offset
    void ApplyRotation(Transform wheel, Quaternion originalRot, float steer, float roll)
    {
        if (wheel == null) return;

        // MATEMATICA:
        // 1. Creiamo la rotazione della sterzata (Attorno all'asse Y dell'auto/padre)
        Quaternion steeringRot = Quaternion.AngleAxis(steer, Vector3.up);

        // 2. Creiamo la rotazione del rotolamento (Attorno all'asse scelto, es X)
        Quaternion rollingRot = Quaternion.AngleAxis(roll, spinAxis);

        // 3. COMBINAZIONE (L'ordine è cruciale!)
        // Applico la Sterzata -> Poi applico la rotazione Originale del modello -> Poi applico il Rotolamento locale
        wheel.localRotation = steeringRot * originalRot * rollingRot;
    }
}