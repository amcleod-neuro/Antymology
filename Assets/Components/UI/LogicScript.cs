using UnityEngine;
using TMPro;
using Antymology.Terrain;


/// <summary>
/// This script is responsible for button/toggle UI interactions and logic.
/// </summary>
public class LogicScript : MonoBehaviour
{
    public static bool AIisOn = true;

    public void OnButtonClick()
    {
        // Reset the simulation using the convenient method from the ML training
        Antymology.Terrain.WorldManager.Instance.ResetWorldForNewEpisode();
    }

    public void OnToggleAI(bool isOn)
    {
        AIisOn = isOn;
        Debug.Log("AI is now " + (AIisOn ? "ON" : "OFF"));
    }


}
