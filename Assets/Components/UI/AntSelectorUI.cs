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

if (antSelector == null) return;

    // Clear existing options
    antSelector.onValueChanged.RemoveListener(OnAntSelected);
    antSelector.ClearOptions();

    // Find all ants in the scene and sort them with the queen first, then workers
    sortedAnts = FindObjectsOfType<Ant>();

    if (sortedAnts.Length == 0)
    {
        Debug.LogWarning("AntSelectorUI: No ants found in scene");
        return;
    }

    System.Array.Sort(sortedAnts, (a, b) =>
    {
        bool aIsQueen = a is QueenAnt;
        bool bIsQueen = b is QueenAnt;
        if (aIsQueen && !bIsQueen) return -1;
        if (!aIsQueen && bIsQueen) return 1;
        return 0;
    });

    // Populate dropdown options with ant names
    List<string> antNames = new List<string>();
    foreach (var ant in sortedAnts)
        antNames.Add(ant.gameObject.name);

    antSelector.AddOptions(antNames);
    antSelector.onValueChanged.AddListener(OnAntSelected);

    // Default to selecting the first ant (ideally the queen) when updating
    antSelector.value = 0;
    antSelector.RefreshShownValue();
    OnAntSelected(0);
    }

    void OnAntSelected(int index)
    {
        if (trackingCamera != null && index >= 0 && index < sortedAnts.Length && sortedAnts[index] != null)
        {
            trackingCamera.target = sortedAnts[index].transform;
        }
    }
}
