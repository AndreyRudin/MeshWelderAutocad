using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test
{
    public class SetLinkedElement
    {
        public ObjectId PlanImageId { get; set; }
        public ObjectId SlabImageId { get; set; }
        public ObjectId PlanPolylineId { get; set; }
        public ObjectId SlabPolylineId { get; set; }
        public SetLinkedElement(ObjectId planImageId, ObjectId slabImageId, ObjectId planPolylineId, ObjectId slabPolylineId)
        {
            PlanImageId = planImageId;
            SlabImageId = slabImageId;
            PlanPolylineId = planPolylineId;
            SlabPolylineId = slabPolylineId;
        }
    }
}
