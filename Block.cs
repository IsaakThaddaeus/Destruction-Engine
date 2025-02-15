using UnityEngine;

public class Block : MonoBehaviour
{
    public int health = 100;
    public bool grounded = false;
    DestructionManager destructionManager;

    void Start()
    {
        destructionManager = FindFirstObjectByType<DestructionManager>();
    }

    public void damage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            destroy();
        }
    }

    public void destroy()
    {
        destructionManager.destroyBlock(this);
        Destroy(gameObject);

        //Stuff to visualize the destruction

    }
}
