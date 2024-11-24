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
    public class PolylineXData
    {
        //Тут по идее надо бы еще хранить позицию картинки на плане текущую и позицию слеба, может информацию об их подрезке также
        [DataMember]
        public Guid SlabPolylineGuid { get; set; }
        [DataMember]
        public Guid PlanPolylineGuid { get; set; }
        [DataMember]
        public Guid PlanImageGuid { get; set; }
        [DataMember]
        public Guid SlabImageGuid { get; set; }
        //[DataMember]
        //public PolylineType PolylineType { get; set; }
        [DataMember]
        public List<Vertex> SlabPolylineVertexes { get; set; }
        [DataMember]
        public List<Vertex> PlanPolylineVertexes { get; set; }
    }
}
