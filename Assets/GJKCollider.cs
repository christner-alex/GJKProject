using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISupport))]
public class GJKCollider : MonoBehaviour {

    private bool colliding = false;

    private ISupport support;

	// Use this for initialization
	void Start ()
    {
        support = GetComponent<ISupport>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        GJKCollider[] colliders = FindObjectsOfType<GJKCollider>();

        //print(colliders.Length);

        colliding = false;
        foreach (GJKCollider c in colliders)
        {
            if(c == this)
            {
                continue;
            }

            if(CollidesWithOther(c))
            {
                colliding = true;
                print("colliding");
            }
            else
            {
                print("not colliding");
            }
            print("blah");
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

    bool CollidesWithOther(GJKCollider other)
    {
        //star point in a arbitrary direction
        Vector3 start_point = MinkowskiDiffSupport(other, -transform.position);

        //add that point to the simplex
        List<Vector3> simplex = new List<Vector3>
        {
            start_point
        };

        //search in the direction of that point to the origin
        Vector3 direction = -start_point;

        while (true)
        {
            Vector3 newest_point = MinkowskiDiffSupport(other, direction);

            if (Vector3.Dot(newest_point, direction) < 0)
            {
                return false;
            }

            if(DoSimplex(newest_point, ref simplex, ref direction))
            {
                return true;
            }
        }
    }

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
        
        if (Vector3.Dot(AB, AO) > 0)
        {
            simplex = new List<Vector3>
            {
                A, B
            };
            
            direction = Cross_ABA(AB, AO);
        }
        else
        {
            simplex = new List<Vector3>
            {
                A
            };

            direction = AO;
        }

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
            direction = Vector3.Cross(AB, AO);

            return false;
        }

        //vector in trianlge's plane perpendicular to AC
        Vector3 ACP = Vector3.Cross(AC, ABC);
        //if the origin lies outside the trianlge edge AB
        if (Vector3.Dot(ACP, AO) > 0)
        {
            //the simplex is the line AC
            simplex = new List<Vector3>
            {
                A, C
            };

            //search in the direction perpenducilar to 
            direction = Vector3.Cross(AC, AO);

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
                C = D;
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

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction)
    {
        return other.support.Support(direction) - support.Support(-direction);
    }

    Vector3 Cross_ABA( Vector3 A, Vector3 B )
    {
        return Vector3.Cross( Vector3.Cross(A, B), A );
    }
}