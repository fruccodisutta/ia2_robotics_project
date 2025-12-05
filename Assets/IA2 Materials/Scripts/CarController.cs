using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Car Physics")]
    public float maxSpeed = 10.0f;
    public float acceleration = 5.0f;
    public float steeringSpeed = 100.0f; // Gradi al secondo
    public float brakePower = 10.0f;

    [Header("Sensors")]
    public LidarAvoidance lidar;

    [Header("Pathfinding")]
    public List<Transform> waypoints;
    public float reachThreshold = 1.0f;

    // 1. AGGIUNGI QUESTA PROPRIETÀ PUBBLICA
    public float FinalSteerAngle { get; private set; }

    private int currentIndex = 0;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() // Usa FixedUpdate per la fisica!
    {
        if (waypoints.Count == 0 || currentIndex >= waypoints.Count) 
        {
            // Freno a mano se finito il percorso
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime);
            return;
        }

        Transform target = waypoints[currentIndex];

        // --- 1. CALCOLO STERZATA (STEERING) ---
        
        // Direzione ideale verso il waypoint
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        directionToTarget.y = 0; // Ignoriamo altezza

        // Direzione finale influenzata dal Lidar
        Vector3 desiredDirection = directionToTarget;
        float speedFactor = 1.0f;

        // 2. AGGIUNGI QUESTO SUBITO DOPO aver calcolato desiredDirection:
        // Calcoliamo l'angolo tra dove guarda l'auto (forward) e dove vuole andare (desiredDirection)
        // Questo include SIA il waypoint SIA l'evitamento ostacolo
        FinalSteerAngle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);

        if (lidar != null)
        {
            speedFactor = lidar.SpeedFactor;
            
            // Se il lidar dice di sterzare, aggiungiamo un vettore laterale
            // AvoidanceSteer è tra -1 (sx) e 1 (dx). 
            // Trasformiamo questo valore in una direzione relativa all'auto.
            if (Mathf.Abs(lidar.AvoidanceSteer) > 0.1f)
            {
                // Calcoliamo un vettore di "fuga"
                Vector3 avoidanceVector = transform.right * lidar.AvoidanceSteer;
                // Lo sommiamo alla direzione target (con un peso, es. 2.0 per dare priorità alla sicurezza)
                desiredDirection = directionToTarget + (avoidanceVector * 2.0f);
            }
        }

        // Calcoliamo la rotazione necessaria per guardare nella desiredDirection
        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
        
        // Ruotiamo fisicamente il Rigidbody verso quella direzione (Smooth)
        // MoveRotation rispetta la fisica (non teletrasporta)
        Quaternion nextRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, steeringSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nextRotation);


        // --- 2. CALCOLO TRAZIONE (THROTTLE) ---
        
        // Calcoliamo quanto siamo allineati col target (Dot Product)
        // Se stiamo guardando il target (valore ~1), acceleriamo.
        // Se dobbiamo girare molto (valore < 0.5), rallentiamo per fare la curva.
        float alignment = Vector3.Dot(transform.forward, desiredDirection.normalized);
        
        // Velocità desiderata: rallenta in curva e se ci sono ostacoli
        float targetSpeed = maxSpeed * speedFactor * Mathf.Clamp01(alignment);

        // Applichiamo la forza solo IN AVANTI (asse Z locale) -> Niente slittamento laterale!
        Vector3 currentForwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        
        if (currentForwardVelocity.magnitude < targetSpeed)
        {
            rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
        }
        
        // Limita la velocità massima laterale (drift killing)
        // Annulla la velocità laterale per evitare l'effetto "ghiaccio"
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = 0; // Uccidi asse X locale
        rb.linearVelocity = transform.TransformDirection(localVelocity);


        // --- 3. CHECK WAYPOINT ---
        if (Vector3.Distance(transform.position, target.position) < reachThreshold)
        {
            currentIndex++;
        }
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}