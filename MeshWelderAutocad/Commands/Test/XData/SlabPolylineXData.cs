using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MeshWelderAutocad.Commands.Test
{
    [DataContract]
    public class SlabPolylineXData 
    {
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public Guid PlanPolylineGuid { get; set; }
        [DataMember]
        public Guid PlanImageGuid { get; set; }
        [DataMember]
        public Guid SlabImageGuid { get; set; }
        [DataMember]
        public Vertex SlabPolylineCentroid { get; set; }
        //public override bool Equals(object obj)
        //{
        //    if (obj is SlabPolylineXData other)
        //    {
        //        if (Guid == other.Guid
        //            && PlanPolylineGuid == other.PlanPolylineGuid
        //            && PlanImageGuid == other.PlanImageGuid
        //            && SlabImageGuid == other.SlabImageGuid)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
    }
}
