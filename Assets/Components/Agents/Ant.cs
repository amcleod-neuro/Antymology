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
    public float distanceToQueen = 100f; // Start with a default max distance

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

            // Set up Rigidbody for movement if it exists, and set interpolation and collision detection for smoother movement and better physics interactions
            rb = GetComponent<Rigidbody>();
                antCollider = GetComponent<Collider>();
            if (rb != null)
            {
               rb.interpolation = RigidbodyInterpolation.Interpolate;
               rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
            distanceToQueen = 100f; // If for some reason the queen doesn't exist, return a default max distance
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
        // Update gridFacing based on new rotation
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
        // Update gridFacing based on new rotation
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

    // Function to check if a block is mulch and no other ants are currently also trying to eat it
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

    // Function to convert the ant's world position to block grid coordinates for the block directly below the ant.
    public Vector3Int GetBlockBelowCoords()
    {
        Vector3 antPos = transform.position;
        
        // FloorToInt converts world position to grid coordinates
        // Example: ant at x=5.7 is in block x=5 (blocks span from integer to integer+1)
        int x = Mathf.FloorToInt(antPos.x);
        int z = Mathf.FloorToInt(antPos.z);
        
        // For Y, we use the bottom of the collider (where the ant's feet are)
        // antCollider.bounds.min.y gives us the lowest point of the collider
        // If no collider exists, estimate feet position as 0.5 units below center
        float baseY = antCollider != null ? antCollider.bounds.min.y : antPos.y - 0.5f;
        
        // Subtract 0.1f safety margin before flooring to ensure we're detecting the block we're standing ON
        // Without this, an ant exactly at y=5.0 might detect the block above instead of below
        int y = Mathf.FloorToInt(baseY - 0.1f);
        
        return new Vector3Int(x, y, z);
    }

    // Function to get the coordinates of the block in front of the ant at foot/ground level (1 block above current floor).
    // Used to check if the ant can step forward onto a block at the same height.
    public Vector3Int GetBlockInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords(); // Start from current position
        Vector3Int step = GetForwardStep(); // Get direction ant is facing (e.g., (1,0,0) for east)
        // Move forward in facing direction and up 1 block (to check foot-level of destination)
        return new Vector3Int(below.x + step.x, below.y + 1, below.z + step.z);
    }

    // Function to get the coordinates of the block one forward and two blocks above the current floor.
    public Vector3Int GetBlockAboveAndInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        // Move forward and up 2 blocks to check head clearance
        return new Vector3Int(below.x + step.x, below.y + 2, below.z + step.z);
    }

    // Function to get the coordinates of the block one forward and three blocks above the current floor.
    public Vector3Int GetBlockTwoAboveAndInFrontCoords()
    {
        Vector3Int below = GetBlockBelowCoords();
        Vector3Int step = GetForwardStep();
        // Move forward and up 3 blocks to check clearance above a step-up
        return new Vector3Int(below.x + step.x, below.y + 3, below.z + step.z);
    }

    // Function to get the grid direction the ant is currently facing.
    // Example: (1,0,0) = East, (-1,0,0) = West, (0,0,1) = North, (0,0,-1) = South
    private Vector3Int GetForwardStep()
    {
        return gridFacing;
    }

    // Function to convert the ant's 3D rotation into a discrete grid direction.
    // This ensures movement is always aligned to the block grid (North/South/East/West only).
    // Called after any rotation to keep gridFacing in sync with visual rotation.
    private void SyncGridFacingFromTransform()
    {
        // Transform model's local forward direction to world space
        // modelForwardLocal is the direction the 3D model considers "forward" in its own coordinate system
        Vector3 forward = transform.TransformDirection(modelForwardLocal).normalized;
        
        // Determine if ant is facing more along X axis or Z axis
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            // Facing is primarily along X axis (East or West)
            // Get sign of X component: positive = East (+1), negative = West (-1)
            int sx = Mathf.RoundToInt(Mathf.Sign(forward.x));
            // Safety check: if somehow we got 0, default to East (1)
            gridFacing = new Vector3Int(sx == 0 ? 1 : sx, 0, 0);
            return;
        }

        // Facing is primarily along Z axis (North or South)
        // Get sign of Z component: positive = North (+1), negative = South (-1)
        int sz = Mathf.RoundToInt(Mathf.Sign(forward.z));
        // Safety check: if somehow we got 0, default to North (1)
        gridFacing = new Vector3Int(0, 0, sz == 0 ? 1 : sz);
    }

    // Function to get the vertical distance from the ant's center to its feet (half of total height).
    // Used when positioning the ant to ensure feet are at the correct height on top of blocks.
    private float GetStandHeight()
    {
        // bounds.extents.y is half the height of the collider
        return antCollider.bounds.extents.y;
    }

    // Function to check if the ant is currently standing on solid ground using a downward raycast.
    // Returns true if ground is detected within a small distance below the ant.
    // Helps detect if the ant is falling or floating due to physics issues.
    public bool IsGrounded()
    {
        // shoot ray from center of collider bounds to detect ground below
        // Ray length is ant's half-height plus small buffer (0.05f)
        float rayLength = antCollider.bounds.extents.y + 0.05f;
        return Physics.Raycast(antCollider.bounds.center, Vector3.down, rayLength);
    }


    #endregion
}