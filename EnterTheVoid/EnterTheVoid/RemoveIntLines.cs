using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace EnterTheVoid
{
    public class RemoveIntLines : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public RemoveIntLines()
          : base("RemoveInterferingLines", "Nickname",
              "Remove intersecting Lines",
              "DOT3", "DOT3")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("use", "u", "toggle on/off", GH_ParamAccess.item);
            pManager.AddBrepParameter("brep", "br", "insert brep", GH_ParamAccess.item);
            pManager.AddCurveParameter("curves", "cr", "insert curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("curvesOut", "cr", "gives back the curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Set the data from the input params
            bool use = true;
            Brep brep = null;
            List<Curve> cCurves = new List<Curve>();

            //Acces the data from the input
            DA.GetData("use", ref use);
            DA.GetData("brep", ref brep);
            DA.GetDataList("curves", cCurves);

            //out
            List<Curve> curvesList = new List<Curve>();

            if (use)
            {
                curvesList = GetNotInterFeringCurves(cCurves, brep);
            }
            else
            {
                curvesList = cCurves;
            }

            DA.SetDataList(0, curvesList);
        }

        private List<Curve> GetNotInterFeringCurves(List<Curve> _curves, Brep _brep)
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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.IntersectingLines;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("feeb7e70-5af2-487c-8e78-aae27625a2b5"); }
        }
    }
}