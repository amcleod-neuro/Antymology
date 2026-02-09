using UnityEngine;
using TMPro;

public class LogicScript : MonoBehaviour
{
    // Reference to the TextMeshProUGUI component to display the number of nest blocks
    [SerializeField] TextMeshProUGUI nestBlocksText;

    // Update is called once per frame
    void Update()
    {
        // Update UI to show the current number of nest blocks
        nestBlocksText.text = "Nest Blocks: " + NestScript.Instance.nestBlocks;
    }
}
