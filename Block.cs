using System.Collections.Generic;
using Unity.VisualScripting;
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
            int lifeTime = Random.Range(5, 15);
            child.transform.localScale = 0.95f * transform.localScale;
            child.gameObject.GetComponent<Rigidbody>().AddExplosionForce(250, transform.position, 10);
            Destroy(child.gameObject, lifeTime);
        }

        Destroy(gameObject);

        //Stuff to visualize the destruction

    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Child " + gameObject.name + " hat eine Kollision mit: " + collision.gameObject.name);

    }
}
