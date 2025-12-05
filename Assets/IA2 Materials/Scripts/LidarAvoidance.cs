using UnityEngine;

public class LidarAvoidance : MonoBehaviour
{
    [Header("Lidar Settings")]
    public int numberOfRays = 15;      // Risoluzione del sensore
    public float fieldOfView = 90f;    // Angolo di visione (es. 90 gradi)
    public float range = 10f;          // Distanza massima di rilevamento
    public LayerMask obstacleLayer;    // Quali layer sono ostacoli (es. 'Default' o 'Buildings')

    [Header("Reaction")]
    public float avoidanceForce = 1.0f; // Quanto bruscamente sterza per evitare
    public bool emergencyStop = false;  // Flag di lettura per altri script

    // Output per il sistema di guida
    public float AvoidanceSteer { get; private set; } // -1 (sinistra) a +1 (destra)
    public float SpeedFactor { get; private set; }    // 0 (stop) a 1 (full speed)

    void Update()
    {
        ScanEnvironment();
    }

    void ScanEnvironment()
    {
        AvoidanceSteer = 0f;
        SpeedFactor = 1f;
        emergencyStop = false;

        float angleStep = fieldOfView / (numberOfRays - 1);
        float startAngle = -fieldOfView / 2;
        // DEFINISCI L'ORIGINE DEL SENSORE (Alzata di 1 metro)
        Vector3 sensorPos = transform.position + (Vector3.up * 1.0f);

        int hitCount = 0;

        for (int i = 0; i < numberOfRays; i++)
        {
            // 1. Calcolo Direzione Raggio (Matematica Vettoriale)
            float currentAngle = startAngle + (angleStep * i);
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            // Combina la rotazione dell'auto con quella del raggio
            Vector3 rayDir = transform.rotation * rotation * Vector3.forward;

            RaycastHit hit;
            // USA sensorPos INVECE DI transform.position
            bool isHit = Physics.Raycast(sensorPos, rayDir, out hit, range, obstacleLayer);

            // 2. Visualizzazione Debug (Verde/Rosso)
            Color rayColor = isHit ? Color.red : Color.green;
            // ANCHE QUI: Disegna partendo da sensorPos
            Debug.DrawRay(sensorPos, rayDir * (isHit ? hit.distance : range), rayColor);
            
            // 3. Logica di Evitamento (Weighted Sum)
            if (isHit)
            {
                hitCount++;

                // Calcolo peso basato sulla vicinanza (più vicino = reazione più forte)
                //float riskFactor = 1.0f - (hit.distance / range); // 0 (lontano) -> 1 (vicino)

                float riskFactor = Mathf.Pow(1.0f - (hit.distance / range), 2) * 3.0f;
                
                // Se l'ostacolo è a DESTRA (angolo > 0), devo sterzare a SINISTRA (valore negativo) e viceversa.
                // Normalizziamo l'angolo tra -1 e 1
                float rayFactor = currentAngle / (fieldOfView / 2); 
                AvoidanceSteer -= rayFactor * riskFactor * avoidanceForce;
                
                // Accumuliamo la forza repulsiva:
                // Se rayFactor è positivo (destra), sottraiamo (andiamo a sinistra).
                AvoidanceSteer -= rayFactor * riskFactor * avoidanceForce;

                // Controllo Sicurezza: Se ho un ostacolo FRONTALE molto vicino, inchioda.
                if (Mathf.Abs(currentAngle) < 15f && hit.distance < 3f)
                {
                    SpeedFactor = 0f;
                    emergencyStop = true;
                }
            }
        }

        // Se non siamo in emergenza, rallentiamo proporzionalmente al numero di ostacoli visti
        if (!emergencyStop && hitCount > 0)
        {
            SpeedFactor = 0.5f; // Rallenta per manovrare meglio
        }
    }
}