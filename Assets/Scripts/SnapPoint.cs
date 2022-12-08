using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 *  A data structure representing a snappoint for a brick
 *  with all necessary information such as:
 *  
 *  Number of the corresponding grid
 *  Location of Brick's main anchor in world coordinates
 *  Location of Brick's main anchor in grid coordinates
 */
public class SnapPoint
{
    public int gridNumber { get; set; }
    public Vector3 worldLocation { get; set; }
    public Vector2Int gridLocation { get; set; }


    public SnapPoint(
        int gridNumber = 0, 
        Vector3 worldLocation = default, 
        Vector2Int gridLocation = default)
    {
        this.gridNumber = gridNumber;
        this.worldLocation = worldLocation;
        this.gridLocation = gridLocation;
    }

    public SnapPoint(SnapPoint other)
    {
        this.gridNumber = other.gridNumber;
        this.worldLocation = other.worldLocation;
        this.gridLocation = other.gridLocation;
    }

    public int GridX()
    {
        return gridLocation.x;
    }


    public int GridZ()
    {
        return gridLocation.y;
    }
}
