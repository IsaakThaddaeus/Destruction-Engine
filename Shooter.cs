using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class Shooter : MonoBehaviour
{
    Vector3 hitPoint;
    public VisualEffect sparks;
    public VisualEffect dust;
    DestructionManager destructionManager;
    void Awake()
    {
        destructionManager = FindFirstObjectByType<DestructionManager>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.GetComponent<Block>())
                {
                    hitPoint = hit.point;
                    sparks.transform.position = hitPoint;
                    sparks.Play();

                    destructionManager.DamageBlock(hit.collider.gameObject.GetComponent<Block>(), 100);
                    



                }


                //Debug.Log("Hit: " + hit.collider.gameObject.name);
            }

            else
            {
                hitPoint = Vector3.zero;
            }
        }
    }


    void OnDrawGizmos()
    {
        if (hitPoint != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hitPoint, 0.05f);
        }
    }

}
