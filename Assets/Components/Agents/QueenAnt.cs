using UnityEngine;

/// <summary>
/// QueenAnt class represents the queen ant in the colony. 
/// It inherits from Ant so it has all the same actions and logic checks, but can place nest blocks and has an emergency signal that worker ants can observe and react to.
/// All actual agent-based behavior (observations, rewards, etc.) is handled in the QueenAgent subclass which holds a reference to this.
/// </summary>
public class QueenAnt : Ant
{

    // Flag to indicate queen is in an emergency state
    public bool emergencySignal = false;

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
    public void PlaceNestBlock()
    {
        // Uses ant function to get the block in front of the queen
        AbstractBlock blockInFront = GetBlockInFront();

        if (CanPlaceNestBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate, new Antymology.Terrain.NestBlock());
            LogicScript.nestBlockCount++; // Increment the nest block count so the UI can update
            currentHealth -= maxHealth / 3f; // Reduce health by 1/3 of max health
        }
    }

    // Function to turn on the emergency signal that other ants can detect
    public void TurnOnEmergencySignal()
    {
        emergencySignal = true;
    }

    // Function to turn off the emergency signal that other ants can detect
    public void TurnOffEmergencySignal()
    {
        emergencySignal = false;
    }

    #endregion

    #region Logic

    // Check that the block in front of the queen is air and she has enough health to place down a nest block
    public bool CanPlaceNestBlock(int x, int y, int z)
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

    // Get distance to the nearest worker ant
    public float GetDistanceToNearestWorkerAnt()
    {
        Ant[] allAnts = FindObjectsOfType<Ant>();
        float nearestDistance = float.MaxValue;

        foreach (Ant ant in allAnts)
        {
            // Skip self and other queen agents
            if (ant == this || ant is QueenAgent)
                continue;

            float distance = Vector3.Distance(transform.position, ant.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        return nearestDistance == float.MaxValue ? 100f : nearestDistance;
    }

    // Count how many worker ants are still alive
    public int CountWorkerAntsAlive()
    {
        Ant[] allAnts = FindObjectsOfType<Ant>();
        int count = 0;

        foreach (Ant ant in allAnts)
        {
            // Skip self and other queen agents
            if (ant == this || ant is QueenAgent)
                continue;

            count++;
        }

        return count;
    }

    #endregion
}
