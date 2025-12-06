using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Stati possibili dell'agente
public enum AgentState
{
    Idle,           // In attesa
    Planning,       // Interroga Neo4j
    Traversing,     // Guida normale
    ObstacleAvoidance, // Manovra locale
    EmergencyStop   // Blocco sicurezza
}

public class TaxiAgent : MonoBehaviour
{
    [Header("Modules")]
    public CarPhysics carPhysics;        // Il corpo
    public LidarSystem lidar;            // La vista
    public Neo4jManager knowledgeBase;   // La memoria

    [Header("State")]
    public AgentState currentState = AgentState.Idle;
    
    [Header("Explainability (XAI)")]
    [TextArea] public string currentThought = "System initialized."; // Qui leggerai cosa pensa l'agente
    
    // Coda di navigazione
    private Queue<WaypointData> pathQueue = new Queue<WaypointData>();
    private WaypointData currentWaypoint;

    void Start()
    {
        // Esempio: Avvio missione
        StartCoroutine(MissionRoutine("Start", "End"));
    }

    void OnDrawGizmos()
    {
        // DEBUG DESTINAZIONE ATTUALE
        if (currentWaypoint != null)
        {
            // Disegna una sfera BLU GIGANTE dove il taxi vuole andare ORA
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentWaypoint.position, 2.0f);
            Gizmos.DrawLine(transform.position, currentWaypoint.position);
        }
    }

    // Coroutine principale per gestire il flusso asincrono (DB richiede tempo)
    IEnumerator MissionRoutine(string startName, string endName)
    {
        // FASE 1: PIANIFICAZIONE
        currentState = AgentState.Planning;
        Think($"Devo andare da {startName} a {endName}. Interrogo Neo4j...");

        // Chiamata asincrona "reale"
        var task = knowledgeBase.GetPathFromTo(startName, endName);
        
        // Attendiamo che il Task finisca senza bloccare Unity
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Think($"Errore critico nel database: {task.Exception.Message}");
            yield break;
        }

        List<WaypointData> path = task.Result;

        if (path != null && path.Count > 0)
        {
            // Riempiamo la coda
            pathQueue = new Queue<WaypointData>(path);
            
            currentState = AgentState.Traversing;
            currentWaypoint = pathQueue.Dequeue(); // Prendi il primo punto
            
            // XAI: Spiegazione iniziale basata sui dati semantici
            Think($"Percorso calcolato: {path.Count} nodi. Il primo tratto è {currentWaypoint.description} con limite {currentWaypoint.speedLimit} km/h.");
        }
        else
        {
            Think("Nessun percorso trovato. Forse i nodi non sono connessi?");
            currentState = AgentState.Idle;
            yield break;
        }

        // FASE 2: ESECUZIONE (Loop di controllo)
       /*  while (pathQueue.Count > 0 || currentWaypoint != null)
        {
            // 1. Percezione
            var sensorData = lidar.Scan();

            // 2. Ragionamento (Decision Making)
            if (sensorData.criticalDanger)
            {
                if (currentState != AgentState.EmergencyStop)
                {
                    currentState = AgentState.EmergencyStop;
                    carPhysics.Stop();
                    Think("PEDONE RILEVATO! Freno di emergenza attivato per garantire la sicurezza.");
                }
            }
            else if (sensorData.obstacleDetected)
            {
                currentState = AgentState.ObstacleAvoidance;
                // Calcola vettore di evitamento
                carPhysics.Drive(currentWaypoint.position, sensorData.avoidanceVector);
                // Nota: Qui non spieghiamo ogni frame per non spammare, solo se cambia stato
            }
            else
            {
                // Guida normale
                if (currentState != AgentState.Traversing) 
                {
                    currentState = AgentState.Traversing;
                    Think("Strada libera. Riprendo la navigazione normale.");
                }
                
                carPhysics.Drive(currentWaypoint.position, Vector3.zero);
            }

            // 3. Check Arrivo Waypoint
            if (Vector3.Distance(transform.position, currentWaypoint.position) < 2.0f)
            {
                if (pathQueue.Count > 0)
                {
                    currentWaypoint = pathQueue.Dequeue();
                    // Explainability contestuale:
                    if(currentWaypoint.type == "Incrocio") 
                        Think("Mi avvicino a un incrocio, rallento e controllo la precedenza.");
                }
                else
                {
                    currentWaypoint = null; // Finito
                }
            }

            yield return new WaitForFixedUpdate();
        } */

        while (pathQueue.Count > 0 || currentWaypoint != null)
        {
            // 1. Percezione
            var sensorData = lidar.Scan();

            // 2. Ragionamento (Decision Making Avanzato)
            
            // Di base, proviamo ad andare alla velocità massima del tratto stradale
            float dynamicSpeed = carPhysics.maxSpeed;
            
            // Se Neo4j ci ha dato un limite (es. 30 km/h), lo rispettiamo
            if(currentWaypoint != null && currentWaypoint.speedLimit > 0)
                dynamicSpeed = Mathf.Min(dynamicSpeed, currentWaypoint.speedLimit);

            // LOGICA OSTACOLI
            if (sensorData.criticalDanger)
            {
                // Situazione: Muro o Pedone vicinissimo (< 3m)
                if (currentState != AgentState.EmergencyStop)
                {
                    currentState = AgentState.EmergencyStop;
                    carPhysics.Stop();
                    Think("PERICOLO IMMINENTE! Freno di emergenza attivato.");
                }
            }
            else if (sensorData.obstacleDetected)
            {
                // Situazione: Vedo qualcosa. Quanto è lontano?
                float dist = sensorData.nearestDistance;

                if (dist < 6.0f)
                {
                    // Vicino: Rallenta molto per manovrare
                    currentState = AgentState.ObstacleAvoidance;
                    dynamicSpeed = 5.0f; // Passo d'uomo
                    // Non spammare il pensiero ogni frame
                    if(Time.frameCount % 60 == 0) Think($"Ostacolo vicino ({dist:F1}m). Rallento a 5 per schivare.");
                }
                else if (dist < 12.0f)
                {
                    // Medio: Rallenta a metà gas
                    currentState = AgentState.ObstacleAvoidance;
                    dynamicSpeed = dynamicSpeed * 0.5f; 
                }
                
                // GUIDA CON EVITAMENTO
                // Passiamo la velocità calcolata al motore fisico
                carPhysics.Drive(currentWaypoint.position, sensorData.avoidanceVector, dynamicSpeed);
            }
            else
            {
                // Situazione: Strada libera
                if (currentState != AgentState.Traversing) 
                {
                    currentState = AgentState.Traversing;
                    Think("Strada libera. Riprendo velocità di crociera.");
                }
                
                // GUIDA NORMALE (Vettore evitamento è zero)
                carPhysics.Drive(currentWaypoint.position, Vector3.zero, dynamicSpeed);
            }

            // 3. Check Arrivo Waypoint (Resta uguale)
            if (Vector3.Distance(transform.position, currentWaypoint.position) < 2.0f)
            {
                if (pathQueue.Count > 0)
                {
                    currentWaypoint = pathQueue.Dequeue();
                    if(currentWaypoint.type == "Incrocio") 
                        Think("Mi avvicino a un incrocio, rallento e controllo la precedenza.");
                }
                else
                {
                    currentWaypoint = null;
                }
            }

            yield return new WaitForFixedUpdate();
        }

        currentState = AgentState.Idle;
        carPhysics.Stop();
        Think("Destinazione raggiunta.");
    }

    // --- XAI MODULE ---
    // In futuro, questa funzione invierà il testo a un'API LLM (OpenAI/Ollama)
    // per riformularlo in modo più naturale o rispondere a domande dell'utente.
    void Think(string rawReasoning)
    {
        currentThought = rawReasoning;
        Debug.Log($"[AGENT BRAIN]: {rawReasoning}");
        
        // TODO: CallLLM_API(rawReasoning);
    }
}