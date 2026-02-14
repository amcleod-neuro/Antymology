using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ant class represents a basic ant in the colony with health, the ability to move, eat mulch, dig blocks, and give health to other ants.
/// All actual agent-based behavior (observations, rewards, etc.) is handled in the AntAgent subclass which holds a reference to this.
/// </summary>
public class Ant : MonoBehaviour
{
    // Public variables to be modified and protected variables to track health
    public float maxHealth = 100;

    // Current health of the ant, which has to be initialized so the ML works (if at 0, it thinks all ants are instantly dead and ends the episode)
    public float currentHealth = 100;

    // List to track ants in the same block
    public List<Ant> antsInBlock;

    // Float to track distance to the queen
    public float distanceToQueen = 1000f; // Start with a default max distance

    // Vector to track direction to queen for decision making
    public Vector3 directionToQueen = Vector3.zero;

    

    #region Basics

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         currentHealth = maxHealth;
         Debug.Log(gameObject.name + " health set to " + currentHealth);

         // Triggers health loss to be repeated every second
         InvokeRepeating(nameof(LoseHealth), 1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {

        // Update useful information for the ant to make decisions based on its surroundings and the queen's location
        UpdateAntsInBlock();
        GetDistanceToQueen();
        GetDirectionToQueen();
        // Kill ant if health is dropped to zero
        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " has died.");
            Destroy(gameObject);

            // Update the ant selector UI with the new ants
            AntSelectorUI antSelectorUI = FindObjectOfType<AntSelectorUI>();
            if (antSelectorUI != null)
            {
                antSelectorUI.UpdateAntSelector();
            }
        }
    }

    protected void UpdateAntsInBlock()
    {
        // Check that the block below the ant is the same as the block below the other ants to determine if they are in the same block
        antsInBlock = new List<Ant>();
        Ant[] allAnts = FindObjectsOfType<Ant>();
        AbstractBlock blockBelow = GetBlockBelow();
        
        // Loops through all ants to find which ones are in the same block as the current ant and adds them to the antsInBlock list
        foreach (Ant ant in allAnts)
        {
            AbstractBlock otherAntBlockBelow = ant.GetBlockBelow();
            
            if (blockBelow == otherAntBlockBelow)
            {
                antsInBlock.Add(ant);
            }
        }
    }

    // Function to update the distance to the queen ant, which can be used by other ants to make decisions based on how close they are to the queen
    protected void GetDistanceToQueen()
    {
        QueenAnt queen = FindObjectOfType<QueenAnt>();
        if (queen != null)
        {
            distanceToQueen = Vector3.Distance(transform.position, queen.transform.position);
        }else
        {
            distanceToQueen = 1000f; // If for some reason the queen doesn't exist, return a default max distance
        }
    }

    // Function to update the direction to the queen ant, which can be used by other ants to move towards the queen or make decisions based on the queen's location
    protected void GetDirectionToQueen()
    {
        QueenAnt queen = FindObjectOfType<QueenAnt>();
        if (queen != null)
        {
            directionToQueen = (queen.transform.position - transform.position).normalized;
        }
        else
        {
            directionToQueen = Vector3.zero;
        }
    }

    #endregion

    #region Actions
    // Function to lose health over time, double if on an acidic block
    protected void LoseHealth()
    {
        currentHealth -= ConfigurationManager.Instance.Health_Loss_Per_Second;

        // Check if the block below is acidic and double the health loss
        AbstractBlock blockBelow = GetBlockBelow();
        if (blockBelow is Antymology.Terrain.AcidicBlock)
        {
            currentHealth -= ConfigurationManager.Instance.Health_Loss_Per_Second;         
        }

        if (Mathf.RoundToInt(currentHealth) % 10 == 0) // Log health every 10% health lost
        {
            Debug.Log(gameObject.name + " health decreased to " + currentHealth);
        }

    }

    // Function to move the ant forward if possible
    public void MoveAnt()
    {
        // Checks that the ant can move forward (checks blocks in front and above)
        if (CanMoveForward())
        {
            AbstractBlock blockInFront = GetBlockInFront();
            // Use right vector for horizontal movement (transform.forward points down on the ant model)
            Vector3 moveDir = transform.right * 8;
            
            // Checks that the block in front is an air block and moves the ant forward if it is
            if (blockInFront is Antymology.Terrain.AirBlock)
            {
                transform.position += moveDir;
            }
            else
            {
                AbstractBlock blockAboveAndInFront = GetBlockAboveandInFront();
                // If the block in front isn't an air block, checks if the block above and in front is an air block and moves the ant up and forward if it is
                if (blockAboveAndInFront is Antymology.Terrain.AirBlock)
                {
                    transform.position += moveDir + new Vector3(0, 8, 0);
                }
                else
                {
                    AbstractBlock blockTwoAboveAndInFront = GetBlockTwoAboveAndInFront();
                    // If the block above and in front isn't an air block, checks if the block two blocks above and in front is an air block and moves the ant up two blocks and forward if it is
                    if (blockTwoAboveAndInFront is Antymology.Terrain.AirBlock)
                    {
                        transform.position += moveDir + new Vector3(0, 16, 0);
                    }
                }

            }
        }
    }

    // Function to rotate the ant 90 degrees to the right
    public void RotateRight()
    {
        transform.Rotate(0, 90, 0);
    }

    // Function to rotate the ant 90 degrees to the left
    public void RotateLeft()
    {
        transform.Rotate(0, -90, 0);
    }

    // Function to eat mulch and gain health
    public void EatMulch()
    {
        // Get the block below the ant to see if there is mulch to eat
        AbstractBlock block = GetBlockBelow();
        
        if (CanEatBlock(block))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(block.worldXCoordinate, block.worldYCoordinate, block.worldZCoordinate, new Antymology.Terrain.AirBlock());
            transform.position += new Vector3(0, -5, 0); // Move the ant down into the space where the block was but not too far to avoid clipping issues
            // Increase health but do not exceed max health
            currentHealth = Mathf.Min(currentHealth + ConfigurationManager.Instance.Health_Gain_From_Mulch, maxHealth);
            Debug.Log(gameObject.name + " ate mulch. Health increased to " + currentHealth);
        }
    }

    // Function to dig up the block below an ant and move down into the space where the block was
    public void DigBlock()
    {
        // Get the block below the ant to see if there is a block to dig
        AbstractBlock block = GetBlockBelow();
        
        // Checks that the block isn't a container block, which ants aren't able to dig through
        if (!(block is Antymology.Terrain.ContainerBlock))
        {
            Antymology.Terrain.WorldManager.Instance.SetBlock(block.worldXCoordinate, block.worldYCoordinate, block.worldZCoordinate, new Antymology.Terrain.AirBlock());
            transform.position += new Vector3(0, -5, 0); // Move the ant down into the space where the block was but not too far to avoid clipping issues

            if (block is Antymology.Terrain.NestBlock)
            {
                LogicScript.nestBlockCount--; // Decrement the nest block count so the UI can update (not optimal behaviour but possible)
            }
        }
    }

    // Function to transfer health to another ant in the same block
    public void GiveHealth(Ant otherAnt, float healthToGive)
    {
        // Checks that the current ant has more health than the amount it is trying to give and that the other ant is in the same block
        if (currentHealth > healthToGive && antsInBlock.Contains(otherAnt))
        {
            currentHealth -= healthToGive;
            otherAnt.ReceiveHealth(healthToGive);
            Debug.Log(gameObject.name + " gave " + healthToGive + " health to another ant. New health: " + currentHealth);
        }
    }

    // Function to give health to the lowest health ant in the same block
    public void GiveHealthToLowestHealthInBlock()
    {
        // Stops looking if antsInBlock hasn't been initialized yet or if there are no other ants in block
        if (antsInBlock == null || antsInBlock.Count <= 1)
            return;

        Ant lowestHealthAnt = null;
        float lowestHealth = float.MaxValue;

        foreach (Ant ant in antsInBlock)
        {
            // Ignore this ant (shouldn't give health to self)
            if (ant == this)
                continue;

            // Checks each ant in the block to find the one with the lowest health
            if (ant.currentHealth < lowestHealth)
            {
                lowestHealth = ant.currentHealth;
                lowestHealthAnt = ant;
            }
        }

        if (lowestHealthAnt != null)
        {
            // Give 10 health to the lowest health ant (arbitrary amount for now, maybe changed later or made variable)
            GiveHealth(lowestHealthAnt, 10f);
        }
    }

    // Function to receive health from another ant
    protected void ReceiveHealth(float healthReceived)
    {
        currentHealth = Mathf.Min(currentHealth + healthReceived, maxHealth);
        Debug.Log(gameObject.name + " received " + healthReceived + " health. New health: " + currentHealth);
    }


    #endregion

    #region Logic Checks

    // Checks if a block is mulch and no other ants are currently also trying to eat it
    protected bool CanEatBlock(AbstractBlock block)
    {
        if (!(block is Antymology.Terrain.MulchBlock))
            return false;
        
        // If there are other ants here, we can't eat the mulch
        if (antsInBlock.Count > 1)
            return false;
            
        return true;
    }

    // Checks whether the ant can move forward based on the blocks in front of it and above
    public bool CanMoveForward()
    {
        AbstractBlock blockInFront = GetBlockInFront();
        AbstractBlock blockAboveAndInFront = GetBlockAboveandInFront();
        AbstractBlock blockTwoAboveAndInFront = GetBlockTwoAboveAndInFront();

        // Checks that there is an airblock in front of the ant somewhere in the range it can move
        if (blockInFront is Antymology.Terrain.AirBlock || blockAboveAndInFront is Antymology.Terrain.AirBlock || blockTwoAboveAndInFront is Antymology.Terrain.AirBlock)
            return true;
        
        return false;
    }

    #endregion

    #region Get Blocks

    // Function to get the block in front of the ant based on its current rotation
    public AbstractBlock GetBlockInFront()
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
    public AbstractBlock GetBlockBelow()
    {
        Vector3 antPos = transform.position;
        int x = Mathf.FloorToInt(antPos.x);
        int y = Mathf.FloorToInt(antPos.y) - 3;
        int z = Mathf.FloorToInt(antPos.z);

        return Antymology.Terrain.WorldManager.Instance.GetBlock(x, y, z);
    }

    // Function to get the block above and in front of the ant based on its current rotation
    protected AbstractBlock GetBlockAboveandInFront()
    {
        Vector3 antPos = transform.position;
        
        // Get the ant's forward-facing direction based on its rotation
        Vector3 forwardDir = transform.forward;

        // Multiplies forward direction by 5 to ensure we get the block in front of the ant and adds 8 to the y coordinate to get the block above
        Vector3 blockAboveAndInFrontPos = antPos + (forwardDir * 5f) + new Vector3(0, 8, 0);
        int x = Mathf.FloorToInt(blockAboveAndInFrontPos.x);
        int y = Mathf.FloorToInt(blockAboveAndInFrontPos.y);
        int z = Mathf.FloorToInt(blockAboveAndInFrontPos.z);

        return Antymology.Terrain.WorldManager.Instance.GetBlock(x, y, z);
    }

    // Function to get the block one block forward and two blocks above the ant based on its current rotation
    protected AbstractBlock GetBlockTwoAboveAndInFront()
    {
        Vector3 antPos = transform.position;
        
        // Get the ant's forward-facing direction based on its rotation
        Vector3 forwardDir = transform.forward;

        // Multiplies forward direction by 5 to ensure we get the block in front of the ant and adds 16 to the y coordinate to get the block two blocks above
        Vector3 blockTwoAboveAndInFrontPos = antPos + (forwardDir * 5f) + new Vector3(0, 16, 0);
        int x = Mathf.FloorToInt(blockTwoAboveAndInFrontPos.x);
        int y = Mathf.FloorToInt(blockTwoAboveAndInFrontPos.y);
        int z = Mathf.FloorToInt(blockTwoAboveAndInFrontPos.z);

        return Antymology.Terrain.WorldManager.Instance.GetBlock(x, y, z);
    }


    #endregion
}