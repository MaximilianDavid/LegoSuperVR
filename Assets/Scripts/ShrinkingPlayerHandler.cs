using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ShrinkingPlayerHandler : MonoBehaviour
{

    [HideInInspector] public LineRenderer lineRenderer;
    public Material lineMaterial;

    [HideInInspector] public Throwable throwable;
    [HideInInspector] public GameObject playerGameObject;


    [HideInInspector] public float scale;



    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool pickedUp;










    // Start is called before the first frame update
    void Start()
    {
        // Initialize fields
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        scale = transform.localScale.x;
        playerGameObject = GridBuildingSystemVR.Instance.playerGameObject;


        // Get necessary components
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startWidth = transform.localScale.x;
        lineRenderer.endWidth = lineRenderer.startWidth;
        lineRenderer.material = GridBuildingSystemVR.Instance.previewGhostMaterial;

        throwable = GetComponent<Throwable>();


        // Add listeners to signals
        throwable.onDetachFromHand.AddListener(onDropped);
        throwable.onPickUp.AddListener(onPickedUp);
    }









    // Update is called once per frame
    void Update()
    {
        // Paint Raycast down to plate
        if(pickedUp)
        {
            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");

            Physics.queriesHitBackfaces = true;
            Physics.Raycast(transform.position, Vector3.down, out hit, 999f, mask);
            Physics.queriesHitBackfaces = false;

            if (hit.collider)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, hit.point);
            }
            else
            {
                lineRenderer.enabled = false;
            }
        }
    }






    /*
     *  Sets the picked up bool
     */
    private void onPickedUp()
    {
        setPickedUp();
    }






    /*
     *  Whenever object is dropped, it is reverted to its original position and player is shrunk 
     *  and teleported.
     */
    private void onDropped()
    {
        try
        {
            teleportAndShrinkPlayer();
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }

        // Disable line Renderer
        lineRenderer.enabled = false;

        resetPosition();
        setPickedUp(false);
    }





    /*
     *  Sets pickedUp to the given value
     */
    private void setPickedUp(bool pickedUp = true)
    {
        this.pickedUp = pickedUp;
    }





    /*
     *  Casts a downward ray and teleports player to intersection point with the structure.
     *  
     *  Shrinks player to the scale of the transform.
     */
    private void teleportAndShrinkPlayer()
    {
        // Get Position within the plate
        RaycastHit hit;
        LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");


        Physics.queriesHitBackfaces = true;
        Physics.Raycast(transform.position, Vector3.down, out hit, 999f, mask);
        Physics.queriesHitBackfaces = false;


        if(!hit.collider)
            throw new GridBuildingSystemVR.NoIntersectionException("No intersection with plate or bricks!");


        // Shrink and teleport player
        playerGameObject.transform.localScale = new Vector3(scale, scale, scale);
        playerGameObject.transform.position = hit.point;


        // Deactivate player's hands
        GridBuildingSystemVR.Instance.disableHands();

        // Set player as shrunk
        GridBuildingSystemVR.Instance.setPlayerShrunk();
    }





    /*
     *  Resets the objects position and rotation to its original ones.
     */
    public void resetPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

}
