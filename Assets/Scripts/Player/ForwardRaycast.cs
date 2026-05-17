using UnityEngine;

public class ForwardRaycast : MonoBehaviour
{
    public float rayLength = 3f;
    public LayerMask layerMask;
    public TagHandle tag;

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayLength, layerMask))
        {
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);

            string tag = hit.collider.gameObject.tag;

            if (tag == "Fish1" || tag == "Fish2" || tag == "Fish3" || tag == "Fish4")
            {
                Debug.Log($"Hit fish: {tag}");
            }
            else
            {
                Debug.Log($"Hit: {hit.collider.gameObject.name} (not a fish)");
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * rayLength, Color.green);
        }
    }
}