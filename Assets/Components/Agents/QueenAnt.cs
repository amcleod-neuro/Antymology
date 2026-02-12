using UnityEngine;

public class QueenAnt : Ant
{
    void Awake()
    {
        // Load and apply the queen ant material
        Material queenMaterial = Resources.Load<Material>("queenAntMat");
        if (queenMaterial != null)
        {
            GetComponent<Renderer>().material = queenMaterial;
        }
        else
        {
            Debug.LogError("Queen ant material not found in Resources/queenAntMat");
        }
    }

    #region Actions

    // Function to place a nest block in front of the queen ant and remove her health accordingly
    void PlaceNestBlock()
    {
        // Uses ant function to get the block in front of the queen
        AbstractBlock blockInFront = GetBlockInFront();

        if (CanPlaceNestBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate, new Antymology.Terrain.NestBlock());
            currentHealth -= maxHealth / 3f; // Reduce health by 1/3 of max health
        }
    }

    #endregion

    #region Logic

    // Check that the block in front of the queen is air and she has enough health to place down a nest block
    bool CanPlaceNestBlock(int x, int y, int z)
    {
        // Checks if the block in front is an air block
        AbstractBlock blockInFront = GetBlockInFront();
        if (!(blockInFront is Antymology.Terrain.AirBlock))
            return false;

        // Checks the queen has enough health to place the nest block
        if (currentHealth < 1/3f * maxHealth)
            return false;
        
        return true;
    }

    #endregion
}
