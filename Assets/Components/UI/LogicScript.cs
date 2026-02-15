using UnityEngine;
using TMPro;
using Antymology.Terrain;


// <summary>
/// This script is responsible for handling UI interactions and logic.
/// </summary>
public class LogicScript : MonoBehaviour
{
    // Reference to the TextMeshProUGUI component to display the number of nest blocks
    [SerializeField] TextMeshProUGUI nestBlocksText;
    public bool AIisOn = true;
    
    // Static counter for nest blocks
    public static int nestBlockCount = 0;

    // Update is called once per frame
    void Update()
    {
        // Update UI to show the current number of nest blocks
        nestBlocksText.text = "Nest Blocks: " + nestBlockCount;
    }

    public void OnButtonClick()
    {
        // Reset the simulation using the convenient method from the ML training
        WorldManager.ResetWorldForNewEpisode();
    }

    public void OnToggleAI(bool isOn)
    {
        AIisOn = isOn;
        Debug.Log("AI is now " + (AIisOn ? "ON" : "OFF"));
    }


}
