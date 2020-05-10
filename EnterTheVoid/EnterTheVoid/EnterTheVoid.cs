using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace EnterTheVoid
{
    public class EnterTheVoid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public EnterTheVoid()
          : base("EnterTheVoid", "Nickname",
              "Connect straight lines to make a sphere void",
              "DOT3", "DOT3")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("UseEntrance", "U", "toggle entrance on/off", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iCount", "iC", "iteration count", GH_ParamAccess.item);
            pManager.AddBrepParameter("brepVoid", "brepV", "The void", GH_ParamAccess.item);
            pManager.AddPointParameter("randomPoint", "P", "random point on curves", GH_ParamAccess.item);
            pManager.AddCurveParameter("cCurves", "cC", "connection curves", GH_ParamAccess.list);
            pManager.AddBrepParameter("brepEntrance", "BE", "entrance block", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("cones", "cones", "show cones", GH_ParamAccess.item);
            pManager.AddPointParameter("pts", "pts", "show pts", GH_ParamAccess.item);
            pManager.AddCurveParameter("ConnectionCurves", "CLs", "show connectionCurves", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Set the data from the input params
            bool useEntrance = true;
            int iCount = 0;
            Brep brepVoid = null;
            Point3d ptRandom = Point3d.Unset;
            List<Curve> cCurves = new List<Curve>();
            Brep brepEnter = null;

            //Acces the data from the input
            DA.GetData("UseEntrance", ref useEntrance);
            DA.GetData("iCount", ref iCount);
            DA.GetData("brepVoid", ref brepVoid);
            DA.GetData("randomPoint", ref ptRandom);
            DA.GetDataList("cCurves", cCurves);
            DA.GetData("brepEntrance", ref brepEnter);

            List<Cone> conesList = new List<Cone>();
            List<Point3d> ptsList = new List<Point3d>();
            List<Curve> curvesList = new List<Curve>();

            //Add the first point
            ptsList.Add(ptRandom);

            List<Point3d> ptsListNew = new List<Point3d>();
            List<Line> linesList = new List<Line>();
            double dx = 0.0;

            for (int i = 0; i < ptsList.Count; i++)
            {
                if (ptsList.Count < iCount)
                {
                    if (GetTheTangent(ptsList[i], brepVoid, out dx)) { }
                    List<Cone> cones = (GetExtendedCones(brepVoid, ptsList[i], dx));
                    foreach (var c in cones) { conesList.Add(c); };

                    for (int j = 0; j < cones.Count; j++)
                    {
                        List<Point3d> pt = GetIntersecetionPoints(brepVoid, ptsList[i], cCurves, cones[j]);
                        foreach (var p in pt)
                        {
                            linesList.Add(new Line(p, ptsList[i]));
                            ptsList.Add(p);
                        }
                    }
                }
                else { break; }
            }

            for (int i = 0; i < linesList.Count; i++)
            {
                curvesList.Add(linesList[i].ToNurbsCurve());
            }

            if (useEntrance)
                curvesList = GetNotIntersectingCurves(curvesList, brepEnter);

            DA.SetDataList(0, conesList);
            DA.SetDataList(1, ptsList);
            DA.SetDataList(2, curvesList);
        }

        //Where the action happens
        private List<Point3d> GetIntersecetionPoints(Brep _brepVoid, Point3d _ptRandom, List<Curve> _cCurves, Cone _cone)
        {
            var brepVoid = _brepVoid;
            var ptRandom = _ptRandom;
            var cCurves = _cCurves;
            var cone = _cone;

            Brep brepCone = cone.ToBrep(true);

            List<Point3d> pt1 = new List<Point3d>();

            double intersection_tolerance = 0.001;
            //double overlap_torelance = 0.0;

            Curve[] overlap_curves;
            Point3d[] inter_points;
            for (int i = 0; i < cCurves.Count; i++)
            {
                if (Rhino.Geometry.Intersect.Intersection.CurveBrep(cCurves[i], brepCone, intersection_tolerance, out overlap_curves, out inter_points))
                    if (inter_points != null)
                    {
                        for (int j = 0; j < inter_points.Length; j++)
                        {
                            pt1.Add(new Point3d(inter_points[j]));
                        }
                    }
            }

            #region 'other intersection method'
            //var events = Rhino.Geometry.Intersect.Intersection.CurveSurface(cCurves, coneExtended.ToRevSurface(), intersection_tolerance, overlap_torelance);
            //if (events != null)
            //{
            //    for (int i = 0; i < events.Count; i++)
            //    {
            //        var ccx = events[i];
            //        pt1.Add(ccx.PointA);
            //    }
            //}
            #endregion

            return pt1;
        }

        //get the extra dx added to the radius for a correct cone radius
        private bool GetTheTangent(Point3d _ptRandom, Brep _brepVoid, out double _dx)
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

            if (inter_curve.TryGetPlane(out plane)) { }

            Circle circeNew = new Circle(plane, centerPoint, sphereRadius);

            Point3d ptA = Point3d.Unset;
            if (FindTangentEasy(circeNew, ptRandom, out ptA)) { }

            var tangentL = ptA - ptRandom;

            var Rdx = sphereRadius * (vectorBtw.Length / tangentL.Length);
            var dx = Rdx - sphereRadius;

            _dx = dx;

            return true;
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

            if (inter_curve.TryGetPlane(out plane)) { }

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

        private List<Curve> GetNotIntersectingCurves(List<Curve> _curves, Brep _brep)
        {
            var cCurves = _curves;
            var brep = _brep;

            List<Curve> curvesListNot = new List<Curve>();
            List<Curve> curvesList = new List<Curve>();

            foreach (var c in cCurves)
            {
                Curve[] overLapCurves;
                Point3d[] intersectionPoints;
                if (Rhino.Geometry.Intersect.Intersection.CurveBrep(c, brep, 0.001, out overLapCurves, out intersectionPoints)) { }
                if (intersectionPoints.Length > 0)
                    curvesListNot.Add(c);
            }
            foreach (var c in cCurves)
                if (!curvesListNot.Contains(c))
                    curvesList.Add(c);

            return curvesList;
        }

        //Method to find the tanget with 2 circles
        private bool FindTangentEasy(Circle _circle1, Point3d _pointExt, out Point3d _ptA)
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
        private List<Cone> GetExtendedCones(Brep _brepVoid, Point3d _ptRandom, double _extraBcTangent)
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
            List<Cone> coneExtendedList = new List<Cone>();
            Cone coneExtended = new Cone(planeCone, vectorCone.Length * iExtend, sphereRadiusExtended * iExtend);
            coneExtendedList.Add(coneExtended);

            return coneExtendedList;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Properties.Resources.VoidImage;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2a3d6bcf-32d9-4ee5-9924-f96a26e9069e"); }
        }
    }
}