using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test
{
    [DataContract]
    public class PolylineXData
    {
        [DataMember]
        public long SlabPolylineId { get; set; }
        [DataMember]
        public long PlanPolylineId { get; set; }
        [DataMember]
        public long PlanImageId { get; set; }
        [DataMember]
        public long SlabImageId { get; set; }
        [DataMember]
        public List<Vertex> SlabPolylineVertexes { get; set; }
        [DataMember]
        public List<Vertex> PlanPolylineVertexes { get; set; }
    }
}
