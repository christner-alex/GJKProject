using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GJKCollider : MonoBehaviour {

    public List<Vector3> verticies;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    bool CollidesWithOther(GameObject other)
    {
        Vector3 start_point = MinkowskiSupport(other, -transform.position);

        List<Vector3> simplex = new List<Vector3>
        {
            start_point
        };

        Vector3 direction = -start_point;

        while (true)
        {
            Vector3 newest_point = MinkowskiSupport(other, direction);

            if (Vector3.Dot(newest_point, direction) < 0)
            {
                return false;
            }

            simplex.Add(newest_point);

            if(DoSimplex(newest_point, ref simplex, ref direction))
            {
                return true;
            }
        }
    }

    bool DoSimplex(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {
        if (simplex.Count == 2)//line
        {
            return DoSimplexLine(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 3)//triangle
        {
            return DoSimplexTri(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 4)//tetrahedron
        {
            return DoSimplexTetra(newest_point, ref simplex, ref direction);
        }
        else
        {
            return false;
        }
    }

    bool DoSimplexLine(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 A = newest_point;
        simplex.Remove(newest_point);
        Vector3 B = simplex[0];
        simplex.Remove(B);

        Vector3 AB = B - A;
        Vector3 AO = -A;
        Vector3 BO = -B;

        if (Vector3.Dot(AB, AO) > 0)
        {
            simplex.Add(A);
            simplex.Add(B);

            direction = Vector3.Cross(AB, AO);
            direction = Vector3.Cross(direction, AB);
        }
        else
        {
            simplex.Add(A);
            direction = AO;
        }

        return false;
    }

    bool DoSimplexTri(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {


        return false;
    }

    bool DoSimplexTetra(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {

    }

    Vector3 MinkowskiSupport(GameObject other, Vector3 direction)
    {
        return other.GetComponent<GJKCollider>().Support(direction) - Support(direction);
    }

    Vector3 Support(Vector3 direction)
    {
        List<Vector3> world_vertices = new List<Vector3>();
        foreach(Vector3 vert in verticies)
        {
            world_vertices.Add(transform.position + vert);
        }
    }
}
