using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad
{
    public class MyRebar
    {
        public int Id { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public double Diameter { get; set; }
    }
}
