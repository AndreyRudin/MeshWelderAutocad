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
    public class XDataJson
    {
        [DataMember]
        public string PlanPolylineId { get; set; }

        [DataMember]
        public string PlanImageId { get; set; }

        [DataMember]
        public string SlabImageId { get; set; }
        [DataMember]
        public Point FirstVertex { get; set; }
    }
}
