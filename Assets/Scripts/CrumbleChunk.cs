using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumbleChunk : MonoBehaviour
{
    public float destroyTime = 1.5f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}

