using UnityEngine;

public class Ant : MonoBehaviour
{
    // Public variables to be modified and private variables to track health
    public float maxHealth = 100;
    private float currentHealth;
    public float healthLossPerSecond = 1;
    public float healthGainFromMulch = 20;

    // Block coordinates below the ant
    private Vector3Int blockBelow;

    // List to track ants in the same block
    private List<Ant> antsInBlock;

    #region Basics
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         currentHealth = maxHealth;
         Debug.Log("Ant health set to " + currentHealth);

         // Triggers health loss to be repeated every second
         InvokeRepeating(nameof(LoseHealth), 1f, 1f);
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

    #endregion

    #region Actions
    // Function to lose health over time
    void LoseHealth()
    {
        currentHealth -= healthLossPerSecond;
        Debug.Log("Ant health decreased to " + currentHealth);
    }

    void ChooseNextAction()
    {
        // Store the coordinates of the block directly below the ant
        Vector3 antPos = transform.position;
        blockBelow = new Vector3Int(
            Mathf.RoundToInt(antPos.x + 0.5f),
            Mathf.RoundToInt(antPos.y) - 1,
            Mathf.RoundToInt(antPos.z + 0.5f)
        );

        // Find all ants in the same air block
        antsInBlock = new List<Ant>();
        Ant[] allAnts = FindObjectsOfType<Ant>();
        foreach (Ant ant in allAnts)
        {
            Vector3 otherAntPos = ant.transform.position;
            int otherBlockX = Mathf.RoundToInt(otherAntPos.x + 0.5f);
            int otherBlockZ = Mathf.RoundToInt(otherAntPos.z + 0.5f);
            int otherBlockY = Mathf.RoundToInt(otherAntPos.y);

            if (otherBlockX == blockBelow.x && otherBlockZ == blockBelow.z && otherBlockY == blockBelow.y + 1)
            {
                antsInBlock.Add(ant);
            }
        }


    }

    void MoveAnt()
    {
        // Placeholder for movement
    }

    // Function to eat mulch and gain health
    void EatMulch(int worldX, int worldY, int worldZ)
    {
        // Check if block is mulch and remove it
        Antymology.Terrain.AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(worldX, worldY, worldZ);
        if (block is Antymology.Terrain.MulchBlock)
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(worldX, worldY, worldZ, new Antymology.Terrain.AirBlock());
        }
        // Increase health but do not exceed max health
        currentHealth = Mathf.Min(currentHealth + healthGainFromMulch, maxHealth);
        Debug.Log("Ant ate mulch. Health increased to " + currentHealth);
    }
    #endregion

    #region Logic Checks

    // Checks if the block below is mulch and no other ants are currently also on it
    bool CanEatMulch(int worldX, int worldY, int worldZ, List<Ant> antsHere)
    {
        Antymology.Terrain.AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(worldX, worldY, worldZ);
        if (!(block is Antymology.Terrain.MulchBlock))
            return false;
        
        // If there are other ants here, we can't eat the mulch
        else if (antsHere.Count > 1)
            return false;
        else
            return true;
    }

    #endregion
}