using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GJKCollider))]
public class CollidingColor : MonoBehaviour {

    public Color colliding_color;
    public Color not_colliding_color;

    private GJKCollider gjk;
    private Renderer rend;

	// Use this for initialization
	void Start ()
    {
        gjk = GetComponent<GJKCollider>();
        rend = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		
        if(gjk.Colliding)
        {
            rend.material.color = colliding_color;
        }
        else
        {
            rend.material.color = not_colliding_color;
        }
	}
}
