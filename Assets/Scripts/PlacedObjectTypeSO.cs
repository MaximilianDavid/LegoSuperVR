using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PlacedObjectTypeSO : ScriptableObject {


    /*
     *  Directions the bricks can be in
     */
    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }





    public string nameString;

    public Transform prefab;
    public Transform visual;
    public Transform ghostVisual;

    public int width;
    public int height;





    /*
     *  Returns the next rotation depending upon the given rotation
     */
    public static Dir GetNextDir(Dir dir) {
        switch (dir) {
            default:
            case Dir.Down:      return Dir.Left;
            case Dir.Left:      return Dir.Up;
            case Dir.Up:        return Dir.Right;
            case Dir.Right:     return Dir.Down;
        }
    }



    /*
     *  Returns the previous rotation depending upon the given rotation
     */
    public static Dir GetPreviousDir(Dir dir)
    {
        switch(dir)
        {
            default:
            case Dir.Down:      return Dir.Right;
            case Dir.Right:     return Dir.Up;
            case Dir.Up:        return Dir.Left;
            case Dir.Left:      return Dir.Down;
        }
    }


   


    /*
     *  Returns the corresponding angle in degrees for the given direction
     */
    public int GetRotationAngle(Dir dir) {
        switch (dir) {
            default:
            case Dir.Down:  return 0;
            case Dir.Left:  return 90;
            case Dir.Up:    return 180;
            case Dir.Right: return 270;
        }
    }



    /*
     *  DEPRICATED!!!!
     *  
     *  Returns the offset caused by the bricks' current roation
     */
    public Vector2Int GetRotationOffset(Dir dir) {
        return new Vector2Int(0,0);
        switch (dir) {
            default:
            case Dir.Down:  return new Vector2Int(0, 0);
            case Dir.Left:  return new Vector2Int(0, width);
            case Dir.Up:    return new Vector2Int(width, height);
            case Dir.Right: return new Vector2Int(height, 0);
        }
    }




    /*
     *  Returns a list of all grid positions the brick occupies / will occupy based on the given
     *  grid position and direction
     */
    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Dir dir) {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
        switch (dir) {
            default:
            case Dir.Down:
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, z));
                    }
                }
                break;
            case Dir.Up:
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        gridPositionList.Add(new Vector2Int(offset.x - x, offset.y - z));
                    }
                }
                break;
            case Dir.Left:
                for (int x = 0; x < height; x++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        gridPositionList.Add(new Vector2Int(offset.x + x, offset.y - z));
                    }
                }
                break;
            case Dir.Right:
                for (int x = 0; x < height; x++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        gridPositionList.Add(new Vector2Int(offset.x - x, offset.y + z));
                    }
                }
                break;

        }
        return gridPositionList;
    }

}
