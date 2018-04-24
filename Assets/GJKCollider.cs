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

    bool CollidesWithOther(GJKCollider other, out Vector3 closest_point, bool temp)
    {
        Vector3 min_norm = Vector3.zero;
        bool result = false;

        List<Vector3> convex_hull = new List<Vector3>();

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            min_norm = MinNorm(ref convex_hull);

            if(min_norm == Vector3.zero)
            {
                result = true;
                break;
            }

            Vector3 support_point = MinkowskiDiffSupport(other, -min_norm);

            //if the min norm is just as extreme as the support point
            //(ie, the vector from norm to support is perpendicular to the direction searched)
            //then there was no collision
            if (Vector3.Dot(support_point - min_norm, -min_norm) == 0)
            {
                break;
            }

            convex_hull.Add(support_point);
        }
        
        closest_point = min_norm;
        return result;
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

    //returns the closest point within the convex hull defined by convex_hull
    //and updates convex_hull to the smallest hull containing that point
    Vector3 MinNorm(ref List<Vector3> convex_hull)
    {
        if(convex_hull.Count == 1)
        {
            return convex_hull[0];
        }
        else if(convex_hull.Count == 2)
        {
            return MinNormLine(ref convex_hull);
        }
        else if (convex_hull.Count == 3)
        {
            return MinNormTri(ref convex_hull);
        }
        else if (convex_hull.Count == 4)
        {
            return MinNormTetra(ref convex_hull);
        }
        else
        {

        }
    }

    Vector3 MinNormLine(ref List<Vector3> convex_hull)
    {
        Vector3 A = convex_hull[0];
        Vector3 B = convex_hull[1];

        Vector3 AB = B - A;
        Vector3 BA = A - B;

        Vector3 AO = -A;
        Vector3 BO = -B;

        //in inner region
        if(Vector3.Dot(AO, AB) > 0 && Vector3.Dot(BO, BA) > 0)
        {
            return Vector3.Project(AO, AB);
        }

        if(AO.magnitude < BO.magnitude)//A is closer
        {
            convex_hull = new List<Vector3>
            {
                A
            };

            return A;
        }
        else //B is closer
        {
            convex_hull = new List<Vector3>
            {
                B
            };

            return B;
        }
    }

    Vector3 MinNormTri(ref List<Vector3> convex_hull)
    {
        Vector3 A = convex_hull[0];
        Vector3 B = convex_hull[1];
        Vector3 C = convex_hull[2];

        Vector3 AO = -A;
        Vector3 BO = -B;
        Vector3 CO = -C;

        Vector3 AB = B - A;
        Vector3 BC = B - C;
        Vector3 CA = C - A;

        Vector3 ABC = Vector3.Cross(AB, -CA);

        Vector3 ABP = Vector3.Cross(AB, ABC);
        Vector3 ACP = Vector3.Cross(AC, ABC);
        Vector3 BCP = Vector3.Cross(BC, ABC);

        //inner region of triangle
        if (Vector3.Dot(ABP, CO) < 0 && Vector3.Dot(ACP, BO) < 0 && Vector3.Dot(BCP, AO) < 0)
        {
            Vector3 p1 = Vector3.Project(AO, AB);
            Vector3 p2 = Vector3.Project(BO, BC);
        }
        
        //check each line
        if (Vector3.Dot(ACP, AO) > 0)
        {
            convex_hull = new List<Vector3>
            {
                A, B
            };
        }

        if (Vector3.Dot(ACP, AO) > 0)
        {
            convex_hull = new List<Vector3>
            {
                A, C
            };
        }

        if (Vector3.Dot(ACP, AO) > 0)
        {
            convex_hull = new List<Vector3>
            {
                B, C
            };
        }

        return MinNormLine(ref convex_hull);
    }

    Vector3 MinNormTetra(ref List<Vector3> convex_hull)
    {
        Vector3 A = convex_hull[0];
        Vector3 B = convex_hull[1];
        Vector3 C = convex_hull[2];
        Vector3 D = convex_hull[3];

        Vector3 AO = -A;
        Vector3 BO = -B;
        Vector3 CO = -C;
        Vector3 DO = -D;

        //check if the origin is inside the tetrahedron
        if(false)
        {


            return Vector3.zero;
        }

        //check which face region the origin is within
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

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction)
    {
        return support.Support(direction) - other.support.Support(-direction);
    }

    Vector3 Cross_ABA(Vector3 A, Vector3 B)
    {
        return Vector3.Cross( Vector3.Cross(A, B), A );
    }
}