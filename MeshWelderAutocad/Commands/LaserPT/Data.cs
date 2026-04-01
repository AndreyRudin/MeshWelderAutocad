using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MeshWelderAutocad.Commands.LaserPT
{
    [DataContract]
    internal class DataPtDto
    {
        [DataMember]
        public List<PanelPtDto> Panels { get; set; } = new List<PanelPtDto>();
    }

    [DataContract]
    internal class PanelPtDto
    {
        [DataMember]
        public string AssemblyName { get; set; }
        [DataMember]
        public List<Line2Dto> Boundaries { get; set; } = new List<Line2Dto>();
        [DataMember]
        public List<List<Line2Dto>> OpeningsLines { get; set; } = new List<List<Line2Dto>>();
        [DataMember]
        public List<Point2Dto> Loops { get; set; } = new List<Point2Dto>();
        [DataMember]
        public List<List<Line2Dto>> Pockets { get; set; } = new List<List<Line2Dto>>();
    }

    [DataContract]
    internal class Point2Dto
    {
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
    }

    [DataContract]
    internal class Line2Dto
    {
        [DataMember]
        public Point2Dto Start { get; set; }
        [DataMember]
        public Point2Dto End { get; set; }
    }
}
