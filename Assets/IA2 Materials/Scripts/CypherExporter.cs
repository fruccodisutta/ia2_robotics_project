using UnityEngine;

[ExecuteInEditMode] // Funziona anche senza premere Play!
public class CypherExporter : MonoBehaviour
{
    [Header("Settings")]
    public string nodeName = "Waypoint";
    public string type = "Intersection";
    public float speedLimit = 50;
    public string desc = "Normale incrocio stradale con strisce pedonali";
    
    [Header("Actions")]
    public bool printQuery = false;

    void Update()
    {
        if (printQuery)
        {
            printQuery = false; // Reset immediato pulsante
            GenerateCypher();
        }
    }

    void GenerateCypher()
    {
        // TRUCCO: transform.position restituisce SEMPRE la posizione GLOBALE (World)
        // indipendentemente dai genitori.
        Vector3 pos = transform.position;

        // Formattiamo la stringa con il punto decimale (InvariantCulture)
        string x = pos.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        string z = pos.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        string query = $"CREATE (w:Waypoint {{name: '{gameObject.name}', x: {x}, z: {z}, type: '{type}', speed_limit: {speedLimit}, desc: '{desc}'}});";

        Debug.Log($"<color=green>CYPHER COPIABILE:</color>\n{query}");
    }
}