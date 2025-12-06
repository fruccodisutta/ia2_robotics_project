/* using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarPhysics : MonoBehaviour
{
    [Header("Parameters")]
    public float maxSpeed = 15f;
    public float acceleration = 20f;
    public float turnSpeed = 5f;

    private Rigidbody rb;

    private Vector3 debugTargetDir;
    private Vector3 debugAvoidDir;
    private Vector3 debugFinalDir;

    [Header("Smoothing")]
    public float steeringSmoothness = 5.0f; // Più è basso, più è dolce (ma lento a reagire)

    private Vector3 currentVelocityVector; // Serve per il movimento fluido

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1500f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 2f;
        // Blocca rotazioni indesiderate (ribaltamento)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Drive(Vector3 targetPosition, Vector3 avoidanceVector)
    {
        // 1. Calcolo Direzione Grezza
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        // Se la forza di avoidance è 300, qui domina completamente.
        // Consiglio: Riporta avoidanceStrength nel Lidar a circa 5 o 10.
        Vector3 rawDirection = (directionToTarget + avoidanceVector).normalized;
        rawDirection.y = 0;

        if (rawDirection == Vector3.zero) return;

        // 2. INTERPOLAZIONE (Il segreto della fluidità)
        // Invece di guardare subito 'rawDirection', ci spostiamo gradualmente verso di essa.
        // Simuliamo il tempo che ci vuole a girare il volante.
        Vector3 smoothDirection = Vector3.Slerp(transform.forward, rawDirection, steeringSmoothness * Time.fixedDeltaTime);

        // 3. Rotazione Fisica
        Quaternion targetRot = Quaternion.LookRotation(smoothDirection);
        rb.MoveRotation(targetRot); // MoveRotation gestisce la fisica meglio di Slerp diretto sul RB

        // 4. Accelerazione (Smart Throttle)
        // Se stiamo sterzando molto, rallentiamo automaticamente (come un pilota vero)
        float corneringFactor = Vector3.Dot(transform.forward, rawDirection); // 1 = dritto, 0 = 90 gradi
        float dynamicSpeed = maxSpeed * Mathf.Clamp(corneringFactor, 0.5f, 1.0f); // Rallenta massimo al 50%

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (currentForwardSpeed < dynamicSpeed)
        {
            rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
        }

        // 5. Kill Lateral Velocity
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x = 0; 
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    void OnDrawGizmos()
    {
        // Freccia BIANCA: Dove voglio andare (Waypoint)
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position + Vector3.up, debugTargetDir * 3);

        // Freccia GIALLA: Dove il Lidar mi spinge (Evitamento)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up, debugAvoidDir * 3);

        // Freccia VERDE FLUO: Dove andrò davvero (Risultante)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up, debugFinalDir * 5);
        
    }

    public void Stop()
    {
        // Frenata attiva
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
    }
    
    // Metodo speciale per frenata d'emergenza (inchiodata)
    public void EmergencyBrake()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
} */

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarPhysics : MonoBehaviour
{
    [Header("Engine Settings")]
    public float maxSpeed = 15f;       // Velocità massima assoluta
    public float acceleration = 30f;   // Coppia motore
    public float brakePower = 60f;     // Potenza frenante
    
    [Header("Steering Settings")]
    public float turnSpeed = 10f;          // Reattività sterzo fisico
    public float steeringSmoothness = 5f;  // Fluidità visuale sterzo

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1500f;
        
        // Linear Damping simula la resistenza dell'aria
        rb.linearDamping = 0.2f; 
        rb.angularDamping = 5f; 
        
        // Vincoli fisici per non far ribaltare il taxi
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    /// <summary>
    /// Metodo principale di guida.
    /// </summary>
    /// <param name="targetPosition">Dove vogliamo andare (Waypoint)</param>
    /// <param name="avoidanceVector">Vettore repulsivo del Lidar (già pesato)</param>
    /// <param name="agentSpeedLimit">Limite di velocità imposto dal cervello (es. per pedoni)</param>
    public void Drive(Vector3 targetPosition, Vector3 avoidanceVector, float agentSpeedLimit)
    {
        // 1. CALCOLO VETTORE DIREZIONE
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        // FUSIONE: Sommiamo il desiderio di andare al target con la necessità di evitare ostacoli.
        // Normalize() è fondamentale qui per avere un vettore di lunghezza 1.
        Vector3 finalDirection = (directionToTarget + avoidanceVector).normalized;
        finalDirection.y = 0; // Blocchiamo l'asse verticale

        if (finalDirection == Vector3.zero) return;

        // 2. INTERPOLAZIONE STERZO (Smoothing)
        // Calcoliamo una direzione intermedia tra dove guardiamo ora e dove dobbiamo andare.
        // Questo evita che l'auto scatti di 90 gradi in un frame.
        Vector3 smoothDirection = Vector3.Slerp(transform.forward, finalDirection, steeringSmoothness * Time.fixedDeltaTime);

        // 3. CALCOLO VELOCITÀ FISICA SICURA
        // Calcoliamo quanto stiamo curvando. 1 = andiamo dritto, 0 = curva a 90 gradi.
        float cornerFactor = Vector3.Dot(transform.forward, smoothDirection);
        
        // Se stiamo curvando molto (cornerFactor basso), riduciamo la velocità massima fisica.
        // Esempio: In curva stretta, max speed diventa 5. In rettilineo è 15.
        float safePhysicalSpeed = Mathf.Lerp(3.0f, maxSpeed, cornerFactor);

        // 4. DECISIONE VELOCITÀ FINALE
        // La velocità target è la MINIMA tra:
        // - Quello che dice l'Agente (es. "C'è un pedone, vai a 2")
        // - Quello che dice la Fisica (es. "Curva stretta, vai a 5")
        float finalTargetSpeed = Mathf.Min(agentSpeedLimit, safePhysicalSpeed);

        // 5. APPLICAZIONE FORZE
        ApplySteering(smoothDirection);
        ApplyThrottleOrBrake(finalTargetSpeed);
        KillLateralVelocity();
    }

    // Ruota il Rigidbody verso la direzione desiderata
    void ApplySteering(Vector3 desiredDirection)
    {
        Quaternion targetRot = Quaternion.LookRotation(desiredDirection);
        // MoveRotation è la funzione corretta per i Rigidbody (gestisce collisioni durante la rotazione)
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }

    // Gestisce acceleratore e freno
    void ApplyThrottleOrBrake(float targetSpeed)
    {
        // A che velocità stiamo andando ORA nella direzione di marcia?
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (currentForwardSpeed < targetSpeed)
        {
            // ACCELERAZIONE
            rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);
        }
        else if (currentForwardSpeed > targetSpeed + 1.0f) // Isteresi di 1 km/h per non far vibrare il freno
        {
            // FRENATA
            // Se dobbiamo rallentare, applichiamo forza opposta
            rb.AddForce(-transform.forward * brakePower, ForceMode.Acceleration);
        }
    }

    // Simula l'attrito laterale delle gomme (evita l'effetto saponetta/drift)
    void KillLateralVelocity()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x = 0; // Azzera velocità laterale
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    // Frenata dolce per fermarsi al semaforo/arrivo
    public void Stop()
    {
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
    }

    // Inchiodata istantanea (Emergenza pedoni)
    public void EmergencyBrake()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}