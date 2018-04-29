using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosestPointDrawer : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        GJKCollider[] colliders = FindObjectsOfType<GJKCollider>();

        for(int i=0; i<colliders.Length; i++)
        {
            for(int j= i + 1; j<colliders.Length; j++)
            {
                GJKCollider c1 = colliders[i];
                GJKCollider c2 = colliders[j];

                //Vector3 p1 = c1.ClosestPointTo(c2);
                //Vector3 p2 = c2.ClosestPointTo(c1);

                //Debug.DrawLine(p1, p2);
            }
        }

	}
}
