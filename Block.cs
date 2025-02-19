using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public int health = 100;
    public bool grounded = false;
    public List<GameObject> fractureModels = new List<GameObject>();
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
      
        GameObject fracturedBlock = Instantiate(fractureModels[Random.Range(0, fractureModels.Count)], transform.position, transform.rotation);
        foreach (Transform child in fracturedBlock.transform)
        {
            child.gameObject.GetComponent<Rigidbody>().AddExplosionForce(1000, transform.position, 10);
            Destroy(child.gameObject, 10);
        }
        
        Destroy(gameObject);

        //Stuff to visualize the destruction

    }
}
