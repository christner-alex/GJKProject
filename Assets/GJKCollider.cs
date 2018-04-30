using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISupport))]
public class GJKCollider : MonoBehaviour {

    //private bool colliding = false;

    private ISupport support;

    private int MAX_ITERATIONS = 256;

    //Dictionary<GJKCollider, Vector3> closest_points;

	// Use this for initialization
	void Start ()
    {
        support = GetComponent<ISupport>();

        //closest_points = new Dictionary<GJKCollider, Vector3>();
    }
	
	// Update is called once per frame
    /*
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

            Vector3 my_support;
            Vector3 other_support;
            if (CollidesWithOther(c, out my_support, out other_support))
            {
                colliding = true;
            }

            closest_points[c] = my_support;
        }
	}
    */

    /*
    public bool Colliding
    {
        get
        {
            return colliding;
        }
    }
    */

    /*
    public Vector3 Support(Vector3 direction)
    {
        return support.Support(direction);
    }
    */

    /*
    public Vector3 ClosestPointTo(GJKCollider other)
    {
        if(!closest_points.ContainsKey(other))
        {
            print("this dictionay does not contain the key");
            return this.gameObject.transform.position;
        }

        return closest_points[other];
    }
    */

    public bool CollidesWithOther(GJKCollider other, out Vector3 my_closest, out Vector3 other_closest)
    {
        bool result = false;
        Vector3 newest_point;
        Vector3 my_support;
        Vector3 other_support;
        Dictionary<Vector3, MinkowskiDiffPair> MinkowskiDiffPairs = new Dictionary<Vector3, MinkowskiDiffPair>();

        /*
        //start point in a arbitrary direction
        newest_point = MinkowskiDiffSupport(other, Vector3.right, out my_support, out other_support);
        if(!MinkowskiDiffPairs.ContainsKey(newest_point))
        {
            MinkowskiDiffPairs.Add(newest_point, new MinkowskiDiffPair(my_support, other_support));
        }

        //add that point to the simplex
        List<Vector3> simplex = new List<Vector3>
        {
            newest_point
        };

        //search in the direction of that point to the origin
        Vector3 direction = -newest_point;
        */
        
        Vector3 direction = Vector3.right;
        Vector3 C = MinkowskiDiffSupport(other, direction, out my_support, out other_support, MinkowskiDiffPairs);
        /*
        if(Vector3.Dot(C, direction) < 0)
        {
            print("check 1");
            //return false;
            goto finish;
        }
        */

        direction = -C.normalized;
        Vector3 B = MinkowskiDiffSupport(other, direction, out my_support, out other_support, MinkowskiDiffPairs);
        /*
        if (Vector3.Dot(B, direction) < 0)
        {
            print("check 2");
            //return false;
            goto finish;
        }
        */

        direction = Cross_ABA(C - B, -B).normalized;
        List<Vector3> simplex = new List<Vector3>
        {
            B, C
        };

        /*
        Vector3 A = MinkowskiDiffSupport(other, direction, out my_support, out other_support, MinkowskiDiffPairs);
        if (Vector3.Dot(Vector3.Cross(B-A,C-A), -A) > 0)
        {
            simplex = new List<Vector3>
            {
                A, B, C
            };

            direction = Vector3.Cross(B - A, C - A);
        }
        else
        {
            simplex = new List<Vector3>
            {
                A, C, B
            };

            direction = -Vector3.Cross(B - A, C - A);
        }
        */

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            newest_point = MinkowskiDiffSupport(other, direction, out my_support, out other_support, MinkowskiDiffPairs);

            if (Vector3.Dot(newest_point, direction) < 0)
            {
                print("iteration " + i);
                //simplex = Simplex_Push_Front(newest_point, simplex);
                //return false;
                break;
            }

            if(DoSimplex(newest_point, ref simplex, ref direction))
            {
                print("iteration " + i);
                result = true;
                //return true;
                break;
            }

        }

        //print("finished iters");
        //return false;

        //finish:

        CalculateClosestPoints(simplex, MinkowskiDiffPairs, out my_closest, out other_closest);
        //my_closest = Vector3.zero;
        //other_closest = Vector3.zero;

        /*
        if(result)
        {
            print("distance = 0");
        }
        else
        {
            print("distance = " + newest_point.magnitude);
        }
        */

        return result;
    }
    
    bool DoSimplex(Vector3 newest_point, ref List<Vector3> simplex, ref Vector3 direction)
    {
        bool result = false;
        if (simplex.Count == 1)//line
        {
            result = DoSimplexLine(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 2)//triangle
        {
            result = DoSimplexTri(newest_point, ref simplex, ref direction);
        }
        else if (simplex.Count == 3)//tetrahedron
        {
            result = DoSimplexTetra(newest_point, ref simplex, ref direction);
        }
        else
        {
            print("simplex error. Count="+simplex.Count);
            result = false;
        }
        direction.Normalize();
        return result;
    }

    bool DoSimplexLine(Vector3 A, ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 B = simplex[0];

        Vector3 AB = B - A;
        Vector3 AO = -A;

        
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
        
        /*
        simplex = new List<Vector3>
        {
            A, B
        };

        direction = Cross_ABA(AB, AO);
        */

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

            //search in the direction perpenducilar to AB
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

            //search in the direction perpenducilar to AC
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

    void CalculateClosestPoints(List<Vector3> simplex, Dictionary<Vector3, MinkowskiDiffPair> MDP, out Vector3 my_closest, out Vector3 other_closest)
    {
        if (simplex.Count == 1)//point is closest
        {
            print("one simplex closest case");
            my_closest = MDP[simplex[0]].MyPoint;
            other_closest = MDP[simplex[0]].OtherPoint;
        }
        else if (simplex.Count == 2)//triangle
        {
            CalculateClosestPointsLine(simplex, MDP, out my_closest, out other_closest);
        }
        else if (simplex.Count == 3)//tetrahedron
        {
            CalculateClosestPointsTri(simplex, MDP, out my_closest, out other_closest);
        }
        else
        {
            print("closest point simplex error. Count=" + simplex.Count);

            my_closest = Vector3.zero;
            other_closest = Vector3.zero;
        }
    }
    
    void CalculateClosestPointsLine(List<Vector3> simplex, Dictionary<Vector3, MinkowskiDiffPair> MDP, out Vector3 my_closest, out Vector3 other_closest)
    {
        Vector3 A = simplex[0];
        Vector3 B = simplex[1];

        if(A == B)
        {
            print("two simplex closest case: smae points");
            my_closest = A;
            other_closest = A;

            return;
        }

        print("two simplex closest case: not same point");

        Vector3 L = B - A;

        float lambda2 = Vector3.Dot(-L, A) / Vector3.Dot(L, L);
        float lambda1 = 1 - lambda2;

        Vector3 my_A = MDP[A].MyPoint;
        Vector3 other_A = MDP[A].OtherPoint;
        Vector3 my_B = MDP[B].MyPoint;
        Vector3 other_B = MDP[B].OtherPoint;

        my_closest = lambda1 * my_A + lambda2 * my_B;
        other_closest = lambda1 * other_A + lambda2 * other_B;
    }

    void CalculateClosestPointsTri(List<Vector3> simplex, Dictionary<Vector3, MinkowskiDiffPair> MDP, out Vector3 my_closest, out Vector3 other_closest)
    {
        print("unimplemented triangle simplex case");
        my_closest = MDP[simplex[0]].MyPoint;
        other_closest = MDP[simplex[0]].OtherPoint;

        return;
        Vector3 A = simplex[0];
        Vector3 B = simplex[1];
        Vector3 C = simplex[2];

        if(A == B && B == C)
        {
            my_closest = A;
            other_closest = A;

            return;
        }
        else if(A == B || A == C)
        {
            simplex = new List<Vector3>
            {
                B, C
            };

            CalculateClosestPointsLine(simplex, MDP, out my_closest, out other_closest);

            return;
        }
        else if(B == C)
        {
            simplex = new List<Vector3>
            {
                A, B
            };

            CalculateClosestPointsLine(simplex, MDP, out my_closest, out other_closest);

            return;
        }

        Vector3 ABC = Vector3.Cross(B - A, C - A);

        float lambda1;
        float lambda2;
        float lambda3 = 1 - lambda1 - lambda2;

        Vector3 my_A = MDP[A].MyPoint;
        Vector3 other_A = MDP[A].OtherPoint;
        Vector3 my_B = MDP[B].MyPoint;
        Vector3 other_B = MDP[B].OtherPoint;
        Vector3 my_C = MDP[C].MyPoint;
        Vector3 other_C = MDP[C].OtherPoint;

        my_closest = lambda1 * my_A + lambda2 * my_B + lambda3 * my_C;
        other_closest = lambda1 * other_A + lambda2 * other_B + lambda3 * other_C;
    }

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction, out Vector3 my_support, out Vector3 other_support, Dictionary<Vector3, MinkowskiDiffPair> record)
    {
        my_support = support.Support(direction);
        other_support = other.support.Support(-direction);
        Vector3 result = my_support - other_support;

        if (record.ContainsKey(result))
        {
            record.Remove(result);
        }
        record.Add(result, new MinkowskiDiffPair(my_support, other_support));

        return result;
    }

    Vector3 Cross_ABA(Vector3 A, Vector3 B)
    {
        return Vector3.Cross( Vector3.Cross(A, B), A );
    }

    /*
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
    */

    /*
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
        Vector3 BC = C - B;
        Vector3 CA = A - C;

        Vector3 ABC = Vector3.Cross(AB, -CA);

        //vector3 perpendicular to each edge pointing outward
        Vector3 ABP = Vector3.Cross(AB, ABC);
        Vector3 BCP = Vector3.Cross(BC, ABC);
        Vector3 CAP = Vector3.Cross(CA, ABC);

        //inner region of triangle
        if (Vector3.Dot(ABP, CO) < 0 && Vector3.Dot(BCP, AO) < 0 && Vector3.Dot(CAP, BO) < 0)
        {
            Vector3 p1 = Vector3.Project(AO, AB);
            Vector3 p2 = Vector3.Project(BO, BC);
        }
        
        //check each line
        if (AO.magnitude < CO.magnitude && BO.magnitude < CO.magnitude)
        {
            convex_hull = new List<Vector3>
            {
                A, B
            };
        }

        if (AO.magnitude < BO.magnitude && CO.magnitude < BO.magnitude)
        {
            convex_hull = new List<Vector3>
            {
                A, C
            };
        }

        if (BO.magnitude < AO.magnitude && CO.magnitude < BO.magnitude)
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

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction)
    {
        return support.Support(direction) - other.support.Support(-direction);
    }

    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction, out Vector3 temp_closest)
    {
        temp_closest = support.Support(direction);
        return temp_closest - other.support.Support(-direction);
    }
    */

    List<Vector3> Simplex_Push_Front(Vector3 new_point, List<Vector3> simplex)
    {
        List<Vector3> new_simplex = new List<Vector3>
        {
            new_point
        };

        new_simplex.AddRange(simplex);

        return new_simplex;
    }

    class MinkowskiDiffPair
    {
        private Vector3 my_point;
        private Vector3 other_point;

        public MinkowskiDiffPair(Vector3 my, Vector3 other)
        {
            my_point = my;
            other_point = other;
        }

        public Vector3 MyPoint
        {
            get
            {
                return my_point;
            }
        }

        public Vector3 OtherPoint
        {
            get
            {
                return other_point;
            }
        }
    }
}