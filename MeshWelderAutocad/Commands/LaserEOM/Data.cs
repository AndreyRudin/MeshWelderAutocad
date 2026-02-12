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
        public List<Panel> Panels { get; set; }
        public Data()
        {

        }
    }
    [DataContract]
    public class Route
    {
        [DataMember]
        public List<Pipe> Pipes { get; set; } = new();
        public Route()
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
        public List<Route> Routes { get; set; } = new();
        [DataMember]
        public List<Detail> Details { get; set; } = new();
        [DataMember]
        public List<Box> Boxes { get; set; } = new();
        [DataMember]
        public List<EmbeddedTube> EmbeddedTubes { get; set; } = new();
        public Panel()
        {

        }
    }
    [DataContract]
    public class EmbeddedTube
    {
        [DataMember]
        public string LayerName { get; set; }
        [DataMember]
        public double Diameter { get; set; }
        [DataMember]
        public double CenterX { get; set; }
        [DataMember]
        public double CenterY { get; set; }
        public EmbeddedTube()
        {
            
        }
    }

    [DataContract]
    public class Detail
    {
        [DataMember]
        public string LayerName { get; set; }
        [DataMember]
        public double MinX { get; set; }
        [DataMember]
        public double MinY { get; set; }
        [DataMember]
        public double MaxX { get; set; }
        [DataMember]
        public double MaxY { get; set; }
        public Detail()
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
        public string LayerName { get; set; }
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
        public string LayerName { get; set; }
        [DataMember]
        public bool IsArc { get; set; }
        [DataMember]
        public double CenterX { get; set; }
        [DataMember]
        public double CenterY { get; set; }
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
