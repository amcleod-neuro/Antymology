using UnityEngine;
using TMPro;
using Antymology.Terrain;

/// <summary>
/// This script is responsible for logic related to the nest block UI.
/// </summary>
public class NestBlockScript : MonoBehaviour
{
    // Reference to the TextMeshProUGUI component to display the number of nest blocks
    [SerializeField] TextMeshProUGUI nestBlocksText;

    // Static counter for nest blocks
    public static int nestBlockCount = 0;

    // Update is called once per frame
    void Update()
    {
        // Update UI to show the current number of nest blocks
        nestBlocksText.text = "Nest Blocks: " + nestBlockCount;
    }

}
