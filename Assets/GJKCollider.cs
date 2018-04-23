using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISupport))]
public class GJKCollider : MonoBehaviour {

    private bool colliding = false;

    private ISupport support;

    private int MAX_ITERATIONS = 256;

    Dictionary<GJKCollider, Vector3> closest_points;

	// Use this for initialization
	void Start ()
    {
        support = GetComponent<ISupport>();

        closest_points = new Dictionary<GJKCollider, Vector3>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        GJKCollider[] colliders = FindObjectsOfType<GJKCollider>();

        colliding = false;
        foreach (GJKCollider c in colliders)
        {
            if(c == this)
            {
                continue;
            }

            if (!closest_points.ContainsKey(c))
            {
                closest_points.Add(c, c.gameObject.transform.position);
            }

            Vector3 closest_point;
            if (CollidesWithOther(c, out closest_point))
            {
                colliding = true;
            }

            closest_points[c] = closest_point;
        }
	}

    public bool Colliding
    {
        get
        {
            return colliding;
        }
    }

    public ISupport Support
    {
        get
        {
            return support;
        }
    }

    public Vector3 ClosestPointTo(GJKCollider other)
    {
        if(!closest_points.ContainsKey(other))
        {
            print("this dictionay does not contain the key");
            return this.gameObject.transform.position;
        }

        return closest_points[other];
    }

    bool CollidesWithOther(GJKCollider other, out Vector3 closest_point)
    {
        /*
        //star point in a arbitrary direction
        Vector3 start_point = MinkowskiDiffSupport(other, Vector3.right);

        //add that point to the simplex
        List<Vector3> simplex = new List<Vector3>
        {
            start_point
        };

        //search in the direction of that point to the origin
        Vector3 direction = -start_point;
        */
        
        Vector3 direction = Vector3.right;
        Vector3 C = MinkowskiDiffSupport(other, direction, out closest_point);
        if(Vector3.Dot(C, direction) < 0)
        {
            return false;
        }

        direction = -C;
        Vector3 B = MinkowskiDiffSupport(other, direction, out closest_point);
        if (Vector3.Dot(B, direction) < 0)
        {
            return false;
        }

        direction = Cross_ABA(C - B, -B);
        List<Vector3> simplex = new List<Vector3>
        {
            B, C
        };

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            Vector3 newest_point = MinkowskiDiffSupport(other, direction, out closest_point);

            if (Vector3.Dot(newest_point, direction) < 0)
            {
                return false;
            }

            if(DoSimplex(newest_point, ref simplex, ref direction))
            {
                return true;
            }
        }

        print("finished iters");
        return false;
    }

    /*
    Vector3 MinNormLine(List<Vector3> hull)
    {
        Vector3 A = hull[0];
        Vector3 B = hull[1];

        Vector3 AB = B - A;
        Vector3 AO = -A;
        Vector3 BO = -B;

        //in inner region
        if(Vector3.Dot(AO, AB) > 0 && Vector3.Dot(BO, AB) > 0)
        {

        }

        //A is closer
        if(AO.magnitude < BO.magnitude)
        {
            return A;
        }
        else //B is closer
        {
            return B;
        }
    }

    Vector3 MinNormTri(Vector3 point, List<Vector3> hull)
    {

    }

    Vector3 MinNormTetra(Vector3 point, List<Vector3> hull)
    {

    }
    */

    bool DoSimplex(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {
        if (simplex.Count == 1)//line
        {
            return DoSimplexLine(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 2)//triangle
        {
            return DoSimplexTri(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 3)//tetrahedron
        {
            return DoSimplexTetra(newest_point, ref simplex, ref direction);
        }
        else
        {
            print("simplex error");
            return false;
        }
    }

    bool DoSimplexLine(Vector3 A, ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 B = simplex[0];

        Vector3 AB = B - A;
        Vector3 AO = -A;

        /*
        if (Vector3.Dot(AB, AO) > 0)
        {
            //origin in region between A and B

            simplex = new List<Vector3>
            {
                A, B
            };
            
            direction = Cross_ABA(AB, AO);
        }
        else
        {
            //origin in region beyond A

            simplex = new List<Vector3>
            {
                A
            };

            direction = AO;
        }
        */

        simplex = new List<Vector3>
        {
            A, B
        };

        direction = Cross_ABA(AB, AO);

        return false;
    }

    bool DoSimplexTri(Vector3 A, ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 B = simplex[0];
        Vector3 C = simplex[1];

        Vector3 AO = -A;

        //Vector3 parallel to edges
        Vector3 AB = B - A;
        Vector3 AC = C - A;

        //triangle's normal vector
        Vector3 ABC = Vector3.Cross(AB, AC);

        //vector in trianlge's plane perpendicular to AB
        Vector3 ABP = Vector3.Cross(AB, ABC);
        //if the origin lies outside the trianlge edge AB
        if(Vector3.Dot(ABP, AO) > 0)
        {
            //the simplex is the line AB
            simplex = new List<Vector3>
            {
                A, B
            };

            //search in the direction perpenducilar to 
            direction = Cross_ABA(AB, AO);

            return false;
        }

        //vector in trianlge's plane perpendicular to AC
        Vector3 ACP = Vector3.Cross(ABC, AC);
        //if the origin lies outside the trianlge edge AB
        if (Vector3.Dot(ACP, AO) > 0)
        {
            //the simplex is the line AC
            simplex = new List<Vector3>
            {
                A, C
            };

            //search in the direction perpenducilar to 
            direction = Cross_ABA(AC, AO);

            return false;
        }

        //the point is within the trianlge either above or below

        if(Vector3.Dot(ABC, AO) > 0)
        {
            simplex = new List<Vector3>
            {
                A, B, C
            };

            direction = ABC;
        }
        else
        {
            simplex = new List<Vector3>
            {
                A, C, B
            };

            direction = -ABC;
        }

        return false;
    }

    bool DoSimplexTetra(Vector3 A, ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 B = simplex[0];
        Vector3 C = simplex[1];
        Vector3 D = simplex[2];

        Vector3 AO = -A;

        Vector3 AB = B - A;
        Vector3 AC = C - A;
        Vector3 AD = D - A;

        Vector3 ABC = Vector3.Cross(AB, AC);
        Vector3 ACD = Vector3.Cross(AC, AD);
        Vector3 ADB = Vector3.Cross(AD, AB);

        Vector3 tmp;

        const int over_abc = 0x1;
        const int over_acd = 0x2;
        const int over_adb = 0x4;

        int plane_tests =
            (Vector3.Dot(ABC, AO) > 0 ? over_abc : 0) |
            (Vector3.Dot(ACD, AO) > 0 ? over_acd : 0) |
            (Vector3.Dot(ADB, AO) > 0 ? over_adb : 0);

        switch(plane_tests)
        {
            case 0:
                //beind all three faces, thus inside tetrahedron
                return true;

            case over_abc:
                goto check_one_face;

            case over_acd:
                //rotate ACD into ABC
                B = C;
                C = D;

                AB = AC;
                AC = AD;

                ABC = ACD;

                goto check_one_face;

            case over_adb:
                //rotate ADB into ABC
                C = B;
                B = D;

                AC = AB;
                AB = AD;

                ABC = ADB;

                goto check_one_face;
                
            case over_abc | over_acd:
                goto check_two_faces;

            case over_acd | over_adb:
                //rotate ACD, ADB into ABC, ACD
                tmp = B;
                B = C;
                C = D;
                D = tmp;

                tmp = AB;
                AB = AC;
                AC = AD;
                AD = tmp;

                ABC = ACD;
                ACD = ADB;

                goto check_two_faces;

            case over_adb | over_abc:
                //rotate ADB, ABC into ABC, ACD
                tmp = C;
                C = B;
                B = D;
                D = tmp;

                tmp = AC;
                AC = AB;
                AB = AD;
                AD = tmp;

                ACD = ABC;
                ABC = ADB;

                goto check_two_faces;

            default:
                return true;
        }

        check_one_face:

        if(Vector3.Dot( Vector3.Cross(ABC, AC), AO ) > 0)
        {
            //in region of AC
            simplex = new List<Vector3>
            {
                A, C
            };

            direction = Cross_ABA(AC, AO);

            return false;
        }

        check_one_face_part_2:

        if(Vector3.Dot( Vector3.Cross(AB, ABC), AO ) > 0)
        {
            //in region of edge AB
            simplex = new List<Vector3>
            {
                A, B
            };

            direction = Cross_ABA(AB, AO);

            return false;
        }

        //in region of ABC
        simplex = new List<Vector3>
        {
            A, B, C
        };

        direction = ABC;

        return false;

        check_two_faces:

        if(Vector3.Dot( Vector3.Cross(ABC, AC), AO) > 0)
        {
            //origin is beyond AC,
            //So we consider ACD
            B = C;
            C = D;

            AB = AC;
            AC = AD;

            ABC = ACD;

            goto check_one_face;
        }
        
        //either over ABC or AB

        goto check_one_face_part_2;
    }

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction, out Vector3 temp_closest)
    {
        temp_closest = support.Support(direction);
        return temp_closest - other.support.Support(-direction);
    }

    Vector3 Cross_ABA(Vector3 A, Vector3 B)
    {
        return Vector3.Cross( Vector3.Cross(A, B), A );
    }
}