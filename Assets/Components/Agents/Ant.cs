using UnityEngine;
using System.Collections.Generic;

public class Ant : MonoBehaviour
{
    // Public variables to be modified and private variables to track health
    public float maxHealth = 100;
    private float currentHealth;
    public float healthLossPerSecond = 1;
    public float healthGainFromMulch = 20;

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

        if (Mathf.RoundToInt(currentHealth) % 10 == 0) // Log health every 10% health lost
        {
            Debug.Log("Ant health decreased to " + currentHealth);
        }
    }

    void ChooseNextAction()
    {
        // Get the block below the ant
        AbstractBlock blockBelowInstance = GetBlockBelow();
        
        // Find all ants at the same Y coordinate as the ant (same air block layer)
        antsInBlock = new List<Ant>();
        Ant[] allAnts = FindObjectsOfType<Ant>();
        int antBlockY = Mathf.FloorToInt(transform.position.y);
        
        foreach (Ant ant in allAnts)
        {
            int otherBlockY = Mathf.FloorToInt(ant.transform.position.y);
            if (otherBlockY == antBlockY)
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
        // Get the block below the ant to see if there is mulch to eat
        AbstractBlock block = GetBlockBelow();
        
        if (CanEatBlock(block))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(worldX, worldY, worldZ, new Antymology.Terrain.AirBlock());
            // Increase health but do not exceed max health
            currentHealth = Mathf.Min(currentHealth + healthGainFromMulch, maxHealth);
            Debug.Log("Ant ate mulch. Health increased to " + currentHealth);
        }
    }
    #endregion

    #region Logic Checks

    // Checks if a block is mulch and no other ants are currently also trying to eat it
    bool CanEatBlock(AbstractBlock block)
    {
        if (!(block is Antymology.Terrain.MulchBlock))
            return false;
        
        // If there are other ants here, we can't eat the mulch
        if (antsInBlock.Count > 1)
            return false;
            
        return true;
    }

    bool CanMoveForward()
    {
        return true; // Placeholder for movement logic
    }

    #endregion

    #region Utility

    // Function to get the block in front of the ant based on its current rotation
    AbstractBlock GetBlockInFront()
    {
        Vector3 antPos = transform.position;
        
        // Get the ant's forward-facing direction based on its rotation
        Vector3 forwardDir = transform.forward;
        Vector3 blockInFrontPos = antPos + (forwardDir * 5f); // Multiplies forward direction by 5 to ensure we get the block in front of the ant, not the block it's currently in
        int x = Mathf.FloorToInt(blockInFrontPos.x);
        int y = Mathf.FloorToInt(blockInFrontPos.y);
        int z = Mathf.FloorToInt(blockInFrontPos.z);

        return Antymology.Terrain.WorldManager.Instance.GetBlock(x, y, z);
    }

    // Function to get the block directly below the ant
    AbstractBlock GetBlockBelow()
    {
        Vector3 antPos = transform.position;
        int x = Mathf.FloorToInt(antPos.x);
        int y = Mathf.FloorToInt(antPos.y) - 1;
        int z = Mathf.FloorToInt(antPos.z);

        return Antymology.Terrain.WorldManager.Instance.GetBlock(x, y, z);
    }

    #endregion
}