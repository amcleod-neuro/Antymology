using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AntSelectorUI : MonoBehaviour
{
    [SerializeField] TMP_Dropdown antSelector; // Dropdown to select which ant to track
    [SerializeField] TrackingCameraScript trackingCamera; // Reference to the camera script to set the target
    
    // List of all ant transforms
    private List<Transform> allAnts = new List<Transform>();

    void Start()
    {
        // Wait for ants to be spawned before initializing
        StartCoroutine(InitializeWhenAntsReady());
    }

    IEnumerator<WaitForSeconds> InitializeWhenAntsReady()
    {
        // Wait until we find ants in the scene
        AntScript[] allAntScripts = FindObjectsOfType<AntScript>();

        // Wait until the number of ants matches the expected starting count
        while (allAntScripts.Length < ConfigurationManager.Instance.Starting_Ant_Count)
        {
            yield return new WaitForSeconds(0.1f);
            allAntScripts = FindObjectsOfType<AntScript>();
        }

        // Now collect all ant transforms
        foreach (AntScript ant in allAntScripts)
        {
            allAnts.Add(ant.transform);
        }

        // Setup dropdown
        if (antSelector != null)
        {
            antSelector.onValueChanged.AddListener(OnAntSelected);
            
            // Fill dropdown with ant names
            List<string> antNames = new List<string>();
            for (int i = 0; i < allAnts.Count; i++)
            {
                antNames.Add($"Ant {i + 1}");
            }
            antSelector.AddOptions(antNames);
            
            // Select the first ant
            if (allAnts.Count > 0 && trackingCamera != null)
            {
                trackingCamera.target = allAnts[0];
            }
        }
    }

    void OnAntSelected(int index)
    {
        if (trackingCamera != null && index >= 0 && index < allAnts.Count)
        {
            trackingCamera.target = allAnts[index];
        }
    }
}
