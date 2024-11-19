using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test
{
    public class LinkElement
    {
        public RasterImage PlanImage { get; set; }
        public RasterImage SlabImage { get; set; }
        public Polyline PlanPolyline { get; set; }
        public Polyline SlabPolyline { get; set; }
        public LinkElement(RasterImage planImage, RasterImage slabImage, Polyline planPolyline, Polyline slabPolyline)
        {
            PlanImage = planImage;
            SlabImage = slabImage;
            PlanPolyline = planPolyline;
            SlabPolyline = slabPolyline;
        }
    }
}
