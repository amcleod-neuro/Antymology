using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages ML-Agents episode lifecycle and coordinates colony-wide rewards based on nest blocks placed
/// </summary>
public class EpisodeManager : MonoBehaviour
{
    [SerializeField] private float maxEpisodeTime = 500f; // Max seconds per episode

    // Variable to toggle whether worker ants receive rewards when the queen places nest blocks (encourages supporting the queen)
    [SerializeField] private bool rewardAntSurvivalWithQueenBlocks = true;
    
    // Variables necessary for episode
    private QueenAgent queenAgent;
    private List<AntAgent> workerAnts;
    private float episodeStartTime;
    private int nestBlocksPlacedThisEpisode = 0;
    private float lastNestBlockRewardTime = 0f;
    private bool episodeInitialized = false;

    private void OnEnable()
    {
        // Don't search for ants yet - they'll be spawned by WorldManager
        episodeInitialized = false;
    }

    private void Update()
    {
        // Defer episode initialization until ants are spawned by WorldManager
        if (!episodeInitialized)
        {
            if (FindObjectOfType<AntAgent>() != null || FindObjectOfType<QueenAgent>() != null)
            {
                ResetEpisode();
                episodeInitialized = true;
            }
            return;
        }

        // Check if queen is dead and end episode if so
        if (queenAgent == null)
        {
            EndEpisode();
            return;
        }

        // Check for episode timing out at 500 seconds
        if (Time.time - episodeStartTime > maxEpisodeTime)
        {
            Debug.Log("Episode ended: Max time exceeded");
            EndEpisode();
        }
    }

    private void ResetEpisode()
    {
        episodeStartTime = Time.time;
        nestBlocksPlacedThisEpisode = 0;
        lastNestBlockRewardTime = Time.time;

        // Find queen agent
        queenAgent = FindObjectOfType<QueenAgent>();
        if (queenAgent != null)
        {
            queenAgent.OnEpisodeBegin();
        }

        // Find all worker ants
        workerAnts = new List<AntAgent>(FindObjectsOfType<AntAgent>());
        foreach (AntAgent ant in workerAnts)
        {
            ant.OnEpisodeBegin();
        }

        Debug.Log($"Episode started with {workerAnts.Count} worker ants and 1 queen");
    }

    private void EndEpisode()
    {
        Debug.Log($"Episode ended. Nest blocks placed: {nestBlocksPlacedThisEpisode}");
        
        // Queen ends its own episode with nest block reward (handles QueenAgent logic)
        if (queenAgent != null)
        {
            queenAgent.OnQueenDeath();
        }
        
        // End all worker ant episodes with shared colony success reward
        AntAgent[] allAnts = FindObjectsOfType<AntAgent>();
        foreach (AntAgent ant in allAnts)
        {
            // Give final reward based on queen's success
            float queenSuccessReward = nestBlocksPlacedThisEpisode * 0.5f;
            ant.AddReward(queenSuccessReward);
            ant.EndEpisode();
        }

        // Wait for new ants to spawn before reinitializing
        episodeInitialized = false;
    }

    /// <summary>
    /// Called when a nest block is successfully placed.
    /// Distributes rewards to all ants based on the colony's success.
    /// </summary>
    public void OnNestBlockPlaced()
    {
        nestBlocksPlacedThisEpisode++;
        
        if (rewardAntSurvivalWithQueenBlocks)
        {
            // Reward all worker ants for supporting a productive queen
            AntAgent[] allAnts = FindObjectsOfType<AntAgent>();
            foreach (AntAgent ant in allAnts)
            {
                ant.AddReward(0.2f); // Bonus for contributing to colony success
            }
        }

        Debug.Log($"Nest block placed! Total this episode: {nestBlocksPlacedThisEpisode}");
    }

}
