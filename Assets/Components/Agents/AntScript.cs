using UnityEngine;

public class AntScript : MonoBehaviour
{
    // Public variables to be modified and private variables to track health
    public float maxHealth = 100;
    private float currentHealth;
    public float healthLossPerSecond = 1;
    public float healthGainFromMulch = 20;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         currentHealth = maxHealth;
         Debug.Log("Ant health set to " + currentHealth);

         // Triggers health loss to be repeated every second
         InvokeRepeating(nameof(LoseHealth), 1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {

        // Kill ant if health is dropped to zero
        if (currentHealth <= 0)
        {
            Debug.Log("Ant has died.");
            Destroy(gameObject);
        }
    }

    // Function to lose health over time
    void LoseHealth()
    {
        currentHealth -= healthLossPerSecond;
        Debug.Log("Ant health decreased to " + currentHealth);
    }

    void MoveAnt()
    {
        // Placeholder for movement
    }


}
