using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace EnterTheVoid
{
    public class FindTangents : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FindTangents()
          : base("FindTangents", "Nickname",
              "Description",
              "DOT3", "DOT3")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "Circle", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "Pt", "Point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("PointA", "PtA", "PointA", GH_ParamAccess.item);
            pManager.AddPointParameter("PointB", "PtB", "PointB", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Set the data from the input params
            Circle circle = Circle.Unset;
            Point3d point = Point3d.Unset;

            //Acces the data from the input
            DA.GetData("Circle", ref circle);
            DA.GetData("Point", ref point);

            var center = circle.Center;
            var raduis = circle.Radius;

            Point3d pointA = Point3d.Unset;
            Point3d pointB = Point3d.Unset;

            if (FindTangentsfnc(center, raduis, point, out pointA, out pointB)) 

            DA.SetData(0, pointA);
            DA.SetData(1, pointB);

        }

        // Find the tangent points for this circle and external point.
        // Return true if we find the tangents, false if the point is
        // inside the circle.
        public bool FindTangentsfnc(Point3d center, double radius, Point3d external_point, out Point3d pt1, out Point3d pt2)
        {
            // Find the distance squared from the
            // external point to the circle's center.
            double dx = center.X - external_point.X;
            double dy = center.Y - external_point.Y;
            double D_squared = dx * dx + dy * dy;
            if (D_squared < radius * radius)
            {
                pt1 = new Point3d(-1, -1,0);
                pt2 = new Point3d(-1, -1,0);
                return false;
            }

            // Find the distance from the external point
            // to the tangent points.
            double L = Math.Sqrt(D_squared - radius * radius);

            // Find the points of intersection between
            // the original circle and the circle with
            // center external_point and radius dist.
            FindCircleCircleIntersections(
                center.X, center.Y, radius,
                external_point.X, external_point.Y, (double)L,
                out pt1, out pt2);

            return true;
        }

        // Find the points where the two circles intersect.
        public int FindCircleCircleIntersections(double cx0, double cy0, double radius0,
            double cx1, double cy1, double radius1,
            out Point3d intersection1, out Point3d intersection2)
        {
            // Find the distance between the centers.
            double dx = cx0 - cx1;
            double dy = cy0 - cy1;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // See how many solutions there are.
            if (dist > radius0 + radius1)
            {
                // No solutions, the circles are too far apart.
                intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
                intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
                return 0;
            }
            else if (dist < Math.Abs(radius0 - radius1))
            {
                // No solutions, one circle contains the other.
                intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
                intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
                return 0;
            }
            else if ((dist == 0) && (radius0 == radius1))
            {
                // No solutions, the circles coincide.
                intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
                intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
                return 0;
            }
            else
            {
                // Find a and h.
                double a = (radius0 * radius0 -
                    radius1 * radius1 + dist * dist) / (2 * dist);
                double h = Math.Sqrt(radius0 * radius0 - a * a);

                // Find P2.
                double cx2 = cx0 + a * (cx1 - cx0) / dist;
                double cy2 = cy0 + a * (cy1 - cy0) / dist;

                // Get the points P3.
                intersection1 = new Point3d(
                    (float)(cx2 + h * (cy1 - cy0) / dist),
                    (float)(cy2 - h * (cx1 - cx0) / dist),
                    (float)(0));
                intersection2 = new Point3d(
                    (float)(cx2 - h * (cy1 - cy0) / dist),
                    (float)(cy2 + h * (cx1 - cx0) / dist),
                    (float)(0));
                // See if we have 1 or 2 solutions.
                if (dist == radius0 + radius1) return 1;
                return 2;
            }
        }

        bool CircleTangents(Vector2d center, double r, Vector2d p, ref Vector2d tanPosA, ref Vector2d tanPosB)
        {
            p -= center;

            double P = p.Length;

            //if p is inside the circle, there ain't no tangents
            if (P <= r)
            {
                return false;
            }

            double a = r * r / P;
            double q = r * (float)Math.Sqrt((P * P) - (r * r)) / P;

            Vector2d pN = p / P;
            Vector2d pNP = new Vector2d(-pN.Y, pN.X);
            Vector2d va = pN * a;

            tanPosA = va + pNP * q;
            tanPosB = va - pNP * q;

            tanPosA += center;
            tanPosB += center;

            return true;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Tangent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("89a48e45-0228-445f-a827-e4fdd229f641"); }
        }
    }
}