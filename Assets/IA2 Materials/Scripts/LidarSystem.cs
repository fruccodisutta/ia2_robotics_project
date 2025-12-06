using UnityEngine;

// DTO (Data Transfer Object) per i sensori
public struct LidarData
{
    public bool obstacleDetected;
    public bool criticalDanger;    // Ostacolo vicinissimo (frenata)
    public Vector3 avoidanceVector; // Direzione consigliata per schivare
    public string detectedObjectTag; // "Pedone", "Muro", "Cono"

    //
    public float nearestDistance;
}

public class LidarSystem : MonoBehaviour
{
    public float range = 15f;
    public float fov = 90f;
    public int rays = 10;
    public LayerMask obstacleMask;
    [Header("Tuning")]
    public float avoidanceStrength = 5.0f;

/*     public LidarData Scan()
    {
        LidarData data = new LidarData();
        data.avoidanceVector = Vector3.zero;

        Vector3 origin = transform.position + Vector3.up * 0.5f; // Altezza fari
        float angleStep = fov / (rays - 1);
        float startAngle = -fov / 2;

        for (int i = 0; i < rays; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
            Vector3 dir = transform.rotation * rot * Vector3.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, obstacleMask))
            {
                data.obstacleDetected = true;
                data.detectedObjectTag = hit.collider.tag;

                // Calcola vettore di repulsione (opposto all'ostacolo)
                // Pesato sulla vicinanza (più vicino = spinta più forte)
                float weight = 1.0f - (hit.distance / range);
                
                // Se l'ostacolo è a destra (dir positivo), spingo a sinistra
                Vector3 repulsion = -dir * weight * avoidanceStrength;
                data.avoidanceVector += repulsion;

                // Logica di pericolo critico
                if (hit.distance < 3.0f)
                {
                    // Se è un pedone o un muro grosso, è critico
                    if (hit.collider.CompareTag("Pedestrian") || hit.collider.CompareTag("Building"))
                    {
                        data.criticalDanger = true;
                    }
                    // Se è un "Cono", magari non è critico, basta schivare forte
                }
                
                // Debug visivo
                Debug.DrawLine(origin, hit.point, Color.red);
            }
            else
            {
                Debug.DrawRay(origin, dir * range, Color.green);
            }
        }
        
        // Normalizza il vettore di evitamento per sommarlo facilmente alla navigazione
        //if(data.obstacleDetected)
        //    data.avoidanceVector.Normalize();

        return data;
    } */

    public LidarData Scan()
    {
        LidarData data = new LidarData();
        data.avoidanceVector = Vector3.zero;
        data.nearestDistance = range; // Di base è "infinito" (il range massimo)

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float angleStep = fov / (rays - 1);
        float startAngle = -fov / 2;

        for (int i = 0; i < rays; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
            Vector3 dir = transform.rotation * rot * Vector3.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, obstacleMask))
            {
                data.obstacleDetected = true;
                data.detectedObjectTag = hit.collider.tag;

                // 1. SALVIAMO LA DISTANZA MINIMA
                if (hit.distance < data.nearestDistance)
                {
                    data.nearestDistance = hit.distance;
                }

                // 2. Calcolo Vettore (Senza normalizzare!)
                float weight = 1.0f - (hit.distance / range);
                Vector3 repulsion = -dir * weight * avoidanceStrength;
                data.avoidanceVector += repulsion;

                Debug.DrawLine(origin, hit.point, Color.red);
            }
            else
            {
                Debug.DrawRay(origin, dir * range, Color.green);
            }
        }
        
        return data;
    }
}