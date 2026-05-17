using UnityEngine;
using UnityEngine.InputSystem;

public class ForwardRaycast : MonoBehaviour
{
    public float rayLength = 3f;
    public LayerMask layerMask;
    Canvas currentInfoCanvas;

    public SwimTryOutMovementController SwimTryOutMovementController;

    public GameObject canvas1;
    public GameObject canvas2;
    public GameObject canvas3;
    public GameObject canvas4;
    
    private bool active = true;
    void Update()
    {
        if (active)
        {

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, rayLength, layerMask))
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
                string tag = hit.collider.gameObject.tag;


                Canvas infoCanvas = hit.collider.transform.root.GetComponentInChildren<Canvas>(true);



                if (infoCanvas != currentInfoCanvas)
                {
                    HideCurrentCanvas();
                    currentInfoCanvas = infoCanvas;
                    currentInfoCanvas.gameObject.SetActive(true);
                }

                if (Keyboard.current.eKey.wasPressedThisFrame) // ← new input system
                {
                    Debug.Log("LOAD THE THING OF THE FISH " + tag);


                    if (tag == "Fish1")
                    {
                        canvas1.SetActive(true);

                       
                        SwimTryOutMovementController.enabled = false;
                        active = false;

                    }
                    else if (tag == "Fish2")
                    {
                        canvas2.SetActive(true);
                        
                        SwimTryOutMovementController.enabled = false;
                        active = false;

                    }
                    else if (tag == "Fish3")
                    {
                        canvas3.SetActive(true);

                        
                        SwimTryOutMovementController.enabled = false;
                        active = false;
                    }
                    else if (tag == "Fish4")
                    {
                        canvas4.SetActive(true);
                        
                        SwimTryOutMovementController.enabled = false;
                        active = false;

                    }
                }



            }
            else
            {
                Debug.DrawRay(transform.position, transform.forward * rayLength, Color.green);
                HideCurrentCanvas();
            }



        }else
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HideAll();
                active = true;
                SwimTryOutMovementController.enabled = true;
            }


        }
        
        
        
    }

    void HideCurrentCanvas()
    {
        if (currentInfoCanvas != null)
        {
            currentInfoCanvas.gameObject.SetActive(false);
            currentInfoCanvas = null;
        }
    }

    void HideAll()
    {
        canvas1.SetActive(false);
        canvas2.SetActive(false);
        canvas3.SetActive(false);
        canvas4.SetActive(false);
    }

}