using UnityEngine;
using System.Collections.Generic;

public class SimpleWaypointDriver : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 5.0f;
    public float rotationSpeed = 5.0f;
    public float reachThreshold = 0.5f;

    [Header("Sensors")]
    public LidarAvoidance lidar; // <--- 1. Riferimento al Lidar

    [Header("Path")]
    public List<Transform> waypoints;

    private int currentIndex = 0;

    void Update()
    {
        if (waypoints.Count == 0 || currentIndex >= waypoints.Count) return;

        Transform target = waypoints[currentIndex];

        // --- CALCOLO VETTORE OBIETTIVO (Goal Vector) ---
        // Calcoliamo la direzione pura verso il waypoint normalizzata (lunghezza 1)
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        directionToTarget.y = 0; // Forziamo il piano orizzontale

        // --- FUSIONE SENSORIALE (Lidar + Waypoint) ---
        Vector3 finalDirection = directionToTarget;
        float currentSpeed = speed;

        if (lidar != null)
        {
            // A. Modifica Velocità: Se c'è un ostacolo, rallentiamo o ci fermiamo
            currentSpeed = speed * lidar.SpeedFactor;

            // B. Modifica Direzione: Aggiungiamo una deviazione alla direzione target
            // Se lidar.AvoidanceSteer è > 0 (destra), ruotiamo il vettore target verso destra
            // Usiamo 60 gradi come "peso" massimo dell'evitamento
            float avoidanceAngle = lidar.AvoidanceSteer * 60.0f; 
            
            // Creiamo un quaternione che rappresenta questa deviazione
            Quaternion diversion = Quaternion.Euler(0, avoidanceAngle, 0);
            
            // Applichiamo la deviazione al vettore originale (Rotazione Vettore)
            finalDirection = diversion * directionToTarget;
        }

        // --- APPLICAZIONE FISICA ---

        // 2. Rotazione: L'auto guarda verso la 'finalDirection' (che include l'evitamento)
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 1. Movimento: NON usiamo più MoveTowards sul target, ma avanziamo nella direzione calcolata
        // Questo evita che l'auto "slitti" lateralmente contro l'ostacolo mentre ruota
        transform.position += finalDirection * currentSpeed * Time.deltaTime;


        // --- LOGICA WAYPOINT ---
        // 3. Check Arrivo
        if (Vector3.Distance(transform.position, target.position) < reachThreshold)
        {
            currentIndex++;
            Debug.Log($"Waypoint {currentIndex} raggiunto.");
        }
    }
    
    void OnDrawGizmos()
    {
        // (Codice Gizmos invariato...)
        if (waypoints == null || waypoints.Count < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if(waypoints[i] != null && waypoints[i+1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
        }
    }
}