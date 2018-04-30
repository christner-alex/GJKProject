using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISupport))]
public class GJKCollider : MonoBehaviour {

    private ISupport support;

    private int MAX_ITERATIONS = 32;

	// Use this for initialization
	void Start ()
    {
        support = GetComponent<ISupport>();
    }

    //returns true if this chape is colliding with other
    public bool CollidesWithOther(GJKCollider other)
    {
        Vector3 newest_point;
        
        //get point in arbitrary direction
        Vector3 direction = Vector3.right;
        Vector3 C = MinkowskiDiffSupport(other, direction);
        //if that point is in the opposite direction from the origin,
        //no collision
        if(Vector3.Dot(C, direction) < 0)
        {
            return false;
        }

        //get point in other direction
        direction = -C;
        Vector3 B = MinkowskiDiffSupport(other, direction);
        //if that point is in the opposite direction from the origin,
        //no collision
        if (Vector3.Dot(B, direction) < 0)
        {
            return false;
        }

        //set next direction to check as perpendicular to that line
        direction = Cross_ABA(C - B, -B);
        List<Vector3> simplex = new List<Vector3>
        {
            B, C
        };
        
        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            //get the support point in newest direction
            newest_point = MinkowskiDiffSupport(other, direction);

            //if that point is not closer to the origin,
            //there is no collision
            if (Vector3.Dot(newest_point, direction) < 0)
            {
                return false;
            }

            //operate on the simplex,
            //return true if the simplex is a tetrahedron
            //that contains the origin
            if(DoSimplex(newest_point, ref simplex, ref direction))
            {
                return true;
            }

        }

        //don't iterate too much to avoid slowdown
        return false;
    }
    
    //return true if the simplex is a tetrahedron that contains the origin, false otherwise.
    //also update direction and simplex to continue searching
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
            print("simplex error. Count="+simplex.Count);
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
            //origin in region between A and B

            //smalles simplex containing the next most extreme point
            //is the line AB
            simplex = new List<Vector3>
            {
                A, B
            };

            //the new direction is perpendicular to the line toward the origin
            direction = Cross_ABA(AB, AO);
        }
        else
        {
            //origin in region beyond A

            //smalles simplex containing the next most extreme point
            //is the point A
            simplex = new List<Vector3>
            {
                A
            };

            //new direction is further in the direciton from A to origin
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
            //smalles simplex containing next most extreme point
            //is the line AB
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
            //smalles simplex containing next most extreme point
            //is the line AC
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
            //smallest simplex containing next most extreme point
            //is the triangle ABC
            simplex = new List<Vector3>
            {
                A, B, C
            };

            //search above the triangle
            direction = ABC;
        }
        else
        {
            //smallest simplex containing next most extreme point
            //is the triangle ACB
            simplex = new List<Vector3>
            {
                A, C, B
            };

            //search below the triangle
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
        //Vector3 tmp;

        Vector3 AB = B - A;
        Vector3 AC = C - A;
        Vector3 AD = D - A;

        //Vectors parallel to faces poining outward
        Vector3 ABC = Vector3.Cross(AB, AC);
        Vector3 ACD = Vector3.Cross(AC, AD);
        Vector3 ADB = Vector3.Cross(AD, AB);

        bool over_ABC = Vector3.Dot(ABC, AO) > 0;
        bool over_ACD = Vector3.Dot(ACD, AO) > 0;
        bool over_ADB = Vector3.Dot(ADB, AO) > 0;

        Vector3 rotA = A;
        Vector3 rotB = B;
        Vector3 rotC = C;
        Vector3 rotD = D;
        Vector3 rotAB = AB;
        Vector3 rotAC = AC;
        Vector3 rotAD = AD;
        Vector3 rotABC = ABC;
        Vector3 rotACD = ACD;

        //determine which faces to test
        if(!over_ABC && !over_ACD && !over_ADB)
        {
            //the origin is behind all 3 faces,
            //so the origin is within the Minkowski Difference,
            //so the spaces are overlapping
            return true;
        }
        else if(over_ABC && !over_ACD && !over_ADB)
        {
            //the origin is over ABC, but not ACD or ADB

            rotA = A;
            rotB = B;
            rotC = C;

            rotAB = AB;
            rotAC = AC;

            rotABC = ABC;

            goto check_one_face;
        }
        else if (!over_ABC && over_ACD && !over_ADB)
        {
            //the origin is over ACD, but not ABC or ADB

            rotA = A;
            rotB = C;
            rotC = D;

            rotAB = AC;
            rotAC = AD;

            rotABC = ACD;

            goto check_one_face;
        }
        else if (!over_ABC && !over_ACD && over_ADB)
        {
            //the origin is over ADB, but not ABC or ACD
            
            rotA = A;
            rotB = D;
            rotC = B;

            rotAB = AD;
            rotAC = AB;

            rotABC = ADB;

            goto check_one_face;
        }
        else if (over_ABC && over_ACD && !over_ADB)
        {
            //the origin is over ABC and ACD, but not ADB

            rotA = A;
            rotB = B;
            rotC = C;
            rotD = D;

            rotAB = AB;
            rotAC = AC;
            rotAD = AD;

            rotABC = ABC;
            rotACD = ACD;

            goto check_two_faces;
        }
        else if (!over_ABC && over_ACD && over_ADB)
        {
            //the origin is over ADB and ACD, but not ABC
            
            rotA = A;
            rotB = C;
            rotC = D;
            rotD = B;

            rotAB = AC;
            rotAC = AD;
            rotAD = AB;

            rotABC = ACD;
            rotACD = ADB;

            goto check_two_faces;
        }
        else if (over_ABC && !over_ACD && over_ADB)
        {
            //the origin is over ABC and ADB, but not ACD
            
            rotA = A;
            rotB = D;
            rotC = B;
            rotD = C;

            rotAB = AD;
            rotAC = AB;
            rotAD = AC;

            rotABC = ADB;
            rotACD = ABC;

            goto check_two_faces;
        }
        
        check_one_face:

        if (Vector3.Dot(Vector3.Cross(rotABC, rotAC), AO) > 0)
        {
            //origin in in the region AC

            //smallest simplex containing next extreme point is line AC
            simplex = new List<Vector3>
            {
                rotA, rotC
            };

            //search in direction perpendicular to AC toward the origin
            direction = Cross_ABA(rotAC, AO);

            return false;
        }

        check_one_face_part_2:

        if (Vector3.Dot(Vector3.Cross(rotAB, rotABC), AO) > 0)
        {
            //origin in in the region AC

            //smallest simplex containing next extreme point is line AB
            simplex = new List<Vector3>
            {
                rotA, rotB
            };

            //search in direction perpendicular to AB toward the origin
            direction = Cross_ABA(rotAB, AO);

            return false;
        }

        //origin in in the region ABC

        //smallest simplex containing next extreme point is triangle ABC
        simplex = new List<Vector3>
        {
            rotA, rotB, rotC
        };

        //search in perpendicular to that triangle
        direction = rotABC;

        return false;

        check_two_faces:

        if (Vector3.Dot(Vector3.Cross(rotABC, rotAC), AO) > 0)
        {
            //origin is beyond AC,
            //So we consider ACD
            rotB = rotC;
            rotC = rotD;

            rotAB = rotAC;
            rotAC = rotAD;

            rotABC = rotACD;

            goto check_one_face;
        }

        //either over ABC or AB

        goto check_one_face_part_2;
    }

    //returns a most most extreme on the Minkowski Difference along direction
    //created by this shape and other
    Vector3 MinkowskiDiffSupport(GJKCollider other, Vector3 direction)
    {
        Vector3 my_support = support.Support(direction);
        Vector3 other_support = other.support.Support(-direction);
        Vector3 result = my_support - other_support;

        return result;
    }

    //returns A x B x A
    Vector3 Cross_ABA(Vector3 A, Vector3 B)
    {
        return Vector3.Cross( Vector3.Cross(A, B), A );
    }
}