using MeshWelderAutocad.Commands.Laser.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.LaserEOM
{
    [DataContract]
    internal class Data
    {
        [DataMember]
        public string RevitFileName { get; set; }
        [DataMember]
        public List<Panel> Panels { get; set; } = new List<Panel>();
        public Data()
        {

        }
    }
    [DataContract]
    internal class Panel
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public Formwork Formwork { get; set; }
        [DataMember]
        public List<Box> Boxes { get; set; } = new();
        [DataMember]
        public List<Pipe> Pipes { get; set; } = new();
        public Panel()
        {

        }
    }
    [DataContract]
    public class Formwork
    {
        [DataMember]
        public double MaxYPanel { get; set; }
        [DataMember]
        public double MaxXPanel { get; set; }
        [DataMember]
        public double MinXPanel { get; set; }
        [DataMember]
        public double MinYPanel { get; set; }
        public Formwork()
        {

        }
    }
    [DataContract]
    public class Box
    {
        [DataMember]
        public double CenterX { get; set; }
        [DataMember]
        public double CenterY { get; set; }
        public Box()
        {
            
        }
    }
    [DataContract]
    public class Pipe
    {
        [DataMember]
        public double StartX { get; set; }
        [DataMember]
        public double StartY { get; set; }
        [DataMember]
        public double EndX { get; set; }
        [DataMember]
        public double EndY { get; set; }
    }
}
