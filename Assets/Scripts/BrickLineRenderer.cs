using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickLineRenderer : MonoBehaviour
{
    private LineRenderer anchorLineRenderer;
    private LineRenderer frontLeftLineRenderer;
    private LineRenderer backLeftLineRenderer;
    private LineRenderer backRightLineRenderer;

    private PlacedObject brickAttached;


    public float lineWidth = 0f;
    public Material lineMaterial;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!attachedToBrick())
            return;
    }




    public void setupRenderer(Material lineMaterial, float lineWidth)
    {
        GameObject anchorRendererObject = new GameObject("anchorRendererObject");
        GameObject frontLeftAnchorRendererObject = new GameObject("frontLeftAnchorRendererObject");
        GameObject backLeftAnchorRendererObject = new GameObject("backLeftAnchorRendererObject");
        GameObject backRightAnchorRendererObject = new GameObject("backRightAnchorRendererObject");

        // Set parent for GameObjects
        anchorRendererObject.transform.parent = transform;
        frontLeftAnchorRendererObject.transform.parent = transform;
        backLeftAnchorRendererObject.transform.parent = transform;
        backRightAnchorRendererObject.transform.parent = transform;

        this.lineMaterial = lineMaterial;
        this.lineWidth = lineWidth;

        anchorLineRenderer = anchorRendererObject.AddComponent<LineRenderer>();
        frontLeftLineRenderer = frontLeftAnchorRendererObject.AddComponent<LineRenderer>();
        backLeftLineRenderer = backLeftAnchorRendererObject.AddComponent<LineRenderer>();
        backRightLineRenderer = backRightAnchorRendererObject.AddComponent<LineRenderer>();

        anchorLineRenderer.startWidth = lineWidth;
        anchorLineRenderer.endWidth = lineWidth;
        anchorLineRenderer.material = lineMaterial;

        frontLeftLineRenderer.startWidth = lineWidth;
        frontLeftLineRenderer.endWidth = lineWidth;
        frontLeftLineRenderer.material = lineMaterial;

        backLeftLineRenderer.startWidth = lineWidth;
        backLeftLineRenderer.endWidth = lineWidth;
        backLeftLineRenderer.material = lineMaterial;

        backRightLineRenderer.startWidth = lineWidth;
        backRightLineRenderer.endWidth = lineWidth;
        backRightLineRenderer.material = lineMaterial;

        Deactivate();
    }



    public void drawAnchorLines(Vector3 hitPoint)
    {
        if (!attachedToBrick())
            return;

        Debug.Log("Painting Lines for: " + hitPoint);
        anchorLineRenderer.SetPosition(0, brickAttached.Anchor.transform.position);
        anchorLineRenderer.SetPosition(1, new Vector3(
            brickAttached.Anchor.transform.position.x,
            hitPoint.y,
            brickAttached.Anchor.transform.position.z));

        frontLeftLineRenderer.SetPosition(0, brickAttached.FrontLeftAnchor.transform.position);
        frontLeftLineRenderer.SetPosition(1, new Vector3(
            brickAttached.FrontLeftAnchor.transform.position.x,
            hitPoint.y,
            brickAttached.FrontLeftAnchor.transform.position.z));

        backLeftLineRenderer.SetPosition(0, brickAttached.BackLeftAnchor.transform.position);
        backLeftLineRenderer.SetPosition(1, new Vector3(
            brickAttached.BackLeftAnchor.transform.position.x,
            hitPoint.y,
            brickAttached.BackLeftAnchor.transform.position.z));

        backRightLineRenderer.SetPosition(0, brickAttached.BackRightAnchor.transform.position);
        backRightLineRenderer.SetPosition(1, new Vector3(
            brickAttached.BackRightAnchor.transform.position.x,
            hitPoint.y,
            brickAttached.BackRightAnchor.transform.position.z));
    }


    public void Activate()
    {
        anchorLineRenderer.enabled = true;
        frontLeftLineRenderer.enabled = true;
        backLeftLineRenderer.enabled = true;
        backRightLineRenderer.enabled = true;
    }




    public void Deactivate()
    {
        Debug.LogWarning("Renderers Deactivated!");
        anchorLineRenderer.enabled = false;
        frontLeftLineRenderer.enabled = false;
        backLeftLineRenderer.enabled = false;
        backRightLineRenderer.enabled = false;
    }




    public void attachToBrick(PlacedObject brick)
    {
        if (!brick)
            return;

        brickAttached = brick;
        brick.attachedBrickLineRenderer = this;
    }


    public void detachFromBrick()
    {
        brickAttached.attachedBrickLineRenderer = null;
        brickAttached = null;
    }


    public PlacedObject AttachedBrick()
    {
        return brickAttached;
    }


    public bool attachedToBrick()
    {
        return brickAttached != null;
    }
}
