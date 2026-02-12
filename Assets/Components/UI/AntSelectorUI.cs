using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AntSelectorUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown antSelector; // Dropdown to select which ant to track
    [SerializeField] TrackingCameraScript trackingCamera; // Reference to the camera script to set the target
    

    // Function to update the dropdown with all ants in the scene
    public void UpdateAntSelector()
    {
        // Clear dropdown
        if (antSelector != null)
            antSelector.options.Clear();

        // Find and sort all ants so Queen is first
        Ant[] sortedAnts = FindObjectsOfType<Ant>();
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
        }
    }

    void OnAntSelected(int index)
    {
        if (trackingCamera != null && index >= 0 && index < sortedAnts.Length)
        {
            trackingCamera.target = sortedAnts[index].transform;
        }
    }
}
