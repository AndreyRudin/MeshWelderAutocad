using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Test
{
    [DataContract]
    public class Vertex
    {
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
