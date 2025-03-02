using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DestructionManager : MonoBehaviour
{
    public List<Structure> structures = new List<Structure>();
    public List<Block> blocks;
    public List<Link> links = new List<Link>();


    private Dictionary<Block, List<Block>> connectionDictionary = new Dictionary<Block, List<Block>>();

    void Awake()
    {
        FindLinks();
        BuildConnectionDictionary();
        MaterializeStructures(FindStructures(blocks));
        Debug.Log("Structures: " + structures.Count);
    }


    //------------------------------------------------------------------------------------------------
    //Find Links
    //------------------------------------------------------------------------------------------------
    void FindLinks()
    {

        blocks = FindObjectsByType<Block>(FindObjectsSortMode.None).ToList();

        for (int i = 0; i < blocks.Count; i++)
        {

            List<GameObject> nearbyObjects = GetNearbyObjects(blocks[i].gameObject, 1.01f);

            for (int j = 0; j < nearbyObjects.Count; j++)
            {
                if (CheckOverlapWithTemporaryScale(blocks[i].gameObject, nearbyObjects[j], 1.01f))
                {
                    if (nearbyObjects[j].GetComponent<Block>())
                        links.Add(new Link(blocks[i], nearbyObjects[j].GetComponent<Block>()));

                    else
                        blocks[i].grounded = true;
                }
            }
        }

        //Remove duplicate links
        for (int i = 0; i < links.Count; i++)
        {
            for (int j = i + 1; j < links.Count; j++)
            {
                if ((links[i].blockA == links[j].blockA && links[i].blockB == links[j].blockB) || (links[i].blockA == links[j].blockB && links[i].blockB == links[j].blockA))
                {
                    links.RemoveAt(j);
                    j--;
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
            nearbyObjects.Add(collider.gameObject);
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

    List<List<Block>> FindStructures(List<Block> blocks)
    {
        List<List<Block>> newStructures = new List<List<Block>>();
        List<Block> tempBlocks = new List<Block>(blocks);

        while (tempBlocks.Count > 0)
        {
            List<Block> newStructure = new List<Block> { tempBlocks[0] };
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

        return newStructures;
    }
    void MaterializeStructures(List<List<Block>> newStructures)
    {
        for (int i = 0; i < newStructures.Count; i++)
        {
            GameObject newStructureObject = new GameObject("Structure");

            Structure newStructure = newStructureObject.AddComponent<Structure>();
            newStructure.blocks = new List<Block>(newStructures[i]);
            newStructure.transform.parent = transform;
            newStructure.rb = newStructure.gameObject.AddComponent<Rigidbody>();

            for (int j = 0; j < newStructure.blocks.Count; j++)
            {
                newStructure.blocks[j].transform.parent = newStructureObject.transform;
                newStructure.rb.mass += 100;
                if (newStructure.blocks[j].grounded)
                    newStructure.rb.isKinematic = true;
            }

            structures.Add(newStructure);
        }
    }



    //------------------------------------------------------------------------------------------------
    //Connection Dictionary
    //------------------------------------------------------------------------------------------------
    void BuildConnectionDictionary()
    {
        connectionDictionary.Clear();
        foreach (Link link in links)
        {
            if (!connectionDictionary.ContainsKey(link.blockA))
                connectionDictionary[link.blockA] = new List<Block>();
            if (!connectionDictionary.ContainsKey(link.blockB))
                connectionDictionary[link.blockB] = new List<Block>();

            if (!connectionDictionary[link.blockA].Contains(link.blockB))
                connectionDictionary[link.blockA].Add(link.blockB);
            if (!connectionDictionary[link.blockB].Contains(link.blockA))
                connectionDictionary[link.blockB].Add(link.blockA);
        }
    }
    List<Block> getAllConnectedBlocks(Block block)
    {
        if (connectionDictionary.TryGetValue(block, out List<Block> connectedBlocks))
        {
            return connectedBlocks;
        }
        return new List<Block>();
    }



    //------------------------------------------------------------------------------------------------
    //Damage
    //------------------------------------------------------------------------------------------------
    public void DamageBlock(Block block, int damage)
    {
        block.health -= damage;
        if (block.health <= 0)
            DestroyBlock(block);
    }
    void DestroyBlock(Block block)
    {
        SwapBlock(block);

        blocks.Remove(block);
        links.RemoveAll(link => link.blockA == block || link.blockB == block);

        Structure structure = structures.Find(structure => structure.blocks.Contains(block));
        structure.blocks.Remove(block);
        structure.transform.DetachChildren();
        structures.Remove(structure);
        Destroy(structure.gameObject);


        BuildConnectionDictionary();
        MaterializeStructures(FindStructures(structure.blocks));
    }

    public void DamagesBlocks(Vector3 position, float radius, int damage)
    {
        List<Block> destroyedBlocks = new List<Block>();
        List<Block> blocksInRange = blocks.FindAll(block => Vector3.Distance(block.transform.position, position) < radius);

        foreach (Block block in blocksInRange)
        {
            block.health -= damage;
            if (block.health <= 0)
                destroyedBlocks.Add(block);
        }

        DestroyBlocks(destroyedBlocks);
    }
    void DestroyBlocks(List<Block> destroyedBlocks)
    {
        List<Block> remainingBlocks = new List<Block>();

        foreach (Block block in destroyedBlocks)
        {
            SwapBlock(block);

            blocks.Remove(block);
            links.RemoveAll(link => link.blockA == block || link.blockB == block);

            Structure structure = structures.Find(structure => structure.blocks.Contains(block));
            if (structure != null)
            {
                structure.blocks.Remove(block);
                structure.transform.DetachChildren();
                structures.Remove(structure);
                Destroy(structure.gameObject);
                remainingBlocks.AddRange(structure.blocks);
            }
        }

        BuildConnectionDictionary();
        MaterializeStructures(FindStructures(remainingBlocks));

    }

    void SwapBlock(Block block)
    {
        GameObject fracturedBlock = Instantiate(block.fractureModels[Random.Range(0, block.fractureModels.Count)], block.transform.position, block.transform.rotation);

        foreach (Transform child in fracturedBlock.transform)
        {
            int lifeTime = Random.Range(5, 15);
            child.transform.localScale = 0.95f * block.transform.localScale;
            child.gameObject.GetComponent<Rigidbody>().AddExplosionForce(250, block.transform.position, 10);
            Destroy(child.gameObject, lifeTime);
        }

        Destroy(block.gameObject);
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


