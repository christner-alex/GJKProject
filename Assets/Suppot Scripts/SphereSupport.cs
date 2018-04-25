using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereSupport : MonoBehaviour, ISupport {

    public float radius;

    public Vector3 Support(Vector3 direction)
    {
        Vector3 center = transform.position;

        return center + direction.normalized * radius;
    }
}
