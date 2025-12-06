using UnityEngine;
using UnityEngine.AI; // FONDAMENTALE per usare la NavMesh

[RequireComponent(typeof(NavMeshAgent))]
public class PedestrianWander : MonoBehaviour
{
    public float wanderRadius = 20f; // Quanto lontano può andare
    public float waitTime = 2f;      // Quanto aspetta prima di ripartire

    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = waitTime;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Se il pedone è arrivato (o quasi) E il timer è scaduto...
        if (timer >= waitTime || (!agent.pathPending && agent.remainingDistance < 0.5f))
        {
            // ...trova una nuova destinazione
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    // Funzione magica che trova un punto casuale SULLA NAVMESH (quindi sul tappeto blu)
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // 1. Genera un punto casuale in una sfera immaginaria
        Vector3 randomDirection = Random.insideUnitSphere * dist;
        randomDirection += origin;

        // 2. Chiede alla NavMesh: "C'è un punto valido vicino a qui?"
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, dist, layermask);

        // 3. Restituisce il punto valido trovato sul tappeto blu
        return navHit.position;
    }
}