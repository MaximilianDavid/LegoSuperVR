using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;


/*
 *  Logic to handle building a brick structure within a 3D grid in VR
 */
public class GridBuildingSystemVR : MonoBehaviour
{
    public static GridBuildingSystemVR Instance { get; private set; }

    public SteamVR_Action_Single squeezeAction;
    public SteamVR_Action_Boolean triggerAction;
    public SteamVR_Action_Boolean leftDirectionAction;
    public SteamVR_Action_Boolean rightDirectionAction;
    public SteamVR_Action_Boolean downDirectionAction;


    [SerializeField] private float maximumAngleCorrection;
    [SerializeField] private Ghost ghost;
    [SerializeField] private List<PlacedObject> bricks;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOVGhosts;
    [SerializeField] private Material deleteGhostMaterial;
    [SerializeField] private Material previewGhostMaterial;
    [SerializeField] private Transform parentTransform;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private Renderer buildManualScreen;
    [SerializeField] private List<Material> buildManualPages;
    [SerializeField] private CircularDrive circularDrive;


    private int currentBuildManualPage = 0;


    

    private LineRenderer anchorLineRenderer;
    private LineRenderer frontLeftLineRenderer;
    private LineRenderer backLeftLineRenderer;
    private LineRenderer backRightLineRenderer;



    [SerializeField] private List<List<PlacedObject>> placedBricks = new List<List<PlacedObject>>();


    private PlacedObjectTypeSO placedObjectTypeSO = null;



    private Transform currentlyHeldObject = null;
    private PlacedObject currentlyHeldPlacedObject = null;



    private LineRenderer lineRenderer = null;


    [SerializeField] private int gridWidth = 24;
    [SerializeField] private int gridLength = 24;
    [SerializeField] private int gridHeight = 24;
    [SerializeField] private float cellSize = 10f;
    [SerializeField] private float brickOffset = 4.8f;
    [SerializeField] private float brickHeight = 9.6f;
    [SerializeField] private float basePlateHeight = .65f;
    [SerializeField] private float scale = 1f;

    [SerializeField] public float currentGlobalRotation = 0f;

    [SerializeField] private Vector3 plateOrigin;
    [SerializeField] public Vector3 plateCenter;



    [SerializeField] private float maximumSnapDistance = 9.6f * 1.5f;


    [SerializeField] private float previewLineWidth = 0.001f;


    private List<GridXZ<GridObject>> grids;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;

   


    public event EventHandler OnSelectedBrickChanged;




    /*
     *  Used to release a held Brick
     *  
     *  Snaps the brick to the grid, if it is currently near a valid grid position
     */
    public void releaseBrick()
    {
        Transform brickTransform = currentlyHeldObject;
        PlacedObject heldBrick = currentlyHeldPlacedObject;
        GameObject heldVisualBrick = heldBrick.VisualBrick;

        // Turn on Collisions again
        heldBrick.ignoreCollisions(false);

        // Reset currently held Object
        currentlyHeldObject = null;
        currentlyHeldPlacedObject = null;
        placedObjectTypeSO = null;


        ghost.Deactivate();


        SnapPoint snapPoint;


        anchorLineRenderer.enabled = false;
        frontLeftLineRenderer.enabled = false;
        backLeftLineRenderer.enabled = false;
        backRightLineRenderer.enabled = false;


        Vector3 absAngles = new Vector3(
            Mathf.Abs(brickTransform.eulerAngles.x),
            Mathf.Abs(brickTransform.eulerAngles.y),
            Mathf.Abs(brickTransform.eulerAngles.z));

        float absDifX = Mathf.Abs(Mathf.DeltaAngle(brickTransform.eulerAngles.x, 0));
        float absDifZ = Mathf.Abs(Mathf.DeltaAngle(brickTransform.eulerAngles.z, 0));
        float distanceToCollision = 0f;



        try
        {
            // Calculate the snap point for the held brick
            LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");
            //Physics.Raycast(heldBrick.transform.position, Vector3.down, out RaycastHit raycastHit, 99f, mask);

            /*
            if (!raycastHit.collider)
                throw new CannotBuildHereException();
            */
            Debug.Log("Calculating Snap Point!");
            snapPoint = GetSnapPoint(heldBrick);
            Debug.Log("Snap Point Calculated! " + snapPoint);

            int gridNumberForBuild = snapPoint.gridNumber;
            //int gridNumberForBuild = GetGridNumber(new Vector3(snapPoint.x, snapPoint.y + brickHeight * 0.5f, snapPoint.z));
            //grids[gridNumberForBuild].GetXZ(raycastHit.point, out int x, out int z);
            //grids[gridNumberForBuild].GetXZ(snapPoint, out int x, out int z);

            Debug.Log("Grid number: " + gridNumberForBuild);

            /*
            if (absDifX > maximumAngleCorrection)
            {
                Debug.Log("Angle on X too far!");
                throw new CannotBuildHereException();
            }

            if (absDifZ > maximumAngleCorrection)
            {
                Debug.Log("Angle on X too far!");
                throw new CannotBuildHereException();
            }

            if (distanceToCollision > maximumSnapDistance)
            {
                Debug.Log("Collision too far away!");
                throw new CannotBuildHereException();
            }
            */

            if (gridNumberForBuild >= 0)
            {
                PlacedObjectTypeSO.Dir dir = heldBrick.GetClosestDir();
                //List<Vector2Int> gridPositionList = heldBrick.placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);
                List<Vector2Int> gridPositionList = heldBrick.placedObjectTypeSO.GetGridPositionList(snapPoint.gridLocation, dir);
                Vector2Int rotationOffset = heldBrick.placedObjectTypeSO.GetRotationOffset(dir);
                heldBrick.SetBaseSupport(true);
                heldBrick.IsPlacedInGrid = true;
                heldBrick.makeKinematic();
                brickTransform.position = snapPoint.worldLocation;
                brickTransform.rotation = GetPlacedObjectRotation(heldBrick);

                // Rotate brick around plate's center
                brickTransform.RotateAround(plateCenter, new Vector3(0, 1, 0), -currentGlobalRotation);


                Debug.Log("Brick occupies:");
                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grids[gridNumberForBuild].GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(heldBrick);
                    Debug.Log(gridPosition);
                }
                Debug.Log("In grid " + gridNumberForBuild);


                // Set Grid number and Position for placed Brick
                heldBrick.OccupiedGridPositions = gridPositionList;
                heldBrick.SetGridNumber(gridNumberForBuild);


                // Set Connections if the current brick is not set on the baseplate
                if (gridNumberForBuild > 0)
                {
                    ConnectBrick(heldBrick);
                }

                // Add the Brick to the List of placed Bricks
                placedBricks[gridNumberForBuild].Add(heldBrick);

                // Set Layer for the brick
                MyUtilities.MyUtils.SetLayerRecursively(heldBrick.gameObject, 12);
            }




        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }

        heldBrick.putDown();
    }


















    /*
     *  Pickup the given brick object 
     */
    public void pickupBrick(PlacedObject placedObject)
    {
        placedObject.makePhysicsEnabled();
        placedObject.pickUp();

        
        // Set brick as currently held object
        currentlyHeldPlacedObject = placedObject;
        currentlyHeldObject = placedObject.transform;
        placedObjectTypeSO = placedObject.placedObjectTypeSO;
        placedObject.ignoreCollisions();
        //MyUtilities.MyUtils.SetLayerRecursively(currentlyHeldPlacedObject.gameObject, 0);

        RefreshSelectedObjectType();

        if (placedObject.HasBaseSupport())
        {
            // Remove Brick from placed Bricks
            placedBricks[placedObject.GetGridNumber()].Remove(placedObject);


            Debug.Log("Removing " + placedObject + " from grid!");
            RemoveFromGrid(placedObject);
            Debug.Log("Removing connections of " + placedObject);
            RemoveAllConnectionsOf(placedObject);
            Debug.Log("DONE!");

            // Check integrity of remaining bricks
            CheckBrickConnections();
        }


        currentlyHeldPlacedObject.OccupiedGridPositions.Clear();
        
    }















    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        Instance = this;

        plateOrigin = transform.position;

        this.scale = transform.localScale.x;
        this.brickHeight = 9.6f * scale;
        this.cellSize = 10 * scale;
        this.brickOffset = 4.8f * scale;
        this.basePlateHeight = .65f * scale * 2;
        this.maximumSnapDistance = brickHeight * 1.5f;


        plateCenter = new Vector3(
            plateOrigin.x + (80 * scale),
            plateOrigin.y,
            plateOrigin.z + (80 * scale));


        grids = new List<GridXZ<GridObject>>();


        // Set materials for manual screen
        Material[] screenMats = buildManualScreen.materials;
        screenMats[1] = buildManualPages[0];
        buildManualScreen.materials = screenMats;


        bool showDebug = true;
        for (int i = 0; i < gridHeight; i++)
        {
            // Add Lists for Bricks within each grid
            placedBricks.Add(new List<PlacedObject>());


            // Add Grids
            grids.Add(
                new GridXZ<GridObject>(
                    gridWidth, 
                    gridLength, 
                    cellSize, 
                    parentTransform.position + new Vector3(0, basePlateHeight + i * brickOffset, 0), 
                    (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z)));

            if (i == 0 && showDebug)
            {
                grids[i].debugLineColor = Color.magenta;
                grids[i].drawGridLines();
            }
        }


        placedObjectTypeSO = placedObjectTypeSOList[0];

        // Initialize line renderers
        GameObject anchorRendererObject = new GameObject("anchorRendererObject");
        anchorLineRenderer = anchorRendererObject.AddComponent<LineRenderer>();
        anchorLineRenderer.startWidth = previewLineWidth;
        anchorLineRenderer.endWidth = previewLineWidth;
        anchorLineRenderer.material = previewGhostMaterial;

        GameObject frontLeftAnchorRendererObject = new GameObject("frontLeftAnchorRendererObject");
        frontLeftLineRenderer = frontLeftAnchorRendererObject.AddComponent<LineRenderer>();
        frontLeftLineRenderer.startWidth = previewLineWidth;
        frontLeftLineRenderer.endWidth = previewLineWidth;
        frontLeftLineRenderer.material = previewGhostMaterial;

        GameObject backLeftAnchorRendererObject = new GameObject("backLeftAnchorRendererObject");
        backLeftLineRenderer = backLeftAnchorRendererObject.AddComponent<LineRenderer>();
        backLeftLineRenderer.startWidth = previewLineWidth;
        backLeftLineRenderer.endWidth = previewLineWidth;
        backLeftLineRenderer.material = previewGhostMaterial;

        GameObject backRightAnchorRendererObject = new GameObject("backRightAnchorRendererObject");
        backRightLineRenderer = backRightAnchorRendererObject.AddComponent<LineRenderer>();
        backRightLineRenderer.startWidth = previewLineWidth;
        backRightLineRenderer.endWidth = previewLineWidth;
        backRightLineRenderer.material = previewGhostMaterial;


        anchorLineRenderer.enabled = false;
        frontLeftLineRenderer.enabled = false;
        backLeftLineRenderer.enabled = false;
        backRightLineRenderer.enabled = false;


        RefreshSelectedObjectType();
        ghost.Deactivate();
    }

















    private void LateUpdate()
    {
        if(currentlyHeldPlacedObject != null)
        {

            // Get all corners of held brick
            //GameObject anchor = currentlyHeldPlacedObject.Anchor;
            GameObject anchor = currentlyHeldPlacedObject.GetAnchorForCurrentRotation();
            GameObject mainAnchor = currentlyHeldPlacedObject.Anchor;
            GameObject frontLeftAnchor = currentlyHeldPlacedObject.FrontLeftAnchor;
            GameObject backLeftAnchor = currentlyHeldPlacedObject.BackLeftAnchor;
            GameObject backRightAnchor = currentlyHeldPlacedObject.BackRightAnchor;


            // Get the angles of the held brick
            Vector3 absAngles = new Vector3(
                Mathf.Abs(currentlyHeldObject.eulerAngles.x),
                Mathf.Abs(currentlyHeldObject.eulerAngles.y),
                Mathf.Abs(currentlyHeldObject.eulerAngles.z));

            // Calculate angle offsets
            float absDifX = Mathf.Abs(Mathf.DeltaAngle(currentlyHeldObject.eulerAngles.x, 0));
            float absDifZ = Mathf.Abs(Mathf.DeltaAngle(currentlyHeldObject.eulerAngles.z, 0));
            float distanceToCollision = 0f;


            // Calculate estimated placement for currently held brick
            RaycastHit hit;
            LayerMask previewMask = LayerMask.GetMask("GridBuildingSystem", "Brick");
            Vector3 floorNormal = new Vector3(currentlyHeldObject.position.x, 0, currentlyHeldObject.position.z).normalized;

            Physics.queriesHitBackfaces = true;
            if(anchor.transform.position.y < this.parentTransform.position.y)
                Physics.Raycast(anchor.transform.position, Vector3.up, out hit, 999f, previewMask);
            else
                Physics.Raycast(anchor.transform.position, Vector3.down, out hit, 999f, previewMask);
            Physics.queriesHitBackfaces = false;

            if (hit.collider)
            {
                /*
                // Get Gridnumbers
                int hitGridNumber = GetGridNumber(hit.point);
                int heldGridNumber = GetGridNumber(new Vector3(anchor.transform.position.x, anchor.transform.position.y + brickHeight*0.5f, anchor.transform.position.z));
                //grids[heldGridNumber].GetXZ(anchor.transform.position, out int heldX, out int heldZ);
                Vector3 snapPoint = GetSnapPoint(currentlyHeldPlacedObject);
                int heldX = (int)snapPoint.x;
                int heldZ = (int)snapPoint.z;
                currentlyHeldPlacedObject.SetOrigin(new Vector2Int(heldX, heldZ));
                currentlyHeldPlacedObject.SetDir(currentlyHeldPlacedObject.GetClosestDir());

                // Get highest brick between currently held location and RayCastHit
                PlacedObject highestBrick = CalculateHighestBrickBetween(
                    hitGridNumber, 
                    heldGridNumber,
                    currentlyHeldPlacedObject.GetGridPositionList());

                // Set collision distance to correct value
                if (highestBrick != null)
                {
                    //Vector3 relHeldPosition = new Vector3(0, currentlyHeldObject.position.y, 0);
                    Vector3 relHeldPosition = new Vector3(0, anchor.transform.position.y, 0);
                    Vector3 relBrickPosition = new Vector3(0, highestBrick.transform.position.y + brickHeight, 0);
                    distanceToCollision = Mathf.Abs(Vector3.Distance(relHeldPosition, relBrickPosition));
                }
                else
                {
                    //distanceToCollision = Mathf.Abs(Vector3.Distance(hit.point, currentlyHeldObject.position));
                    distanceToCollision = Mathf.Abs(Vector3.Distance(hit.point, anchor.transform.position));
                }
                */

                // Display Guide Lines
                anchorLineRenderer.enabled = true;
                frontLeftLineRenderer.enabled = true;
                backLeftLineRenderer.enabled = true;
                backRightLineRenderer.enabled = true;

                anchorLineRenderer.SetPosition(0, mainAnchor.transform.position);
                anchorLineRenderer.SetPosition(1, new Vector3(mainAnchor.transform.position.x, hit.point.y, mainAnchor.transform.position.z));

                frontLeftLineRenderer.SetPosition(0, frontLeftAnchor.transform.position);
                frontLeftLineRenderer.SetPosition(1, new Vector3(frontLeftAnchor.transform.position.x, hit.point.y, frontLeftAnchor.transform.position.z));

                backLeftLineRenderer.SetPosition(0, backLeftAnchor.transform.position);
                backLeftLineRenderer.SetPosition(1, new Vector3(backLeftAnchor.transform.position.x, hit.point.y, backLeftAnchor.transform.position.z));

                backRightLineRenderer.SetPosition(0, backRightAnchor.transform.position);
                backRightLineRenderer.SetPosition(1, new Vector3(backRightAnchor.transform.position.x, hit.point.y, backRightAnchor.transform.position.z));
            }
            else
            {
                distanceToCollision = float.MaxValue;


                anchorLineRenderer.enabled = false;
                frontLeftLineRenderer.enabled = false;
                backLeftLineRenderer.enabled = false;
                backRightLineRenderer.enabled = false;
            }



            if (distanceToCollision <= maximumSnapDistance && !ghost.IsActive())
            {
                // Display ghost
                ghost.Activate();


            }
            if(distanceToCollision > maximumSnapDistance && ghost.IsActive())
            {
                ghost.Deactivate();
                Debug.Log("Deactivating ghost due to high distance");
            }



        }
        else
        {
            if (ghost.IsActive())
                ghost.Deactivate();
        }
    }








    private void Update()
    {
        // Update Rotation
        UpdateRotation();

        if(rightDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Advance manual page
            TurnManualPageForward();
        }

        if(leftDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Go back 1 manual page
            TurnManualPageBackward();
        }


       if(downDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            ResetLooseBrickPositions();
        }
    }




    /*
     *  Refreshes the currently held birkc type
     */
    public void RefreshSelectedObjectType()
    {
        OnSelectedBrickChanged?.Invoke(this, EventArgs.Empty);
    }






   


    /*
     *  Calculates a snap point for the given brick object
     *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
     *  BRICK NEEDS TO BE ROTATED AROUND PLATE CENTER WHEN PLACING
     *  OTHERWISE SNAPPOINT IS NOT CORRECT
     */
    public SnapPoint GetSnapPoint(PlacedObject placedObject)
    {
        if (placedObject == null)
            throw new CannotBuildHereException();

        GameObject anchor = placedObject.GetAnchorForCurrentRotation();
        GameObject visualBrick = placedObject.VisualBrick;

        // Calculate closest snapping direction
        PlacedObjectTypeSO.Dir dir = placedObject.GetClosestDir();
        //Debug.Log("Orientation: " + dir);

        

        LayerMask mask = LayerMask.GetMask("GridBuildingSystem", "Brick");
        //Physics.Raycast(anchor.transform.position, Vector3.down, out RaycastHit raycastHit, 99f, mask);

        Physics.queriesHitBackfaces = true;
        // ^ Needs to be enabled in case the anchor is inside the grid's collider
        RaycastHit raycastHit;
        if (anchor.transform.position.y < parentTransform.position.y)
            Physics.Raycast(anchor.transform.position, Vector3.up, out raycastHit, 99f, mask);
        else
            Physics.Raycast(anchor.transform.position, Vector3.down, out raycastHit, 99f, mask);
        Physics.queriesHitBackfaces = false;


        if (!raycastHit.collider)
            throw new CannotBuildHereException("No Hitpoint On Grid!");

        // Get gridNumber of the grid the User is holding the brick in
        int heldInGridNumber = GetGridNumber(
            new Vector3(
                visualBrick.transform.position.x,
                visualBrick.transform.position.y + brickHeight * 0.5f,
                visualBrick.transform.position.z));


        // Reverse rotation for brick anchor
        //Vector3 anchorPointBeforeRotation = ReverseRotation(anchor.transform.position);
        //grids[heldInGridNumber].GetXZ(anchorPointBeforeRotation, out int heldX, out int heldZ);


        // Calculate gridNumber of where the rayCast hits
        Debug.Log("Hitpoint: " + raycastHit.point);
        Vector3 hitPointBeforeRotation = ReverseRotation(raycastHit.point);
        Debug.Log("Hitpoint after reverse : " + hitPointBeforeRotation);
        int gridNumber = GetGridNumber(raycastHit.point);
        grids[gridNumber].GetXZ(hitPointBeforeRotation, out int hitX, out int hitZ);


        // Handle Edge cases
        if (hitX == gridLength)
            hitX -= 1;
        if (hitX == -1)
            hitX = 0;
        if (hitZ == gridWidth)
            hitZ -= 1;
        if (hitZ == -1)
            hitZ = 0;





        // Shift main snappoint according to current rotation
        Vector3 snapWorldLocation = grids[gridNumber].GetWorldPosition(hitX, hitZ); // No reverse rotation necessary?
        snapWorldLocation = ShiftAnchorSnapToMainAnchorSnap(snapWorldLocation, dir, placedObject.placedObjectTypeSO);
        grids[gridNumber].GetXZ(snapWorldLocation, out int x, out int z);
        Vector2Int anchorLocationOnGrid = new Vector2Int(x, z);
        Debug.Log("Grid location after shifting: " + anchorLocationOnGrid);


        SnapPoint snapPoint = new SnapPoint(gridNumber, snapWorldLocation, anchorLocationOnGrid);

        
        List<Vector2Int> gridPositionList = placedObject.placedObjectTypeSO.GetGridPositionList(anchorLocationOnGrid, dir);
        Debug.Log("Grid positions:");
        foreach(Vector2Int gridPos in gridPositionList)
        {
            Debug.Log(gridPos);
        }

        // Calculate highest Brick if there is a Brick between the RaycastHit and the held position
        PlacedObject highestBrickBetween = CalculateHighestBrickBetween(gridNumber, heldInGridNumber, gridPositionList);


        // Calculate the grid number to snap the brick into
        int gridNumberForBuild = 0;

        if (highestBrickBetween != null)
        {
            gridNumberForBuild = GetBuildableGridNumber(gridPositionList, GetGridNumber(
                new Vector3(
                    highestBrickBetween.transform.position.x,
                    highestBrickBetween.transform.position.y + brickHeight * 0.5f,
                    highestBrickBetween.transform.position.z)));
        }
        else
        {
            gridNumberForBuild = GetBuildableGridNumber(gridPositionList, gridNumber);
        }


        // Reduce grid number until brick has base support
        while(!HasPotentialSupport(gridPositionList, gridNumberForBuild))
        {
            gridNumberForBuild = GetBuildableGridNumber(gridPositionList, gridNumberForBuild - 1);
        }

     


        // Return the snapped position for the brick
        if (gridNumberForBuild >= 0)
        {
            Vector2Int rotationOffset = placedObject.placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = 
                grids[gridNumberForBuild].GetWorldPosition(x, z, dir) 
                + new Vector3(0, grids[gridNumberForBuild].GetOriginPosition().y - grids[0].GetOriginPosition().y, 0);

            snapPoint.gridNumber = gridNumberForBuild;
            snapPoint.worldLocation = placedObjectWorldPosition;

            return snapPoint;
        }
        else
        {
            throw new CannotBuildHereException();
        }
    }










    /*
     *  Updates the base plate's rotation and the bricks connected to it
     */
    public void UpdateRotation()
    {
        // Check if plate needs to be rotated
        if (currentGlobalRotation != (circularDrive.outAngle % 360) * 1.5f)
        {
            // Rotate plate around center
            float rotateBy = currentGlobalRotation - (circularDrive.outAngle % 360) * 1.5f;
            transform.RotateAround(plateCenter, new Vector3(0, 1, 0), rotateBy);
            // Rotate all connected bricks around center
            rotateConnectedBricksBy(rotateBy);
            // Save new rotation
            currentGlobalRotation = (circularDrive.outAngle % 360) * 1.5f;
        }
    }













    /*
     *  Rotates all bricks connected to the base plate around the plate's center by the given angle
     */
    public void rotateConnectedBricksBy(float angle)
    {
        foreach(List<PlacedObject> bricksInGrid in placedBricks)
        {
            foreach(PlacedObject brick in bricksInGrid)
            {
                brick.transform.RotateAround(plateCenter, new Vector3(0, 1, 0), angle);
            }
        }
    }













    /*
     *  Returns whether a brick occupying the given positions in the given grid number would have
     *  base support
     */
    public bool HasPotentialSupport(List<Vector2Int> gridPositionList, int gridNumber = 0)
    {
        // Bricks on Baseplate always have support
        if (gridNumber == 0)
            return true;


        // Check if any of the downward positions are occupied
        foreach(Vector2Int gridPosition in gridPositionList)
        {
            if(!grids[gridNumber-1].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
            {
                return true;
            }
        }

        return false;
    }























    /*
     *  Returns the position that equals the snap point for the 
     *  main anchor of the given brick type with the given orientation
     */
    public Vector3 ShiftAnchorSnapToMainAnchorSnap(
        Vector3 snapPoint, 
        PlacedObjectTypeSO.Dir dir, 
        PlacedObjectTypeSO placedObjectTypeSO)
    {
        Vector3 mainSnapPoint = Vector3.zero;

        switch(dir)
        {
            // Shift anchor to the left by width
            case PlacedObjectTypeSO.Dir.Left:
                Debug.Log("Shifting Left by" + placedObjectTypeSO.width);
                mainSnapPoint = ShiftBySquares(snapPoint, placedObjectTypeSO.width - 1, 0);
                break;

            // Shift anchor upwards by width and left by height
            case PlacedObjectTypeSO.Dir.Up:
                mainSnapPoint = ShiftBySquares(snapPoint, placedObjectTypeSO.height - 1, placedObjectTypeSO.width - 1);
                break;

            // Shift anchor up by height
            case PlacedObjectTypeSO.Dir.Right:
                mainSnapPoint = ShiftBySquares(snapPoint, 0, placedObjectTypeSO.height - 1);
                break;


            case PlacedObjectTypeSO.Dir.Down:
            default:
                mainSnapPoint = snapPoint;
                break;
        }

        return mainSnapPoint;
    }


















    /*
     *  Shifts the given position equal to the amount in given grid spaces
     */
    public Vector3 ShiftBySquares(Vector3 position, int z, int x)
    {
        float shiftedZ = position.z + z * cellSize;
        float shiftedX = position.x + x * cellSize;

        return new Vector3(shiftedX, position.y, shiftedZ);
    }











    /*
     *  Returns the position with the plate's current rotation reversed
     */
    public Vector3 ReverseRotation(Vector3 position)
    {
        Vector3 positionBeforeRotation = 
            RotatePointAroundPivot(position, plateCenter, new Vector3(0, currentGlobalRotation, 0));

        return positionBeforeRotation;
    }














    /*
     *  Returns the given point rotated around the given pivot by the given angles
     */
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        // Translate Point to pivot
        Vector3 dir = point - pivot;
        // Rotate point
        dir = Quaternion.Euler(angles) * dir;
        // Translate back
       Vector3 rotatedPoint = dir + pivot;

        return rotatedPoint;
    }




















    /*
     *  Returns the rotation quaternion for the currently held brick 
     */
    public Quaternion GetPlacedObjectRotation()
    {
        if(placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(currentlyHeldPlacedObject.GetClosestDir()), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }



    
    
    /*
     *  Returns the rotation quaternion of the given brick
     */
    public Quaternion GetPlacedObjectRotation(PlacedObject placedObject)
    {
        if (placedObject != null)
        {
            return Quaternion.Euler(0, placedObject.placedObjectTypeSO.GetRotationAngle(placedObject.GetClosestDir()), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }




    /*
     *  Goes through the list of all placed bricks starting with the lowest grid.
     *  Removes the brick from the grid and makes it physics enabled if it's not 
     *  supported by a brick below.
     */
    public void CheckBrickConnections()
    {
        for(int currentGrid = 1; currentGrid < placedBricks.Count; currentGrid++)
        {
            List<PlacedObject> bricksInCurrentGrid = new List<PlacedObject>(placedBricks[currentGrid]);
            foreach (PlacedObject brick in bricksInCurrentGrid)
            {
                if (brick.DownwardConnections.Count <= 0)
                {
                    RemoveAllConnectionsOf(brick);
                    RemoveFromGrid(brick);
                    brick.makePhysicsEnabled();
                    brick.ignoreCollisions(false);
                }
            }
        }
    }




    /*
     *  Removes a given brick from the grid.
     */
    public void RemoveFromGrid(PlacedObject placedObject)
    {
        int gridNumber = placedObject.GetGridNumber();

        if (gridNumber < 0)
            return;

        Debug.Log("Removing from Grid " + gridNumber);

        foreach (Vector2Int gridPosition in placedObject.OccupiedGridPositions)
        {
            grids[gridNumber].GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
        }

        placedBricks[gridNumber].Remove(placedObject);
        placedObject.IsPlacedInGrid = false;
        placedObject.SetBaseSupport(false);
        placedObject.SetGridNumber(-1);
        // Remove from Bricks Layer
        //MyUtilities.MyUtils.SetLayerRecursively(placedObject.gameObject, 0);
        // ^ obsolete since pickup already handles this for picked up bricks
    }







    /*
     *  Connects the given brick to all bricks directly below it
     */
    private void ConnectBrick(PlacedObject placedObject)
    {
        HashSet<PlacedObject> downward = new HashSet<PlacedObject>();


        int gridBelow = placedObject.GetGridNumber() - 1;
        Debug.Log("Grid below is: " + gridBelow);
        if (gridBelow < 0)
            return;

        // Get all bricks below the given brick
        foreach(Vector2Int position in placedObject.OccupiedGridPositions)
        {
            PlacedObject objectInSpaceBelow = 
                grids[gridBelow].GetGridObject(position.x, position.y).GetPlacedObject();// y == z

            Debug.Log("Donward of " + position + " is: " + objectInSpaceBelow);
            if(objectInSpaceBelow != null)
                downward.Add(objectInSpaceBelow); 
        }

        Debug.Log(placedObject + " connected downward to " + downward.Count + " bricks");
        Debug.Log(placedObject + " connected downward to " + downward);


        // Connect the previously calculated bricks to the current one
        foreach (PlacedObject brick in downward)
        {     
            if (brick == null)
            {
                Debug.Log("Downward brick is null!");
            }
            Debug.Log(brick.gameObject.ToString());
            placedObject.AddToDownwardConnections(brick);
            brick.AddToUpwardConnections(placedObject);
        }
    }



    






    /*
     *  Removes all brick connections for the given brick
     */
    public void RemoveAllConnectionsOf(PlacedObject placedObject)
    {
        foreach(PlacedObject downwardBrick in placedObject.DownwardConnections)
        {
            downwardBrick.RemoveFromUpwardConnections(placedObject);
        }
        placedObject.ClearDownwardConnections();


        foreach(PlacedObject upwardBrick in placedObject.UpwardConnections)
        {
            upwardBrick.RemoveFromDownwardConnections(placedObject);
        }
        placedObject.ClearDownwardConnections();
    }




    /*
     *  Returns the scriptable Object for the currently held brick
     */
    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }





    /*
     *  Returns the scriptable object for the ghost, depending on the currently held brick
     */
    public PlacedObjectTypeSO GetCurrentGhosttPlacedObjectTypeSO()
    {
        if (currentlyHeldPlacedObject != null)
            return currentlyHeldPlacedObject.placedObjectTypeSO;
        else
            return null;
    }






    /*
     *  Returns the number of the first grid that has no bricks occupying the given positions
     *  Starting with the given grid number
     */
    public int GetBuildableGridNumber(List<Vector2Int> gridPositions, int startingGridNumber = 0)
    {
        for(int currentGrid = startingGridNumber; currentGrid < gridHeight; currentGrid++)
        {
            bool canBuildHere = true;
            foreach(Vector2Int gridPosition in gridPositions)
            {
                if(!grids[currentGrid].GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    canBuildHere = false;
                    break;
                }
            }

            if (canBuildHere)
                return currentGrid;
        }

        return -1;
    }





    /*
     *  Returns the number of the corresponding grid, for a given world position
     */
    public int GetGridNumber(Vector3 worldPosition)
    {
        int gridNumber = Mathf.FloorToInt((worldPosition.y - grids[0].GetOriginPosition().y) / brickHeight);
        if (gridNumber < 0)
            gridNumber = 0;

        return gridNumber;
    }




    /*
     *  Returns the brick with the highest grid number, with at least one position from the given position list.
     *  Starts with the given lowerGrid and stops at the given higherGrid.
     *  
     *  Returns null if no brick is found!
     */
    private PlacedObject CalculateHighestBrickBetween(int lowerGrid, int higherGrid, List<Vector2Int> gridPositions)
    {
        for(int currentGridNumber = higherGrid; currentGridNumber >= lowerGrid; currentGridNumber--)
        {
            List<PlacedObject> bricksInCurrentGrid = placedBricks[currentGridNumber];
            foreach(PlacedObject brick in bricksInCurrentGrid)
            {
                foreach(Vector2Int gridPosition in brick.OccupiedGridPositions)
                {
                    if (gridPositions.Contains(gridPosition))
                        return brick;
                }
            }
        }
        return null;
    }




    /*
     *  Advances the manual by one page
     */
    private void TurnManualPageForward()
    {
        // If at last page do nothing
        if (currentBuildManualPage >= buildManualPages.Count - 1)
        {
            currentBuildManualPage = buildManualPages.Count - 1;
            return;
        }
        else
        {
            // Show next page
            currentBuildManualPage++;
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage];
            buildManualScreen.materials = screenMats;
        }
    }




    /*
     *  Goes back one page in the manual
     */
    private void TurnManualPageBackward()
    {
        // If at first page do nothing
        if(currentBuildManualPage <= 0)
        {
            currentBuildManualPage = 0;
            return;
        }
        else
        {
            // Show previous page
            currentBuildManualPage--;
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage];
            buildManualScreen.materials = screenMats;
        }
    }





    /*
     *  Resets all bricks with no position within the grid to their
     *  starting position
     */
    private void ResetLooseBrickPositions()
    {
        foreach(PlacedObject brick in bricks)
        {
            if (!brick.IsPlacedInGrid)
                brick.RevertToStartingPosition();
        }
    }




    /*
     *  Exception for not getting a raycast intersection
     */
    public class NoIntersectionException : Exception 
    { 
        public NoIntersectionException() { }

        public NoIntersectionException(string message) 
            : base(message) { }

        public NoIntersectionException(string message, Exception inner)
            : base(message, inner) { }
    }



    /*
     *  Exception for illegal build positions
     */
    public class CannotBuildHereException : Exception
    {
        public CannotBuildHereException() { }

        public CannotBuildHereException(string message)
            : base(message) { }

        public CannotBuildHereException(string message, Exception inner)
            : base(message, inner) { }
    }




    /*
     *  Placeholder object to insert into the grid
     */
    public class GridObject
    {
        private GridXZ<GridObject> grid;
        private int x;
        private int z;
        private PlacedObject placedObject;

        public GridObject(GridXZ<GridObject> grid, int x, int z)
        {
            this.grid = grid;
            this.x = x;
            this.z = z;
        }


        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            //grid.TriggerGridObejectChanged(x, z);
        }


        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }



        public void ClearPlacedObject()
        {
            placedObject = null;
            //grid.TriggerGridObejectChanged(x, z);
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }


        public override string ToString()
        {
            return x + ", " + z + "\n" + placedObject;
        }
    }

    public PlacedObject GetCurrentlyHeldPlacedObject()
    {
        return currentlyHeldPlacedObject;
    }
}
