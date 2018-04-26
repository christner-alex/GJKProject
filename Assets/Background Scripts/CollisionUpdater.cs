using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionUpdater : MonoBehaviour {

    public Color colliding_color;
    public Color not_colliding_color;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        GJKCollider[] colliders = FindObjectsOfType<GJKCollider>();
        bool[] colliding = new bool[colliders.Length];
        for(int i = 0; i < colliding.Length; i++)
        {
            colliding[i] = false;
        }

        for(int i = 0; i < colliders.Length; i++)
        {
            for (int j = i + 1; j < colliders.Length; j++)
            {
                Vector3 i_support;
                Vector3 j_support;
                bool collides = colliders[i].CollidesWithOther(colliders[j], out i_support, out j_support);
                
                if (collides)
                {
                    colliding[i] = true;
                    colliding[j] = true;
                }

                Debug.DrawLine(i_support, j_support);
            }
        }

        for (int i = 0; i < colliding.Length; i++)
        {
            Renderer[] rends = colliders[i].gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rends)
            {
                if (colliding[i])
                {
                    r.material.color = colliding_color;

                }
                else
                {
                    r.material.color = not_colliding_color;
                }
            }
        }

        //int test;
        //OutTest(out test);
        //print(test);
    }
    /*
    void OutTest(out int test)
    {
        out1(out test);
        out2(out test);
    }

    void out1(out int test)
    {
        test = 1;
    }

    void out2(out int test)
    {
        test = 2;
    }
    */
}
