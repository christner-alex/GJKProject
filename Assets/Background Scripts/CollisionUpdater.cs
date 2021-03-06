﻿using System.Collections;
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

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.GetComponent<Renderer>().material.color = not_colliding_color;
        }

        for(int i = 0; i < colliders.Length; i++)
        {
            for (int j = i + 1; j < colliders.Length; j++)
            {
                bool collides = colliders[i].CollidesWithOther(colliders[j]);

                if(collides)
                {
                    colliders[i].gameObject.GetComponent<Renderer>().material.color = colliding_color;
                    colliders[j].gameObject.GetComponent<Renderer>().material.color = colliding_color;
                }
            }
        }
    }
}
