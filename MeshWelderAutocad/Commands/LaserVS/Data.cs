using System.Collections.Generic;

namespace MeshWelderAutocad.Commands.LaserVS
{
    internal sealed class DataVsDto
    {
        public List<PanelVsDto> Panels { get; set; } = new List<PanelVsDto>();
    }

    internal sealed class PanelVsDto
    {
        public string AssemblyName { get; set; }
        public List<Line2Dto> Boundaries { get; set; } = new List<Line2Dto>();
        public List<CurveDto> LargeOpeningsLines { get; set; } = new List<CurveDto>();
        public List<Line2Dto> Loops { get; set; } = new List<Line2Dto>();
        public List<Line2Dto> Anchors { get; set; } = new List<Line2Dto>();
        public List<Line2Dto> Pockets { get; set; } = new List<Line2Dto>();
        public List<CurveDto> SmallOpeningsLines { get; set; } = new List<CurveDto>();
        public Dictionary<string, List<Line2Dto>> EmbeddedDetails { get; set; } = new Dictionary<string, List<Line2Dto>>();
    }

    internal sealed class Point2Dto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    internal sealed class Line2Dto
    {
        public Point2Dto Start { get; set; }
        public Point2Dto End { get; set; }
    }

    internal sealed class CurveDto
    {
        public CurveDtoKind Kind { get; set; }
        public Point2Dto Start { get; set; }
        public Point2Dto End { get; set; }
        public Point2Dto PointOnArc { get; set; }
    }

    internal enum CurveDtoKind
    {
        Line = 0,
        Arc = 1,
    }
}
