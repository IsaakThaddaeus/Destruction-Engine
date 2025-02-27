using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Structure : MonoBehaviour
{
    public List<Block> blocks;
    public Rigidbody rb;
    DestructionManager destructionManager;

    void Awake()
    {
        destructionManager = FindFirstObjectByType<DestructionManager>();
    }

    void OnCollisionEnter(Collision collision)
    {
 
        
    }

}
