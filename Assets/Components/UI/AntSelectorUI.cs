using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AntSelectorUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown antSelector; // Dropdown to select which ant to track
    [SerializeField] TrackingCameraScript trackingCamera; // Reference to the camera script to set the target
    
    // List of all ant transforms - sorted with Queen first
    private List<Transform> allAnts = new List<Transform>();

    // Function to update the dropdown with all ants in the scene
    public void UpdateAntSelector()
    {
        // Find all ants in the scene
        Ant[] antArray = FindObjectsOfType<Ant>();

        // Clear the previous list and dropdown
        allAnts.Clear();
        if (antSelector != null)
            antSelector.options.Clear();

        // Sort ants so Queen is first, then workers
        System.Array.Sort(antArray, (a, b) => {
            bool aIsQueen = a is QueenAnt;
            bool bIsQueen = b is QueenAnt;
            if (aIsQueen && !bIsQueen) return -1;
            if (!aIsQueen && bIsQueen) return 1;
            return 0;
        });

        // Collect all ant transforms
        foreach (Ant ant in antArray)
        {
            allAnts.Add(ant.transform);
        }

        // Setup dropdown
        if (antSelector != null)
        {
            antSelector.onValueChanged.AddListener(OnAntSelected);
            
            // Fill dropdown with ant names (QueenAnt first, then Ant 2, Ant 3, etc.)
            List<string> antNames = new List<string>();
            for (int i = 0; i < allAnts.Count; i++)
            {
                if (i == 0)
                    antNames.Add("QueenAnt");
                else
                    antNames.Add($"Ant {i + 1}");
            }
            antSelector.AddOptions(antNames);
            
        }

    void OnAntSelected(int index)
    {
        if (trackingCamera != null && index >= 0 && index < allAnts.Count)
        {
            trackingCamera.target = allAnts[index];
        }
    }
    }
}
