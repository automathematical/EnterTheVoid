using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace EnterTheVoid
{
    public class EnterTheVoidComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public EnterTheVoidComponent()
          : base("EnterTheVoidComponent", "ETV",
              "Description","DOT3", "DOT3")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iCount", "iC","iteration count", GH_ParamAccess.item);
            pManager.AddBrepParameter("brepVoid", "brepV", "The void", GH_ParamAccess.item);
            pManager.AddPointParameter("randomPoint", "P","random point on curves",GH_ParamAccess.item);
            pManager.AddCurveParameter("cCurves", "cC", "connection curves" ,GH_ParamAccess.item);
            pManager.AddBrepParameter("brepEntrance", "BE","entrance block", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Pl", "show plane", GH_ParamAccess.item);
            pManager.AddCurveParameter("interCurves", "interCurves", "interCurves", GH_ParamAccess.item);
            pManager.AddCircleParameter("circleN", "circleN", "circleN", GH_ParamAccess.item);
            pManager.AddPointParameter("ptA", "ptA", "show ptA", GH_ParamAccess.item);
            pManager.AddBrepParameter("cone", "cone", "cone cone", GH_ParamAccess.item);
            pManager.AddPointParameter("pts", "pts", "pts", GH_ParamAccess.item);
            pManager.AddLineParameter("ConnectionLines", "ConnectionLines", "ConnectionLines", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Set the data from the input params
            bool reset = true;
            int iCount = 0;
            Brep brepVoid = null; 
            Point3d ptRandom = Point3d.Unset;
            Curve cCurves = null;
            Brep brepEnter = null;

            //Acces the data from the input
            DA.GetData("Reset", ref reset);
            DA.GetData("iCount", ref iCount);
            DA.GetData("brepVoid", ref brepVoid);
            DA.GetData("randomPoint", ref ptRandom);
            DA.GetData("cCurves", ref cCurves);
            DA.GetData("brepEntrance", ref brepEnter);

            Plane plane = Plane.Unset;
            Curve curveA = null;
            Circle circleN = Circle.Unset;
            Point3d ptA = Point3d.Unset;
            double dx = 0.0;

            if (GetTheTangent(ptRandom, brepVoid, out plane, out curveA,out circleN, out ptA, out dx)) { }
            Cone cone = (GetExtendedCone(brepVoid, ptRandom, dx));
            List<Point3d> pts = GetIntersecetionPoints(brepVoid, ptRandom, cCurves, cone);

            List<Line> lines = new List<Line>();
            for (int i = 0; i < pts.Count; i++)
            {
                Line l= new Line(ptRandom, pts[i]);
                lines.Add(l);
            }

            DA.SetData(0, plane);
            DA.SetData(1, curveA);
            DA.SetData(2, circleN);
            DA.SetData(3, ptA);
            DA.SetData(4, cone);
            DA.SetDataList(5, pts);
            DA.SetDataList(6, lines);
        }

        //Where the action happens
        public List<Point3d> GetIntersecetionPoints(Brep _brepVoid, Point3d _ptRandom, Curve _cCurves, Cone _cone)
        {
            var brepVoid = _brepVoid;
            var ptRandom = _ptRandom;
            var cCurves = _cCurves;
            var cone = _cone;

            Brep brepCone = cone.ToBrep(true);

            List<Point3d> pt1 = new List<Point3d>();

            double intersection_tolerance = 0.001;
            double overlap_torelance = 0.0;

            Curve[] overlap_curves;
            Point3d[] inter_points;
            if (Rhino.Geometry.Intersect.Intersection.CurveBrep(cCurves, brepCone, intersection_tolerance, out overlap_curves, out inter_points)) ;
            if (inter_points != null)
            {
                for (int i = 0; i < inter_points.Length; i++)
                {
                    pt1.Add(new Point3d(inter_points[i]));
                }
            }

            //var events = Rhino.Geometry.Intersect.Intersection.CurveSurface(cCurves, coneExtended.ToRevSurface(), intersection_tolerance, overlap_torelance);
            //if (events != null)
            //{
            //    for (int i = 0; i < events.Count; i++)
            //    {
            //        var ccx = events[i];
            //        pt1.Add(ccx.PointA);
            //    }
            //}

            return pt1;
        }

        //get the extra dx added to the radius for a correct cone radius
        private bool GetTheTangent(Point3d _ptRandom, Brep _brepVoid, out Plane _plane, out Curve _curve, out Circle _circle, out Point3d _ptA, out double _dx)
        {
            var brepVoid = _brepVoid;
            var ptRandom = _ptRandom;

            Vector3d vecSide = Vector3d.ZAxis;

            var bb = brepVoid.GetBoundingBox(true);
            var sph = new Sphere(bb.Center, (bb.Max.X - bb.Min.X) / 2);

            var centerPoint = sph.Center;
            var sphereRadius = sph.Radius;

            var vectorBtw = centerPoint - ptRandom;
            var vecNormal = Vector3d.CrossProduct(vectorBtw, vecSide);

            var planeIntersect = new Plane(centerPoint, vecNormal);

            Curve[] inter_curves;
            Point3d[] inter_points;
            double intersection_tolerance = 0.001;

            if (Rhino.Geometry.Intersect.Intersection.BrepPlane(brepVoid, planeIntersect, intersection_tolerance, out inter_curves, out inter_points)) { }

            Curve inter_curve = inter_curves[0];
            Plane plane = Plane.Unset;

            if(inter_curve.TryGetPlane(out plane)) { }

            Circle circeNew = new Circle(plane, centerPoint, sphereRadius);

            Point3d ptA = Point3d.Unset;
            if (FindTangentEasy(circeNew, ptRandom, out ptA)) { }

            var tangentL = ptA - ptRandom;

            var Rdx = sphereRadius * (vectorBtw.Length / tangentL.Length);
            var dx = Rdx - sphereRadius;

            _plane = planeIntersect;
            _curve = inter_curve;
            _circle = circeNew;
            _ptA = ptA;
            _dx = dx;

            return true;
        }

        //Method to find the tanget with 2 circles
        public bool FindTangentEasy(Circle _circle1, Point3d _pointExt, out Point3d _ptA)
        {
            var circle = _circle1;
            var ptExt = _pointExt;

            var centerPointCircle = circle.Center;
            var connectionline = centerPointCircle - ptExt;
            var vectorMid = connectionline * 0.5;

            var ptMid = ptExt + vectorMid;

            var circlePlane = circle.Plane;

            var circle2 = new Circle(circlePlane, ptMid, vectorMid.Length);

            Point3d ptA = Point3d.Unset;

            var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(circle.ToNurbsCurve(), circle2.ToNurbsCurve(), 0.001, 0.0);
            if (events != null)
            {
                for (int i = 0; i < 1; i++)
                {
                    var ccx = events[i];
                    ptA = ccx.PointA;
                }
            }

            _ptA = ptA;

            return true;
        }

        //Make the extended cone to intersect with
        public Cone GetExtendedCone(Brep _brepVoid, Point3d _ptRandom, double _extraBcTangent)
        {
            var brepVoid = _brepVoid;
            var ptRandom = _ptRandom;
            var extraBcTangent = _extraBcTangent;

            var bb = brepVoid.GetBoundingBox(true);
            var sph = new Sphere(bb.Center, (bb.Max.X - bb.Min.X) / 2);

            var centerPoint = sph.Center;
            var sphereRadius = sph.Radius;
            var sphereRadiusExtended = sphereRadius + extraBcTangent;

            var vectorCone = centerPoint - ptRandom;
            var planeCone = new Plane(ptRandom, vectorCone);
            //Cone cone = new Cone(planeCone, vectorCone.Length, sphereRadius);

            int iExtend = 10;
            Cone coneExtended = new Cone(planeCone, vectorCone.Length * iExtend, sphereRadiusExtended * iExtend);

            return coneExtended;
        }

        #region old 

        //// Find the tangent points for this circle and external point.
        //// Return true if we find the tangents, false if the point is
        //// inside the circle.
        //public bool FindTangentsfnc(Point3d center, double radius, Point3d external_point, out Point3d pt1, out Point3d pt2)
        //{
        //    // Find the distance squared from the
        //    // external point to the circle's center.
        //    double dx = center.X - external_point.X;
        //    double dy = center.Y - external_point.Y;
        //    double D_squared = dx * dx + dy * dy;
        //    if (D_squared < radius * radius)
        //    {
        //        pt1 = new Point3d(-1, -1, 0);
        //        pt2 = new Point3d(-1, -1, 0);
        //        return false;
        //    }

        //    // Find the distance from the external point
        //    // to the tangent points.
        //    double L = Math.Sqrt(D_squared - radius * radius);

        //    // Find the points of intersection between
        //    // the original circle and the circle with
        //    // center external_point and radius dist.
        //    FindCircleCircleIntersections(
        //        center.X, center.Y, radius,
        //        external_point.X, external_point.Y, (double)L,
        //        out pt1, out pt2);

        //    return true;
        //}

        //// Find the points where the two circles intersect.
        //public int FindCircleCircleIntersections(double cx0, double cy0, double radius0,
        //    double cx1, double cy1, double radius1,
        //    out Point3d intersection1, out Point3d intersection2)
        //{
        //    // Find the distance between the centers.
        //    double dx = cx0 - cx1;
        //    double dy = cy0 - cy1;
        //    double dist = Math.Sqrt(dx * dx + dy * dy);

        //    // See how many solutions there are.
        //    if (dist > radius0 + radius1)
        //    {
        //        // No solutions, the circles are too far apart.
        //        intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        return 0;
        //    }
        //    else if (dist < Math.Abs(radius0 - radius1))
        //    {
        //        // No solutions, one circle contains the other.
        //        intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        return 0;
        //    }
        //    else if ((dist == 0) && (radius0 == radius1))
        //    {
        //        // No solutions, the circles coincide.
        //        intersection1 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        intersection2 = new Point3d(float.NaN, float.NaN, float.NaN);
        //        return 0;
        //    }
        //    else
        //    {
        //        // Find a and h.
        //        double a = (radius0 * radius0 -
        //            radius1 * radius1 + dist * dist) / (2 * dist);
        //        double h = Math.Sqrt(radius0 * radius0 - a * a);

        //        // Find P2.
        //        double cx2 = cx0 + a * (cx1 - cx0) / dist;
        //        double cy2 = cy0 + a * (cy1 - cy0) / dist;

        //        // Get the points P3.
        //        intersection1 = new Point3d(
        //            (float)(cx2 + h * (cy1 - cy0) / dist),
        //            (float)(cy2 - h * (cx1 - cx0) / dist),
        //            (float)(0));
        //        intersection2 = new Point3d(
        //            (float)(cx2 - h * (cy1 - cy0) / dist),
        //            (float)(cy2 + h * (cx1 - cx0) / dist),
        //            (float)(0));
        //        // See if we have 1 or 2 solutions.
        //        if (dist == radius0 + radius1) return 1;
        //        return 2;
        //    }
        //}

        //private Curve CreateConnectionCurves(int _iCount, Brep _brepVoid, Point3d _ptRandom, Curve _cCurves, Brep _brepEnter)
        //{
        //    List<Curve> curves = new List<Curve>();

        //    for (int i = 0; i < _iCount; i++)
        //    {
        //        Point3d pt1 = new Point3d(0, 0, 0);
        //        Point3d pt2 = new Point3d(22, 22, 0);

        //        Line line = new Line(pt1, pt2);
        //        Curve crv = line.ToNurbsCurve();

        //        curves.Add(crv);
        //    }
        //    return curves[0];
        //}

        #endregion

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("adb8444b-5f25-4428-b1ef-09d801f6f400"); }
        }
    }
}
