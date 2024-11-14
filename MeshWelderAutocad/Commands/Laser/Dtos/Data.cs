using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Laser.Dtos
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
        public List<Loop> Loops { get; set; } = new List<Loop>();
        [DataMember]
        public List<Connection> Connection1 { get; set; } = new List<Connection>();
        [DataMember]
        public List<Connection> Connection2 { get; set; } = new List<Connection>();
        [DataMember]
        public List<Anchor> Anchors { get; set; } = new List<Anchor>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts6 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts9 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<Pocket> Pockets { get; set; } = new List<Pocket>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts7 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts5 { get; set; } = new List<EmbeddedPart>();
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
        [DataMember]
        public List<Opening> Openings { get; set; } = new List<Opening>();
        public Formwork()
        {

        }
    }
    [DataContract]
    internal class Connection
    {
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double Angle { get; set; }
        [DataMember]
        public bool IsDiagonal { get; set; }
        public Connection()
        {

        }
        public Connection(double x, double y, double angle, bool isDiagonal)
        {
            X = x;
            Y = y;
            Angle = angle;
            IsDiagonal = isDiagonal;
        }
    }
    [DataContract]
    public class Opening
    {
        [DataMember]
        public double MaxY { get; set; }
        [DataMember]
        public double MaxX { get; set; }
        [DataMember]
        public double MinY { get; set; }
        [DataMember]
        public double MinX { get; set; }
        [DataMember]
        public bool IsDoor { get; set; }
        public Opening()
        {

        }
    }
    [DataContract]
    public class Loop
    {
        [DataMember]
        public double X { get; set; }
        public Loop() { }
        public Loop(double x)
        {
            X = x;
        }
    }
    [DataContract]
    public class Anchor
    {
        [DataMember]
        public double X { get; set; }
        public Anchor() { }
        public Anchor(double x)
        {
            X = x;
        }
    }
    [DataContract]
    public class EmbeddedPart
    {
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double X { get; set; }
        public EmbeddedPart() { }
        public EmbeddedPart(double x, double y = 0)
        {
            X = x;
            Y = y;
        }
    }
    [DataContract]
    public class Pocket
    {
        [DataMember]
        public double X { get; set; }
        public Pocket() { }
        public Pocket(double x)
        {
            X = x;
        }
    }
}
