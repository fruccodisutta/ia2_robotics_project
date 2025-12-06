using UnityEngine;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

// Classe dati pura per passare info ricche al Controller
[System.Serializable]
public class WaypointData
{
    public Vector3 position;
    public string type;       // es. "Incrocio", "Rettilineo", "Stop"
    public float speedLimit;  // es. 50, 30
    public string description; // Per Explainability: "Una strada a scorrimento veloce"
}

public class Neo4jManager : MonoBehaviour
{
    // Singleton per accesso facile
    public static Neo4jManager Instance;

    [Header("Connection Settings")]
    [SerializeField] private string uri = "bolt://localhost:7687";
    [SerializeField] private string username = "neo4j";
    [SerializeField] private string password = "turgidissimo"; // <--- METTI LA TUA PASSWORD

    private IDriver _driver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Inizializza il driver ma NON connetterti subito (lo facciamo a richiesta)
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
    }

    // Funzione ASINCRONA per ottenere il percorso
    public async Task<List<WaypointData>> GetPathFromTo(string startNodeName, string endNodeName)
    {
        var pathList = new List<WaypointData>();

        // Query Cypher Intelligente
        // 1. Trova i nodi di inizio e fine per nome
        // 2. Trova il percorso più breve usando la relazione :CONNECTED_TO
        // 3. Restituisce le proprietà dei nodi lungo il percorso
        string query = @"
            MATCH (start:Waypoint {name: $startName}), (end:Waypoint {name: $endName})
            MATCH p = shortestPath((start)-[:CONNECTED_TO*]->(end))
            UNWIND nodes(p) AS node
            RETURN 
                node.x AS x, 
                node.z AS z, 
                node.type AS type, 
                node.speed_limit AS limit,
                node.desc AS desc
        ";

        var session = _driver.AsyncSession();
        try
        {
            // Eseguiamo la query passando i parametri
            var cursor = await session.RunAsync(query, new { startName = startNodeName, endName = endNodeName });

            // Leggiamo i risultati riga per riga
            await cursor.ForEachAsync(record =>
            {
                // Creiamo l'oggetto C# dai dati del DB
                var wp = new WaypointData();
                
                // Conversione coordinate (Attenzione ai float!)
                float x = Convert.ToSingle(record["x"]);
                float z = Convert.ToSingle(record["z"]);
                
                // Unity usa (X, 0, Z) per il piano orizzontale
                wp.position = new Vector3(x, 0, z);
                
                // Altri dati semantici
                wp.type = record["type"].As<string>();
                wp.speedLimit = Convert.ToSingle(record["limit"]);
                
                // Gestione opzionale della descrizione
                if(record.ContainsKey("desc"))
                    wp.description = record["desc"].As<string>();
                else
                    wp.description = "un tratto di strada normale";

                pathList.Add(wp);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[Neo4j Error]: {e.Message}");
        }
        finally
        {
            await session.CloseAsync();
        }

        return pathList;
    }

    void OnDestroy()
    {
        _driver?.Dispose();
    }
}