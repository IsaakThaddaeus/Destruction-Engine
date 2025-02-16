using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DestructionManager : MonoBehaviour
{
    public List<Structure> structures;
    public List<Block> blocks;
    public List<Link> links;

    void Start()
    {
        FindLinks();
        InitStructures();
        Debug.Log("Structures: " + structures.Count);
    }


    //------------------------------------------------------------------------------------------------
    //Find Links
    //------------------------------------------------------------------------------------------------
    void FindLinks()
    {
        links = new List<Link>();
        blocks = FindObjectsByType<Block>(FindObjectsSortMode.None).ToList();

        for (int i = 0; i < blocks.Count; i++)
        {

            for (int j = i + 1; j < blocks.Count; j++)
            {
                if (CheckOverlapWithTemporaryScale(blocks[i].gameObject, blocks[j].gameObject, 1.01f))
                {
                    Debug.Log("Linking " + blocks[i].gameObject.name + " and " + blocks[j].name);
                    links.Add(new Link(blocks[i], blocks[j]));
                }
            }

            List<GameObject> nearbyObjects = GetNearbyObjects(blocks[i].gameObject, 1.01f);

            for (int j = 0; j < nearbyObjects.Count; j++)
            {
                if (CheckOverlapWithTemporaryScale(blocks[i].gameObject, nearbyObjects[j], 1.01f))
                {
                    Debug.Log("Linking " + blocks[i].gameObject.name + " and " + nearbyObjects[j].name);
                    blocks[i].grounded = true;
                }
            }
        }
    }
    List<GameObject> GetNearbyObjects(GameObject block, float multiplier)
    {
        List<GameObject> nearbyObjects = new List<GameObject>();
        Collider blockCollider = block.GetComponent<Collider>();

        Bounds bounds = blockCollider.bounds;
        Vector3 sphereCenter = bounds.center;
        float sphereRadius = bounds.extents.magnitude * multiplier;

        Collider[] colliders = Physics.OverlapSphere(sphereCenter, sphereRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.GetComponent<Block>() == null)
            {
                nearbyObjects.Add(collider.gameObject);
            }
        }

        return nearbyObjects;
    }
    bool CheckOverlapWithTemporaryScale(GameObject gameObjectA, GameObject gameObjectB, float multiplier)
    {
        Collider colliderA = gameObjectA.GetComponent<Collider>();
        Collider colliderB = gameObjectB.GetComponent<Collider>();

        Vector3 originalScaleA = colliderA.transform.localScale;
        Vector3 originalScaleB = colliderB.transform.localScale;

        colliderA.transform.localScale = originalScaleA * multiplier;
        colliderB.transform.localScale = originalScaleB * multiplier;

        Physics.SyncTransforms();

        Vector3 direction;
        float penetrationDistance;
        bool overlapping = Physics.ComputePenetration(
            colliderA, colliderA.transform.position, colliderA.transform.rotation,
            colliderB, colliderB.transform.position, colliderB.transform.rotation,
            out direction, out penetrationDistance);

        colliderA.transform.localScale = originalScaleA;
        colliderB.transform.localScale = originalScaleB;

        Physics.SyncTransforms();

        return overlapping;
    }


    //------------------------------------------------------------------------------------------------
    //Update Structures
    //------------------------------------------------------------------------------------------------
    void InitStructures()
    {
        structures = new List<Structure>();
        List<Block> tempBlocks = new List<Block>(blocks);

        while (tempBlocks.Count > 0)
        {
            Structure structure = new Structure();
            structure.blocks = new List<Block> { tempBlocks[0] };
            tempBlocks.RemoveAt(0);

            int i = 0;
            while (i < structure.blocks.Count)
            {
                List<Block> connectedBlocks = getAllConnectedBlocks(structure.blocks[i]);
                tempBlocks = tempBlocks.Except(connectedBlocks).ToList();
                structure.blocks = structure.blocks.Union(connectedBlocks).ToList();
                i++;
            }

            structures.Add(structure);
        }



        for (int i = 0; i < structures.Count; i++)
        {
            structures[i].structureObject = new GameObject("Structure " + i);
            structures[i].structureObject.transform.parent = transform;
            Rigidbody structureRigidBody = structures[i].structureObject.AddComponent<Rigidbody>();
            structureRigidBody.mass = 100;

            for (int j = 0; j < structures[i].blocks.Count; j++)
            {
                structures[i].blocks[j].transform.parent = structures[i].structureObject.transform;

                if (structures[i].blocks[j].grounded)
                    structureRigidBody.isKinematic = true;
            }
        }

    }
    void UpdateStructure(Block block)
    {
       
        Structure structure = structures.Find(structure => structure.blocks.Contains(block));
        structure.blocks.Remove(block);

        List<List<Block>> newStructures = new List<List<Block>>();
        List<Block> tempBlocks = new List<Block>(structure.blocks);

        while (tempBlocks.Count > 0)
        {
            List<Block> newStructure = new List<Block>();
            newStructure = new List<Block> { tempBlocks[0] };
            tempBlocks.RemoveAt(0);

            int i = 0;
            while (i < newStructure.Count)
            {
                List<Block> connectedBlocks = getAllConnectedBlocks(newStructure[i]);
                tempBlocks = tempBlocks.Except(connectedBlocks).ToList();
                newStructure = newStructure.Union(connectedBlocks).ToList();
                i++;
            }

            newStructures.Add(newStructure);
        }


        structure.structureObject.transform.DetachChildren();
        structures.Remove(structure);
        Destroy(structure.structureObject);

        for (int i = 0; i < newStructures.Count; i++)
        {
            Structure newStructure = new Structure();
            newStructure.blocks = new List<Block>(newStructures[i]);

            newStructure.structureObject = new GameObject("Structure " + i);
            newStructure.structureObject.transform.parent = transform;
            Rigidbody structureRigidBody = newStructure.structureObject.AddComponent<Rigidbody>();
            structureRigidBody.mass = 100;

            for (int j = 0; j < newStructure.blocks.Count; j++)
            {
                newStructure.blocks[j].transform.parent = newStructure.structureObject.transform;

                if (newStructure.blocks[j].grounded)
                    structureRigidBody.isKinematic = true;
            }

            structures.Add(newStructure);
        }

    }

    List<Block> getAllConnectedBlocks(Block block)
    {
        List<Block> connectedBlocks = new List<Block>();

        for (int i = 0; i < links.Count; i++)
        {
            if (links[i].blockA == block)
                connectedBlocks.Add(links[i].blockB);

            else if (links[i].blockB == block)
                connectedBlocks.Add(links[i].blockA);
        }

        return connectedBlocks;
    }
    public void destroyBlock(Block block)
    {
        blocks.Remove(block);
        links.RemoveAll(link => link.blockA == block || link.blockB == block);
        UpdateStructure(block);

        Debug.Log("Structures: " + structures.Count);
    }


    //------------------------------------------------------------------------------------------------
    //Draw Debug Lines
    //------------------------------------------------------------------------------------------------
    void OnDrawGizmos()
    {
        if (links != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < links.Count; i++)
            {
                Gizmos.DrawLine(links[i].blockA.transform.position, links[i].blockB.transform.position);
            }
        }
    }


}

public class Structure
{
    public List<Block> blocks;
    public GameObject structureObject;
}

public class Link
{
    public Block blockA;
    public Block blockB;

    public Link(Block BlockA, Block BlockB)
    {
        blockA = BlockA;
        blockB = BlockB;

    }

}


