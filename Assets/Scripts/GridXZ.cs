using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUtilities;

public class GridXZ<TGridObject>
{
    public Color debugLineColor = Color.white;

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;


    [SerializeField] private TGridObject[,] gridArray;
    private TextMesh[,] debugTextArray;


    public EventHandler<OnGridValueChangedEvenArgs> OnGridValueChanged;
    public class OnGridValueChangedEvenArgs : EventArgs
    {
        public int x;
        public int z;
    }





    public GridXZ(
        int width, 
        int height, 
        float cellSize, 
        Vector3 originPosition, 
        Func<GridXZ<TGridObject>, int, int, TGridObject>  createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        // Fill the grid with empty grid objects
        for(int x = 0; x < gridArray.GetLength(0); x++)
        {
            for(int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }
    }



    /*
     *  Draws a visualisation of the grid
     */
    public void drawGridLines()
    {
        Debug.DrawLine(GetWorldPosition(0, 0), GetWorldPosition(0, 0) + new Vector3(0, 1, 0), Color.red, 100f);
        debugTextArray = new TextMesh[width, height];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), debugLineColor, 100f);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), debugLineColor, 100f);
            }
        }

        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), debugLineColor, 100f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), debugLineColor, 100f);

        OnGridValueChanged += (object sender, OnGridValueChangedEvenArgs evenArgs) =>
        {
                debugTextArray[evenArgs.x, evenArgs.z].text = gridArray[evenArgs.x, evenArgs.z]?.ToString();
        };
    }



    public int GetWidth()
    {
        return width;
    }


    public int GetHeight()
    {
        return height;
    }


    public float GetCellSize()
    {
        return cellSize;
    }


    public Vector3 GetOriginPosition()
    {
        return originPosition;
    }


    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }


    /*
     *  Returns the world position of a grid object depending on its given 
     *  grid position and its scriptable object type
     */
    public Vector3 GetWorldPosition(int x, int z, PlacedObjectTypeSO.Dir dir)
    {
        // Offset the origin of the brick by the error caused by the given rotation
        switch(dir)
        {
            default:
            case PlacedObjectTypeSO.Dir.Down:
                return new Vector3(x, 0, z) * cellSize + originPosition;
            case PlacedObjectTypeSO.Dir.Up:
                return new Vector3(x + 1, 0, z + 1) * cellSize + originPosition;
            case PlacedObjectTypeSO.Dir.Left:
                return new Vector3(x, 0, z + 1) * cellSize + originPosition;
            case PlacedObjectTypeSO.Dir.Right:
                return new Vector3(x + 1, 0, z) * cellSize + originPosition;

        }
    }



    /*
     *  Returns the coordinates rounded down within the grid for a given world position
     */
    public void GetXZFloor(Vector3 worldPosition, out int x, out int z)
    { 
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
        //Debug.Log("X: " + x + " Z: " + z);
    }


    /*
     *  Returns the nearest coordinates within the grid for a given world position
     */
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.RoundToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.RoundToInt((worldPosition - originPosition).z / cellSize);
        Debug.Log("X: " + (worldPosition - originPosition).x / cellSize + " Z: " + (worldPosition - originPosition).z / cellSize);
        Debug.Log("X rounded: " + x + " Z rounded: " + z);
    }



    /*
     *  Sets the object at the given world postion to the given value
     */
    public void SetGridObject(Vector3 worldPosition, TGridObject value)
    {
        int x, z;

        GetXZ(worldPosition, out x, out z);
        SetGridObject(x, z, value);
    }




    /*
     *  Sets the object at the given grid postion to the given value
     */
    public void SetGridObject(int x, int z, TGridObject value)
    {
        if(x >= 0 && x >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
            if (OnGridValueChanged != null)
            {
                OnGridValueChanged(this, new OnGridValueChangedEvenArgs { x = x, z = z });
            }
        }
    }




    /*
     *  Triggers the OnGridValueChanged event
     */
    public void TriggerGridObejectChanged(int x, int z)
    {
        if (OnGridValueChanged != null)
        {
            OnGridValueChanged(this, new OnGridValueChangedEvenArgs { x = x, z = z });
        }
    }



    /*
     *  Returns the grid object at the given grid coordinates
     */
    public TGridObject GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            return default(TGridObject);
        }
    }



    /*
     *  Returns the grid object located at the given world postion
     */
    public TGridObject GetGridObject(Vector3 worldPosition)
    {
        int x, z;

        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }

   
}
