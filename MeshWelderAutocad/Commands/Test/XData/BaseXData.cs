using MeshWelderAutocad.Commands.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test
{
    [DataContract]
    public class BaseXData
    {
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public Guid SlabPolylineGuid { get; set; }
    }
}
