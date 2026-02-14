using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AntSelectorUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown antSelector; // Dropdown to select which ant to track
    [SerializeField] TrackingCameraScript trackingCamera; // Reference to the camera script to set the target
    
    // Sorted array of all ants in the scene
    private Ant[] sortedAnts = new Ant[0];

    // Function to update the dropdown with all ants in the scene
    public void UpdateAntSelector()
    {
        // Clear dropdown
        if (antSelector != null)
            antSelector.options.Clear();

        sortedAnts = new Ant[0]; // Clear the sorted ants array

        // Find and sort all ants so Queen is first
        sortedAnts = FindObjectsOfType<Ant>();
        
        if (sortedAnts.Length == 0)
        {
            Debug.LogWarning("AntSelectorUI: No ants found in scene");
            return;
        }

        System.Array.Sort(sortedAnts, (a, b) => {
            bool aIsQueen = a is QueenAnt;
            bool bIsQueen = b is QueenAnt;
            if (aIsQueen && !bIsQueen) return -1;
            if (!aIsQueen && bIsQueen) return 1;
            return 0;
        });

        // Collect names from sorted ants
        List<string> antNames = new List<string>();
        foreach (Ant ant in sortedAnts)
        {
            antNames.Add(ant.gameObject.name);
        }

        // Setup dropdown
        if (antSelector != null)
        {
            antSelector.onValueChanged.AddListener(OnAntSelected);
            antSelector.AddOptions(antNames);
            // Set initial selection to the first ant (should be the queen if present)
            if (antNames.Count > 0)
            {
                antSelector.value = 0;
                OnAntSelected(0); // Immediately set camera to first ant
            }
        }
    }

    void OnAntSelected(int index)
    {
        if (trackingCamera != null && index >= 0 && index < sortedAnts.Length && sortedAnts[index] != null)
        {
            trackingCamera.target = sortedAnts[index].transform;
        }
    }
}
