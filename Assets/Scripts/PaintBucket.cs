using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintBucket : MonoBehaviour
{
    public Material paintMaterial;

    public void Start()
    {
        paintMaterial = transform.Find("Paint").GetComponent<MeshRenderer>().material;
    }
}
