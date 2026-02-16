using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// QueenAgent class represents the ML-Agents Agent for the queen ant. It handles all agent-based behavior (observations, rewards, actions).
/// It holds a reference to a QueenAnt component which contains the actual queen behavior and state.
/// </summary>
public class QueenAgent : Agent
{
    // Reference to the QueenAnt component on this GameObject
    private QueenAnt queenAnt;

    // Track state for decision making
    private int nestBlocksPlacedThisEpisode = 0;
    private float previousHealth;

    // Variables used in heuristic mode for testing random actions
    private float timeSinceLastAction = 0f;
    private const float ACTION_INTERVAL = 2f; // one action per 2 seconds
    private float timeUntilFirstAction = 3f; // Wait 3 seconds for queen to settle
    
    // Debug flag to use random actions instead of model predictions (useful for testing before training)
    [SerializeField] private bool useRandomActions = false;

    #region ML-Agents Core Methods

    protected override void OnEnable()
    {
        queenAnt = GetComponent<QueenAnt>();
        if (queenAnt != null)
        {
            nestBlocksPlacedThisEpisode = 0;
            previousHealth = queenAnt.maxHealth;
        }
    }

    protected override void OnDisable()
    {
        // Skip base cleanup - we're not using the Academy properly anyway
        // base.OnDisable();
    }

    // Reset variables at the start of each episode
    public override void OnEpisodeBegin()
    {
        nestBlocksPlacedThisEpisode = 0;
        previousHealth = queenAnt.maxHealth;
    }

    private void FixedUpdate()
    {
        // Manually trigger decision requests for exploration
        if (queenAnt == null)
        {
            Debug.LogError("QueenAgent: queenAnt reference is NULL!");
            return;
        }

        if (!queenAnt.IsGrounded())
        {
            return;
        }

        // Wait for queen to settle on ground before taking actions
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
            int action = Random.Range(0, 9);
            Debug.Log($"QueenAgent.OnActionReceived() called with action: {action}");
            
            switch (action)
            {
                case 0: break; // idle
                case 1: queenAnt.MoveAnt(); break;
                case 2: queenAnt.RotateRight(); break;
                case 3: queenAnt.RotateLeft(); break;
                case 4: queenAnt.EatMulch(); break;
                case 5: queenAnt.DigBlock(); break;
                case 6: queenAnt.TurnOnEmergencySignal(); break;
                case 7: queenAnt.TurnOffEmergencySignal(); break;
                case 8: AttemptPlaceNestBlock(); break;
            }
            
            AddReward(-0.002f);
            if (queenAnt.currentHealth > previousHealth)
            {
                AddReward(0.01f);
            }
            previousHealth = queenAnt.currentHealth;
        }
    }

    private bool IsGrounded()
    {
        return queenAnt.IsGrounded();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Health (normalized)
        sensor.AddObservation(queenAnt.currentHealth / queenAnt.maxHealth);

        // 2. Distance to nearest worker ant (normalized - assume max distance is 100 units)
        float distanceToNearestWorker = queenAnt.GetDistanceToNearestWorkerAnt();
        sensor.AddObservation(distanceToNearestWorker / 100f);

        // 3. Block in front type (one-hot encoding for different block types)
        AddBlockTypeObservation(sensor, queenAnt.GetBlockInFront());

        // 4. Can place nest block
        AbstractBlock blockInFront = queenAnt.GetBlockInFront();
        sensor.AddObservation(queenAnt.CanPlaceNestBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate));

        // 5. Number of ants in the same block (normalized)
        float antsInBlockCount = (queenAnt.antsInBlock != null) ? queenAnt.antsInBlock.Count : 0;
        sensor.AddObservation(Mathf.Min(antsInBlockCount / 10f, 1f)); // Normalize to max 10 ants

        // 6. Current nest blocks placed this episode (normalized - assume max 100 per episode)
        sensor.AddObservation(nestBlocksPlacedThisEpisode / 100f);

        // 7. Emergency signal status
        sensor.AddObservation(queenAnt.emergencySignal);

        // 8. Number of worker ants alive (normalized - assume max 100 ants)
        int workerAntsAlive = queenAnt.CountWorkerAntsAlive();
        sensor.AddObservation(workerAntsAlive / 100f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get the discrete action (which action to perform)
        if (!LogicScript.AIisOn)
        {
            useRandomActions = true; // Force random actions when AI is toggled off
        }
        int action = useRandomActions ? Random.Range(0, 9) : actions.DiscreteActions[0];
        // Debug.Log($"QueenAgent.OnActionReceived() called with action: {action}");

        // Execute the corresponding action
        switch (action)
        {
            case 0:
                // Do nothing / idle
                break;
            case 1:
                queenAnt.MoveAnt();
                break;
            case 2:
                queenAnt.RotateRight();
                break;
            case 3:
                queenAnt.RotateLeft();
                break;
            case 4:
                queenAnt.EatMulch();
                break;
            case 5:
                queenAnt.DigBlock();
                break;
            case 6:
                queenAnt.TurnOnEmergencySignal();
                break;
            case 7:
                queenAnt.TurnOffEmergencySignal();
                break;
            case 8:
                AttemptPlaceNestBlock();
                break;
        }

        // Tiny negative reward each step to encourage efficiency
        AddReward(-0.002f);

        // Bonus for staying alive (the queen can be more selfish than worker ants)
        if (queenAnt.currentHealth > previousHealth)
        {
            AddReward(0.01f);
        }
        previousHealth = queenAnt.currentHealth;
    }

    // For testing: use random actions instead of model predictions
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For testing: return random actions
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Random.Range(0, 9);
    }

    #endregion

    #region Helper Methods

    // Attempt to place a nest block and handle the outcome
    private void AttemptPlaceNestBlock()
    {
        AbstractBlock blockInFront = queenAnt.GetBlockInFront();
        if (queenAnt.CanPlaceNestBlock(blockInFront.worldXCoordinate, blockInFront.worldYCoordinate, blockInFront.worldZCoordinate))
        {
            queenAnt.PlaceNestBlock();
            nestBlocksPlacedThisEpisode++;
        }
    }

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

    #region Public Methods for External Reward Control

    // Call this from an episode manager when the queen dies
    public void OnQueenDeath()
    {
        // Reward based on nest blocks placed - the primary objective
        float nestBlockReward = nestBlocksPlacedThisEpisode * 1.0f;
        
        // Bonus for placing many blocks
        if (nestBlocksPlacedThisEpisode >= 10)
            nestBlockReward += 5.0f;
        else if (nestBlocksPlacedThisEpisode >= 5)
            nestBlockReward += 2.0f;

        AddReward(nestBlockReward);

        // End episode
        EndEpisode();
    }

    // Get the total nest blocks placed this episode (useful for external tracking)
    public int GetNestBlocksPlaced()
    {
        return nestBlocksPlacedThisEpisode;
    }

    #endregion

}