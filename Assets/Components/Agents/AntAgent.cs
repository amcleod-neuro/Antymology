using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

/// <summary>
/// AntAgent class represents the ML-Agents Agent for a worker ant. It handles all agent-based behavior (observations, rewards, actions).
/// It holds a reference to an Ant component which contains the actual ant behavior and state.
/// </summary>
public class AntAgent : Agent
{
    // Reference to the Ant component on this GameObject
    private Ant ant;
    
    // Maximum number of ants observed in the same block (to keep reasonable size of observation vector)
    private const int MAX_ANTS_TO_OBSERVE = 10;
    
    // Debug flag to use random actions instead of model predictions (useful for testing before training)
    [SerializeField] private bool useRandomActions = false;
    
    // Track previous health to calculate survival bonus
    private float previousHealth;

    #region ML-Agents Methods

    protected override void OnEnable()
    {
        ant = GetComponent<Ant>();
        if (ant != null)
        {
            previousHealth = ant.maxHealth;
        }
    }

    protected override void OnDisable()
    {
        // Skip base cleanup - we're not using the Academy properly anyway
        // base.OnDisable();
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("AntAgent.OnEpisodeBegin() called!");
        previousHealth = ant.maxHealth;
    }

    private float timeSinceLastAction = 0f;
    private const float ACTION_INTERVAL = 2f; // one action per 2 seconds
    private float timeUntilFirstAction = 3f; // Wait 3 seconds for ant to settle

    private void FixedUpdate()
    {
        // Manually trigger decision requests for exploration
        if (ant == null)
        {
            Debug.LogError("AntAgent: ant reference is NULL!");
            return;
        }

        if (!ant.IsGrounded())
        {
            return;
        }

        // Wait for ant to settle on ground before taking actions
        if (timeUntilFirstAction > 0)
        {
            timeUntilFirstAction -= Time.fixedDeltaTime;
            return;
        }

        timeSinceLastAction += Time.fixedDeltaTime;
        if (timeSinceLastAction >= ACTION_INTERVAL)
        {
            timeSinceLastAction = 0f;
            
            // Manually execute a random action
            int action = Random.Range(0, 7);
            Debug.Log($"AntAgent.OnActionReceived() called with action: {action}");
            
            switch (action)
            {
                case 0: break; // idle
                case 1: ant.MoveAnt(); break;
                case 2: ant.RotateRight(); break;
                case 3: ant.RotateLeft(); break;
                case 4: ant.EatMulch(); break;
                case 5: ant.DigBlock(); break;
                case 6: ant.GiveHealthToLowestHealthInBlock(); break;
            }
            
            AddReward(-0.001f);
            if (ant.currentHealth > previousHealth)
            {
                AddReward(0.005f);
            }
            previousHealth = ant.currentHealth;
        }
    }

    private bool IsGrounded()
    {
        return ant.IsGrounded();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("AntAgent.CollectObservations() called!");
        // 1. Health (normalized)
        sensor.AddObservation(ant.currentHealth / ant.maxHealth);

        // 2. Distance to queen (normalized - assume max distance is 1000 units)
        sensor.AddObservation(ant.distanceToQueen / 1000f);

        // 3. Block under type (one-hot encoding for different block types)
        AddBlockTypeObservation(sensor, ant.GetBlockBelow());

        // 4. Can move forward (boolean)
        sensor.AddObservation(ant.CanMoveForward());

        // 5. Queen emergency signal
        QueenAnt queen = FindObjectOfType<QueenAnt>();
        // Check if queen exists before trying to access emergency signal (extra safety even though queen should always exist)
        if (queen != null)
        {
            sensor.AddObservation(queen.emergencySignal);
        }
        else
        {
            sensor.AddObservation(false);
        }

        // 6. Direction to queen (normalized 3D vector)
        sensor.AddObservation(ant.directionToQueen.x);
        sensor.AddObservation(ant.directionToQueen.y);
        sensor.AddObservation(ant.directionToQueen.z);

        // 7. Ants in block - 2D array with ant presence and health
        // For each potential ant slot, we observe: [exists (1/0), normalized health]
        int antsObserved = 0;
        if (ant.antsInBlock != null)
        {
            foreach (Ant antInBlock in ant.antsInBlock)
            {
                if (antsObserved >= MAX_ANTS_TO_OBSERVE)
                    break;
                
                // Skip self
                if (antInBlock == ant)
                    continue;

                sensor.AddObservation(1f); // Ant exists
                sensor.AddObservation(antInBlock.currentHealth / antInBlock.maxHealth); // Normalized health
                antsObserved++;
            }
        }

        // Pad remaining ant slots with zeros
        for (int i = antsObserved; i < MAX_ANTS_TO_OBSERVE; i++)
        {
            sensor.AddObservation(0f); // No ant
            sensor.AddObservation(0f); // No health
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!LogicScript.AIisOn)
        {
            useRandomActions = true; // Force random actions when AI is toggled off
        }
        // Get the discrete action (which action to perform)
        int action = useRandomActions ? Random.Range(0, 7) : actions.DiscreteActions[0];

        /// Debug.Log($"AntAgent.OnActionReceived() called with action: {action}");

        // Execute the corresponding action
        switch (action)
        {
            case 0:
                // Do nothing / idle
                break;
            case 1:
                ant.MoveAnt();
                break;
            case 2:
                ant.RotateRight();
                break;
            case 3:
                ant.RotateLeft();
                break;
            case 4:
                ant.EatMulch();
                break;
            case 5:
                ant.DigBlock();
                break;
            case 6:
                // Give health to lowest health ant in block
                ant.GiveHealthToLowestHealthInBlock();
                break;
        }

        // Small negative reward each step to encourage efficiency and not wasting time
        AddReward(-0.001f);
        
        // Bonus for staying alive (encourages self-preservation, but reward is less than queen's)
        if (ant.currentHealth > previousHealth)
        {
            AddReward(0.005f);
        }
        previousHealth = ant.currentHealth;
    }

    // For testing: use random actions instead of model predictions
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For testing: return random actions
        Debug.Log("AntAgent.Heuristic() called");
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Random.Range(0, 7);
    }

    #endregion

    #region Helper Methods

    // Add block type as one-hot encoded observation
    private void AddBlockTypeObservation(VectorSensor sensor, AbstractBlock block)
    {
        // One-hot encoding for block types
        sensor.AddObservation(block is Antymology.Terrain.AirBlock);
        sensor.AddObservation(block is Antymology.Terrain.MulchBlock);
        sensor.AddObservation(block is Antymology.Terrain.NestBlock);
        sensor.AddObservation(block is Antymology.Terrain.AcidicBlock);
        sensor.AddObservation(block is Antymology.Terrain.ContainerBlock);
        // Add a catch-all for any other block type
        sensor.AddObservation(!(block is Antymology.Terrain.AirBlock || 
                                block is Antymology.Terrain.MulchBlock || 
                                block is Antymology.Terrain.NestBlock || 
                                block is Antymology.Terrain.AcidicBlock || 
                                block is Antymology.Terrain.ContainerBlock));
    }

    #endregion
}
