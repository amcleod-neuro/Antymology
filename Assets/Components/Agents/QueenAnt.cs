using UnityEngine;

public class QueenAnt : Ant
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Actions

    // Function to place a nest block in front of the queen ant and remove her health accordingly
    void PlaceNestBlock()
    {
        // Uses ant function to get the coordinates of the block in front of the queen
        Vector3Int blockInFront = GetBlockInFront();

        if (CanPlaceNestBlock(blockInFront.x, blockInFront.y, blockInFront.z))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(blockInFront.x, blockInFront.y, blockInFront.z, new Antymology.Terrain.NestBlock());
            currentHealth -= maxHealth / 3f; // Reduce health by 1/3 of max health
        }
    }

    #endregion

    #region Logic

    // Check that the block in front of the queen is air and she has enough health to place down a nest block
    bool CanPlaceNestBlock(int worldX, int worldY, int worldZ)
    {
        // Checks if the block in front is an air block
        AbstractBlock block = Antymology.Terrain.WorldManager.Instance.GetBlock(worldX, worldY, worldZ);
        if (!(block is Antymology.Terrain.AirBlock))
            return false;

        // Checks the queen has enough health to place the nest block
        if (currentHealth < 1/3f * maxHealth)
            return false;
        
        return true;
    }

    #endregion
}
