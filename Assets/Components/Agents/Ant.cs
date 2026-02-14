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

    private Rigidbody rb;
    private Collider antCollider;
    private Vector3Int gridFacing = new Vector3Int(0, 0, 1);
    [SerializeField] private Vector3 modelForwardLocal = Vector3.right;

    

    #region Basics

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         currentHealth = maxHealth;
         Debug.Log(gameObject.name + " health set to " + currentHealth);

            // Set up Rigidbody for movement if it exists, and set interpolation and collision detection for smoother movement and better physics interactions, and freeze rotation to prevent ants from tipping over
            rb = GetComponent<Rigidbody>();
                antCollider = GetComponent<Collider>();
            if (rb != null)
            {
               rb.interpolation = RigidbodyInterpolation.Interpolate;
               rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
               rb.freezeRotation = true;
               rb.useGravity = true; // Need gravity for falling into world and over gaps
            }

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
        
        // Debug log if the ant is really low in y coordinates, which can indicate falling out of the world due to a bug
        if (transform.position.y < -1000f)
        {
            Debug.LogWarning(gameObject.name + " is very low in y coordinate at " + transform.position.y);
        }

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
        SyncGridFacingFromTransform();
        Vector3Int belowCoords = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        int targetX = belowCoords.x + step.x;
        int targetY = belowCoords.y;
        int targetZ = belowCoords.z + step.z;

        Debug.Log(gameObject.name + " MoveAnt from (" + belowCoords.x + "," + belowCoords.y + "," + belowCoords.z + ") to (" + targetX + "," + targetY + "," + targetZ + ") step " + step);

        AbstractBlock blockAtFoot = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY, targetZ);
        AbstractBlock blockAtFootBelow = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY - 1, targetZ);
        AbstractBlock blockAtHead = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY + 1, targetZ);
        AbstractBlock blockAtHead2 = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY + 2, targetZ);

        Debug.Log(gameObject.name + " blocks at target: foot=" + blockAtFoot.GetType().Name + ", footBelow=" + blockAtFootBelow.GetType().Name + ", head=" + blockAtHead.GetType().Name + ", head2=" + blockAtHead2.GetType().Name);

        int standY;
        if (blockAtFoot is Antymology.Terrain.AirBlock)
        {
            // Check if there's a block one level up to step onto
            if (!(blockAtHead is Antymology.Terrain.AirBlock))
            {
                // There's a block to step onto one level up
                if (blockAtHead2 is Antymology.Terrain.AirBlock)
                {
                    standY = targetY + 1; // Step onto the higher block
                    Debug.Log(gameObject.name + " MoveAnt: stepping up onto block at head level");
                }
                else
                {
                    Debug.Log(gameObject.name + " MoveAnt blocked: no headroom for step up");
                    return;
                }
            }
            else
            {
                // Both foot and head are air - check if we can fall down safely
                // Allow stepping into air and let physics handle falling
                standY = belowCoords.y;
                Debug.Log(gameObject.name + " MoveAnt: stepping into air, will fall");
            }
        }
        else
        {
            // blockAtFoot is solid
            if (!(blockAtHead is Antymology.Terrain.AirBlock))
            {
                // Both foot and head blocked - need to step up two levels
                if (blockAtHead2 is Antymology.Terrain.AirBlock)
                {
                    standY = targetY + 1;
                    Debug.Log(gameObject.name + " MoveAnt: stepping up onto solid block");
                }
                else
                {
                    Debug.Log(gameObject.name + " MoveAnt blocked: no headroom");
                    return;
                }
            }
            else
            {
                // foot is solid, head is air - normal forward step
                standY = targetY;
                Debug.Log(gameObject.name + " MoveAnt: normal forward step");
            }
        }

        // Center on integer coordinates with offset and larger y-buffer to prevent falling through
        Vector3 targetPos = new Vector3(Mathf.Round(targetX), standY + 1f + GetStandHeight(), Mathf.Round(targetZ));
        Debug.Log(gameObject.name + " MoveAnt moving to " + targetPos);
        if (rb != null)
        {
            rb.MovePosition(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }
    }

    // Function to rotate the ant 90 degrees to the right
    public void RotateRight()
    {
        Quaternion delta = Quaternion.AngleAxis(90f, transform.forward);
        Quaternion target = delta * transform.rotation;
        if (rb != null)
        {
            rb.MoveRotation(target);
        }
        transform.rotation = target;
        RotateFacingRight();
        SyncGridFacingFromTransform();
    }

    // Function to rotate the ant 90 degrees to the left
    public void RotateLeft()
    {
        Quaternion delta = Quaternion.AngleAxis(-90f, transform.forward);
        Quaternion target = delta * transform.rotation;
        if (rb != null)
        {
            rb.MoveRotation(target);
        }
        transform.rotation = target;
        RotateFacingLeft();
        SyncGridFacingFromTransform();
    }

    // Function to eat mulch and gain health
    public void EatMulch()
    {
        // Get the block below the ant to see if there is mulch to eat
        AbstractBlock block = GetBlockBelow();
        Vector3Int belowCoords = GetBlockBelowCoords();
        
        if (block is Antymology.Terrain.AirBlock)
            return;

        if (CanEatBlock(block))
        {
            // Safety check: make sure there's a block 2 levels below before eating
            AbstractBlock twoBelow = Antymology.Terrain.WorldManager.Instance.GetBlock(belowCoords.x, belowCoords.y - 1, belowCoords.z);
            if (twoBelow is Antymology.Terrain.AirBlock)
            {
                Debug.LogWarning(gameObject.name + " cannot eat - no support below!");
                return;
            }
            
            Antymology.Terrain.WorldManager.Instance.SetBlock(belowCoords.x, belowCoords.y, belowCoords.z, new Antymology.Terrain.AirBlock());
            AbstractBlock updated = Antymology.Terrain.WorldManager.Instance.GetBlock(belowCoords.x, belowCoords.y, belowCoords.z);
            if (updated is Antymology.Terrain.AirBlock)
            {
                // Center the ant on the block and move down with larger y-buffer, let gravity handle the rest
                float targetX = Mathf.Round(belowCoords.x);
                float targetZ = Mathf.Round(belowCoords.z);
                float targetY = transform.position.y - 0.5f; // Move down less, let gravity pull them down
                Vector3 newPos = new Vector3(targetX, targetY, targetZ);
                
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Preserve y velocity for falling
                    rb.MovePosition(newPos);
                }
                else
                {
                    transform.position = newPos;
                }
            }
            else
            {
                Debug.LogWarning(gameObject.name + " tried to eat mulch, but block below is still " + updated.GetType().Name);
            }
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
        Vector3Int belowCoords = GetBlockBelowCoords();
        
        if (block is Antymology.Terrain.AirBlock)
            return;

        // Checks that the block isn't a container block, which ants aren't able to dig through
        if (!(block is Antymology.Terrain.ContainerBlock))
        {
            // Safety check: make sure there's a block 2 levels below before digging
            AbstractBlock twoBelow = Antymology.Terrain.WorldManager.Instance.GetBlock(belowCoords.x, belowCoords.y - 1, belowCoords.z);
            if (twoBelow is Antymology.Terrain.AirBlock)
            {
                Debug.LogWarning(gameObject.name + " cannot dig - no support below!");
                return;
            }
            
            Antymology.Terrain.WorldManager.Instance.SetBlock(belowCoords.x, belowCoords.y, belowCoords.z, new Antymology.Terrain.AirBlock());
            AbstractBlock updated = Antymology.Terrain.WorldManager.Instance.GetBlock(belowCoords.x, belowCoords.y, belowCoords.z);
            if (updated is Antymology.Terrain.AirBlock)
            {
                // Center the ant on the block and move down with larger y-buffer, let gravity handle the rest
                float targetX = Mathf.Round(belowCoords.x);
                float targetZ = Mathf.Round(belowCoords.z);
                float targetY = transform.position.y - 0.5f; // Move down less, let gravity pull them down
                Vector3 newPos = new Vector3(targetX, targetY, targetZ);
                
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Preserve y velocity for falling
                    rb.MovePosition(newPos);
                }
                else
                {
                    transform.position = newPos;
                }
            }
            else
            {
                Debug.LogWarning(gameObject.name + " tried to dig, but block below is still " + updated.GetType().Name);
            }

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
        SyncGridFacingFromTransform();
        Vector3Int belowCoords = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        int targetX = belowCoords.x + step.x;
        int targetY = belowCoords.y;
        int targetZ = belowCoords.z + step.z;

        AbstractBlock blockAtFoot = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY, targetZ);
        AbstractBlock blockAtHead = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY + 1, targetZ);
        AbstractBlock blockAtHead2 = Antymology.Terrain.WorldManager.Instance.GetBlock(targetX, targetY + 2, targetZ);

        // Can move if any of these scenarios are true:
        // 1. Foot is air and head is air (falling/walking into air)
        // 2. Foot is air, head is solid, head2 is air (stepping up onto a block)
        // 3. Foot is solid, head is air (normal forward movement)
        // 4. Foot is solid, head is solid, head2 is air (stepping up two levels)
        
        if (blockAtFoot is Antymology.Terrain.AirBlock)
        {
            // Can move into air, or step up if there's a block at head level with headroom
            if (blockAtHead is Antymology.Terrain.AirBlock)
                return true; // Can fall/walk into air
            if (blockAtHead2 is Antymology.Terrain.AirBlock)
                return true; // Can step up onto block at head level
            return false; // No headroom
        }
        else
        {
            // Foot is solid, need air at head level or ability to step up
            if (blockAtHead is Antymology.Terrain.AirBlock)
                return true; // Normal forward step
            if (blockAtHead2 is Antymology.Terrain.AirBlock)
                return true; // Can step up
            return false; // Blocked
        }
    }

    #endregion

    #region Get Blocks

    // Function to get the block in front of the ant based on its current rotation
    public AbstractBlock GetBlockInFront()
    {
        Vector3Int coords = GetBlockInFrontCoords();
        return Antymology.Terrain.WorldManager.Instance.GetBlock(coords.x, coords.y, coords.z);
    }

    // Function to get the block directly below the ant
    public AbstractBlock GetBlockBelow()
    {
        Vector3Int coords = GetBlockBelowCoords();
        return Antymology.Terrain.WorldManager.Instance.GetBlock(coords.x, coords.y, coords.z);
    }

    // Function to get the block above and in front of the ant based on its current rotation
    protected AbstractBlock GetBlockAboveandInFront()
    {
        Vector3Int coords = GetBlockAboveAndInFrontCoords();
        return Antymology.Terrain.WorldManager.Instance.GetBlock(coords.x, coords.y, coords.z);
    }

    // Function to get the block one block forward and two blocks above the ant based on its current rotation
    protected AbstractBlock GetBlockTwoAboveAndInFront()
    {
        Vector3Int coords = GetBlockTwoAboveAndInFrontCoords();
        return Antymology.Terrain.WorldManager.Instance.GetBlock(coords.x, coords.y, coords.z);
    }

    public Vector3Int GetBlockBelowCoords()
    {
        Vector3 antPos = transform.position;
        int x = Mathf.FloorToInt(antPos.x);
        int z = Mathf.FloorToInt(antPos.z);
        float baseY = antCollider != null ? antCollider.bounds.min.y : antPos.y - 0.5f;
        int y = Mathf.FloorToInt(baseY - 0.1f); // Increased safety margin
        return new Vector3Int(x, y, z);
    }

    public Vector3Int GetBlockInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        return new Vector3Int(below.x + step.x, below.y + 1, below.z + step.z);
    }

    public Vector3Int GetBlockAboveAndInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        return new Vector3Int(below.x + step.x, below.y + 2, below.z + step.z);
    }

    public Vector3Int GetBlockTwoAboveAndInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        return new Vector3Int(below.x + step.x, below.y + 3, below.z + step.z);
    }

    private Vector3Int GetForwardStep()
    {
        return gridFacing;
    }

    private void SyncGridFacingFromTransform()
    {
        Vector3 forward = transform.TransformDirection(modelForwardLocal).normalized;
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            int sx = Mathf.RoundToInt(Mathf.Sign(forward.x));
            gridFacing = new Vector3Int(sx == 0 ? 1 : sx, 0, 0);
            return;
        }

        int sz = Mathf.RoundToInt(Mathf.Sign(forward.z));
        gridFacing = new Vector3Int(0, 0, sz == 0 ? 1 : sz);
    }

    private void RotateFacingRight()
    {
        int x = gridFacing.x;
        int z = gridFacing.z;
        gridFacing = new Vector3Int(z, 0, -x);
    }

    private void RotateFacingLeft()
    {
        int x = gridFacing.x;
        int z = gridFacing.z;
        gridFacing = new Vector3Int(-z, 0, x);
    }

    private float GetStandHeight()
    {
        if (antCollider == null)
            return 1f;

        return antCollider.bounds.extents.y;
    }

    public bool IsGrounded()
    {
        if (antCollider == null)
            return Physics.Raycast(transform.position, Vector3.down, 1.1f);

        float rayLength = antCollider.bounds.extents.y + 0.05f;
        return Physics.Raycast(antCollider.bounds.center, Vector3.down, rayLength);
    }


    #endregion
}