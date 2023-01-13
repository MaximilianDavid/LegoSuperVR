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

    public GameObject playerGameObject;
    [HideInInspector] public GameObject brickParent;
    [HideInInspector] public bool playerShrunk = false;
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;


    public List<ShrinkingPlayerHandler> shrinkingPlayerHandlers;

    [SerializeField] private float maximumAngleCorrection;
    [SerializeField] private Ghost ghost;
    [SerializeField] private List<PlacedObject> bricks;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOVGhosts;
    [SerializeField] private Material deleteGhostMaterial;
    [SerializeField] public Material previewGhostMaterial;
    [SerializeField] public Transform parentTransform;
    public GameObject leftHand;
    public GameObject rightHand;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private Renderer buildManualScreen;
    public List<InstructionPage> buildManualPages;
    [SerializeField] private CircularDrive circularDrive;

    public PaintBrush paintBrush;


    private int currentBuildManualPage = 0;


    

    private LineRenderer anchorLineRenderer;
    private LineRenderer frontLeftLineRenderer;
    private LineRenderer backLeftLineRenderer;
    private LineRenderer backRightLineRenderer;

    private List<BrickLineRenderer> brickLineRenderers = new List<BrickLineRenderer>();
    private List<Ghost> ghosts = new List<Ghost>();

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
    [SerializeField] public float scale = 1f;

    [SerializeField] public float currentGlobalRotation = 0f;

    [SerializeField] private Vector3 plateOrigin;
    [SerializeField] public Vector3 plateCenter;



    [SerializeField] private float maximumSnapDistance = 9.6f * 1.5f;


    [SerializeField] private float previewLineWidth = 0.001f;



    public float controleCooldown;

    private float lastControlUsage = default;





    private List<GridXZ<GridObject>> grids;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;

   


    public event EventHandler OnSelectedBrickChanged;












    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        brickParent = GameObject.Find("Bricks");

        // Setup grid system variables
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


        initialPlayerPosition = playerGameObject.transform.position;
        initialPlayerRotation = playerGameObject.transform.rotation;


        lastControlUsage = Time.time;

        // Setup LineRenderers for Bricks
        for (int i = 0; i < 2; i++)
        {
            GameObject brickLineRendererObject = new GameObject("BrickLineRenderer" + i);
            brickLineRenderers.Add(brickLineRendererObject.AddComponent<BrickLineRenderer>());
        }
        foreach (BrickLineRenderer brickLineRenderer in brickLineRenderers)
        {
            brickLineRenderer.setupRenderer(previewGhostMaterial, previewLineWidth);
        }


        // Setup Ghosts for Bricks
        for(int i = 0; i < 2; i++)
        {
            GameObject ghost = new GameObject("Ghost" + i);
            ghosts.Add(ghost.AddComponent<Ghost>());
        }
        foreach(Ghost ghost in ghosts)
        {
            // Setup ghost values
            ghost.SetupGhost(previewGhostMaterial, new Vector3(scale, scale, scale));
        }

   


        




        loadInstructionPages();




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
    }



















    /*
     *  Used to release a held Brick
     *  
     *  Snaps the brick to the grid, if it is currently near a valid grid position
     */
    public void releaseBrick(PlacedObject placedObject)
    {
        Transform brickTransform = placedObject.transform;
        PlacedObject heldBrick = placedObject;


        // Detach LBrickLineRenderers
        placedObject.attachedBrickLineRenderer.Deactivate();
        placedObject.attachedBrickLineRenderer.detachFromBrick();


        // Unassign ghost
        placedObject.assignedGhost.Deactivate();
        placedObject.assignedGhost.UnassignFromBrick();


        // Turn on Collisions again
        heldBrick.ignoreCollisions(false);



        SnapPoint snapPoint;


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

            Debug.Log("Calculating Snap Point!");
            snapPoint = GetSnapPoint(heldBrick);
            Debug.Log("Snap Point Calculated! " + snapPoint);

            int gridNumberForBuild = snapPoint.gridNumber;
            Debug.Log("Grid number: " + gridNumberForBuild);


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
        placedObject.makeKinematic();
        placedObject.pickUp();




        // Attach free line renderer to brick
        if (!placedObject.attachedBrickLineRenderer)
            foreach (BrickLineRenderer brickLineRenderer in brickLineRenderers)
            {
                if (!brickLineRenderer.attachedToBrick())
                {
                    brickLineRenderer.attachToBrick(placedObject);
                    //Debug.LogWarning("Attached BrickLineRenderer " + brickLineRenderer);
                    break;
                }
            }
        

        // Assign free ghost to brick
        if(!placedObject.assignedGhost)
            foreach (Ghost ghost in ghosts)
            {
                if(!ghost.IsAssignedToBrick())
                {
                    ghost.AssignToBrick(placedObject);
                    break;
                }
            }

        // Update ghosts Visual
        placedObject.assignedGhost.RefreshVisual();

        placedObject.ignoreCollisions();


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


        placedObject.OccupiedGridPositions.Clear();
        
    }















    
















    private void LateUpdate()
    {
        
    }








    private void Update()
    {
        // Update Plate Rotation
        UpdateRotation();


        // Check if enough time passed since last control use
        float pressedControlTime = Time.time;
        if (pressedControlTime < lastControlUsage)
            return;

        if(rightDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Advance manual page
            TurnManualPageForward();
            lastControlUsage = pressedControlTime + controleCooldown;
        }
        else if(leftDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Go back 1 manual page
            TurnManualPageBackward();
            lastControlUsage = pressedControlTime + controleCooldown;
        }
        else if(downDirectionAction.GetStateDown(SteamVR_Input_Sources.Any))
        {
            // Check if player is shrunk
            if (playerShrunk)
            {
                unshrinkPlayer();
                enableHands();
            }
            else
            {
                // Cleanup Scene
                CleanupBricks();
                cleanupShrinkingPlayerHandlers();
                paintBrush.revertPosition();
            }
            lastControlUsage = pressedControlTime + controleCooldown;
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
     *  Adds the given object ot the list of existing bricks
     */
    public void addToBrickList(PlacedObject brick)
    {
        bricks.Add(brick);
    }




    /*
     *  Returns player to his original scale and position
     */
    public void unshrinkPlayer()
    {
        playerGameObject.transform.localScale = Vector3.one;
        playerGameObject.transform.position = initialPlayerPosition;
        playerGameObject.transform.rotation = initialPlayerRotation;

        setPlayerShrunk(false);
    }







    /*
     *  Disables the hands
     */
    public void disableHands()
    {
        if (leftHand)
            leftHand.SetActive(false);

        if (rightHand)
            rightHand.SetActive(false);
    }





    /*
     *  Enables the hands
     */
    public void enableHands()
    {
        if (leftHand)
            leftHand.SetActive(true);

        if (rightHand)
            rightHand.SetActive(true);
    }





    


    /*
     *  Loads the manual pages from Resources
     */
    private void loadInstructionPages()
    {
        for(int i = 1; i <= 7; i++)
        {
            buildManualPages.Add(Resources.Load<InstructionPage>("InstructionPages/ControlScreen " + i));
        }
        
        for(int i = 1; i <= 22; i++)
        {
            buildManualPages.Add(Resources.Load<InstructionPage>("InstructionPages/Screen" + i));
        }
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
            // Advance Page
            currentBuildManualPage++;
            
            CleanupBricks();

            // Change build manual material
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage].screenMaterial;
            buildManualScreen.materials = screenMats;

            SpawnBricksForInstructionPage(buildManualPages[currentBuildManualPage]);
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
            // Reduce Page number
            currentBuildManualPage--;

            CleanupBricks();

            // Change screen material
            Material[] screenMats = buildManualScreen.materials;
            screenMats[1] = buildManualPages[currentBuildManualPage].screenMaterial;
            buildManualScreen.materials = screenMats;


            SpawnBricksForInstructionPage(buildManualPages[currentBuildManualPage]);
        }
    }





    /*
     *  Removes all bricks that are not currently inside the grid, picked up
     *  or spawners.
     *  
     *  Also resets every spawner's position.
     */
    private void CleanupBricks()
    {
        List<PlacedObject> bricksToRemove = new List<PlacedObject>();
        foreach(PlacedObject brick in bricks)
        {
            // Destroy all bricks that are lying around
            if (!brick.IsPlacedInGrid && !brick.isPickedUp() && !brick.neverPickedUp)
            {
                // Remove Brick from brick list & Destroy
                bricksToRemove.Add(brick);
                brick.DestroySelf();
            }
            else if(brick.neverPickedUp)
            {
                // Restore position of all spawner bricks
                brick.RevertToStartingPosition();
            }
        }

        foreach(PlacedObject brickToRemove in bricksToRemove)
        {
            bricks.Remove(brickToRemove);
        }
    }










    /*
     *  Spawns all bricks for the given InstructionPage next to the build plate
     */
    public void SpawnBricksForInstructionPage(InstructionPage page)
    {
        // Set spawn position for first brick
        Vector3 spawnPosition = plateOrigin;
        spawnPosition += new Vector3(0, 0, (gridWidth + 1) * cellSize);


        int numSpawned = 0;
        int longestBrickLength = 0;
        foreach(PlacedObject brick in page.bricks)
        {
            // Instantiate brick
            GameObject spawnedBrick = Instantiate(brick.gameObject, spawnPosition, Quaternion.identity);
            spawnedBrick.transform.localScale = new Vector3(scale, scale, scale);
            PlacedObject spawnedPlacedObject = spawnedBrick.GetComponent<PlacedObject>();

            // Change material
            spawnedPlacedObject.changeBrickMaterial(page.brickMaterials[numSpawned]);
            numSpawned++;

            // Register brick for deletion upn cleanup
            spawnedPlacedObject.neverPickedUp = false;

            // Register brick within system
            addToBrickList(spawnedPlacedObject);


            // Parent instantiated brick to Bricks GameObject
            spawnedBrick.transform.parent = brickParent.transform;

            // Check if new brick is longer than longest brick yet
            if (longestBrickLength < spawnedPlacedObject.placedObjectTypeSO.height)
                longestBrickLength = spawnedPlacedObject.placedObjectTypeSO.height;

            // Advance Spawn position
            if(numSpawned % 3 != 0) // 3 bricks in a column
            {
                // Shift in x direction
                spawnPosition += new Vector3(cellSize, 0, 0); // Space between bricks
                spawnPosition += new Vector3(cellSize * spawnedPlacedObject.placedObjectTypeSO.width, 0, 0);
            }
            else
            {
                // Reset x position down to plate base line
                spawnPosition.x = plateOrigin.x;

                // Shift in z direction by size of longest brick
                spawnPosition.z += cellSize;
                spawnPosition.z += cellSize * longestBrickLength;

                // Reset longest brick
                longestBrickLength = 0;
            }
        }
    }









    /*
     *  Returns all ShrinkingPlayerHandlers to their initial position
     */
    public void cleanupShrinkingPlayerHandlers()
    {
        foreach(ShrinkingPlayerHandler handler in shrinkingPlayerHandlers)
        {
            handler.resetPosition();
        }
    }








    /*
     *  Sets the playerShrunk field to the given Value
     */
    public void setPlayerShrunk(bool shrunk = true)
    {
        playerShrunk = shrunk;
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
