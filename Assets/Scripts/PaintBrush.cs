using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PaintBrush : MonoBehaviour
{
    public Renderer bristleRenderer;

    public Material currentBrushMaterial;
    public BoxCollider brushCollider;

    [HideInInspector] public Throwable throwable;

    public Vector3 initialPosition;
    public Quaternion inititialRotation;

    private bool pickedUp = false;

    // Start is called before the first frame update
    void Start()
    {
        // Set all necessary fields
        currentBrushMaterial = bristleRenderer.material;
        brushCollider = GetComponent<BoxCollider>();
        initialPosition = transform.position;
        inititialRotation = transform.rotation;

        throwable = GetComponent<Throwable>();


        // Register on Pickup and on drop listeners
        throwable.onPickUp.AddListener(onPickedUp);
        throwable.onDetachFromHand.AddListener(onDropped);
    }






    // Update is called once per frame
    void Update()
    {
        
    }



    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Brush entered collision with " + other.name);
        // Check for intersection with brick or paint pot
        if (other.gameObject.tag == "Brick")
        {
            // Get the placed Object of brick (Somewhere in the parents)
            PlacedObject brick = other.gameObject.GetComponentInParent<PlacedObject>();
            Debug.Log("BrickName: " + brick.name);

            // Get the mesh renderer of the brick (Somewhere in the children)
            MeshRenderer brickRenderer = brick.GetComponentInChildren<MeshRenderer>();

            // Change brick material
            brickRenderer.material = currentBrushMaterial;
        }
        else if (other.gameObject.tag == "Bucket")
        {
            // Change material of brush
            PaintBucket paintBucket = other.gameObject.GetComponent<PaintBucket>();
            currentBrushMaterial = paintBucket.paintMaterial;
            bristleRenderer.material = currentBrushMaterial;
        }
    }



    /*
     *  Handles behavior when brush is picked up
     *  
     *  - Turns off physical collisions for brush
     *  - Sets pickedUp boolean
     */
    public void onPickedUp()
    {
        setPickedUp();
    }




    /*
     *  Handles behavior when brush is dropped
     *  
     *  - Turn on physical collisions for brush
     *  - Sets pickedUp boolean
     */
    public void onDropped()
    {
        setPickedUp(false);
    }



    /*
     *  Sets whether or not the brush is currently picked up
     */
    public void setPickedUp(bool pickedUp = true)
    {
        this.pickedUp = pickedUp;
    }




    /*
     *  Returns the brush to its initial position and rotation
     */
    public void revertPosition()
    {
        // Check if brush is picked up before reverting
        if (pickedUp)
            return;

        transform.position = initialPosition;
        transform.rotation = inititialRotation;
    }
}
