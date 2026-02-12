using UnityEngine;

public class WorkerAnt : Ant
{

    void Awake()
    {
        // Load and apply the worker ant material
        Material workerMaterial = Resources.Load<Material>("workerAntMat");
        if (workerMaterial != null)
        {
            GetComponent<Renderer>().material = workerMaterial;
        }
        else
        {
            Debug.LogError("Worker ant material not found in Resources/workerAntMat");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // Kill ant if health is dropped to zero
        if (currentHealth <= 0)
        {
            Debug.Log("Ant has died.");
            Destroy(gameObject);
        }
    }
}
