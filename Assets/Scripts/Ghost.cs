using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 *  Preview that will be displayed when placing a brick
 */
public class Ghost : MonoBehaviour
{

    //[SerializeField] GridBuildingSystemVR grid; // Corresponding grid system
    [SerializeField] Material ghostMaterial;

    private Transform visual;   
    private PlacedObjectTypeSO brickType;

    private PlacedObject assignedBrick;

    public Vector3 scale = Vector3.one;


    private void Start()
    {

    }




    /*
     *  Sets all necessary values for the ghost
     */
    public void SetupGhost(Material ghostMaterial, Vector3 scale)
    {
        this.ghostMaterial = ghostMaterial;
        this.scale = scale;

        RefreshVisual();
    }






    private void LateUpdate()
    {
    }




    /*
     *  Updates the ghost's Position, Rotation, Visibility
     */
    public void UpdateGhost()
    {
        if (!assignedBrick)
            return;
        try
        {
            // Get the position and rotation to display the preview
            SnapPoint snapPoint = GridBuildingSystemVR.Instance.GetSnapPoint(assignedBrick);
            Vector3 targetPosition = snapPoint.worldLocation;

            if (visual == null)
                RefreshVisual();

            transform.position = targetPosition;
            transform.rotation = GridBuildingSystemVR.Instance.GetPlacedObjectRotation(assignedBrick);

            // Rotate ghost around plate center
            transform.RotateAround(
                GridBuildingSystemVR.Instance.plateCenter,
                new Vector3(0, 1, 0),
                -GridBuildingSystemVR.Instance.currentGlobalRotation);


        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }
        }
    }




    
    /*
     *  Reloads the visual for the preview, based on the currently held brick type
     */
    public void RefreshVisual()
    {
        if(visual != null)
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        if (!assignedBrick)
            return;

        //PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystemVR.Instance.GetCurrentGhosttPlacedObjectTypeSO();


        if (brickType != null)
        {
            visual = Instantiate(brickType.ghostVisual, Vector3.zero, Quaternion.identity);
            visual.localScale = scale;
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetLayerRecursive(visual.gameObject, 0);
        }
    }







    /*
     *  Assigns the ghost to  the given brick
     */
    public void AssignToBrick(PlacedObject brick)
    {
        // Unsassign brick if already assigned
        if(assignedBrick)
        {
            UnassignFromBrick();
        }

        assignedBrick = brick;
        brickType = brick.placedObjectTypeSO;

        brick.assignedGhost = this;
    }






    /*
     *  Removes the ghost from its current brick
     */
    public void UnassignFromBrick()
    {
        if (!assignedBrick)
            return;

        assignedBrick.assignedGhost = null;
        assignedBrick = null;
        brickType = null;
    }




    /*
     *  Returns wether the ghost is currently assigned to a brick
     */
    public bool IsAssignedToBrick()
    {
        return assignedBrick;
    }




    /*
     *  Sets a given gameObject and all its children to the given layer
     */
    private void SetLayerRecursive(GameObject targetObject, int layer)
    {
        targetObject.layer = layer;
        foreach(Transform child in targetObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }




    /*
     *  Makes the preview visible
     */
    public void Activate()
    {
        if (visual == null)
            return;

        visual.gameObject.SetActive(true);
    }



    /*
     *  Make the preview invisible
     */
    public void Deactivate()
    {
        if (visual == null)
            return;

        visual.gameObject.SetActive(false);
    }



    /*
     *  Returns wether the preview is currently active
     */
    public bool IsActive()
    {
        if (visual == null)
            return false;

        return visual.gameObject.activeSelf;
    }
}
