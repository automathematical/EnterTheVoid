using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace EnterTheVoid
{
    public class EnterTheVoidInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "EnterTheVoid";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.VoidImage;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Create a Void with only straight lines";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("8dc90e23-4c75-460a-9547-68e310f0b4c9");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Mathieu van Leemputten";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "mathieuvanleemputten@hotmail.com";
            }
        }
    }
}
