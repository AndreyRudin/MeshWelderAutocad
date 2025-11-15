using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
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
        public List<List<Connection>> ConnectionsGroups { get; set; } = new List<List<Connection>>();
        [DataMember]
        public List<Anchor> Anchors { get; set; } = new List<Anchor>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts6 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts9 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<EmbeddedPart> EmbeddedParts11 { get; set; } = new List<EmbeddedPart>();
        [DataMember]
        public List<Pocket> Pockets { get; set; } = new List<Pocket>();
        [DataMember]
        public List<DetailDto> EmbeddedParts5 { get; set; } = new List<DetailDto>();
        [DataMember]
        public List<DetailDto> EmbeddedParts7 { get; set; } = new List<DetailDto>();
        [DataMember]
        public List<DetailDto> EmbeddedParts8 { get; set; } = new List<DetailDto>();
        [DataMember]
        public List<DetailDto> UnionDetails { get; set; } = new List<DetailDto>();
        public Panel()
        {

        }
    }
    [DataContract]
    internal class DetailDto
    {
        [DataMember]
        public double YCenter { get; set; }
        [DataMember]
        public double XCenter { get; set; }
        [DataMember]
        public double Height { get; set; }
        [DataMember]
        public double Width { get; set; }
        public DetailDto()
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

    public class CurveConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Curve);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            string type = obj["Type"]?.ToString();
            Curve result = type switch
            {
                "Line" => new Line(),
                "Arc" => new Arc(),
                "Circle" => new Circle(),
                _ => throw new NotSupportedException($"Unknown type: {type}")
            };
            serializer.Populate(obj.CreateReader(), result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jo = new JObject();
            jo.Add("Type", value.GetType().Name);

            switch (value)
            {
                case Line line:
                    jo.Add("Start", JToken.FromObject(line.Start, serializer));
                    jo.Add("End", JToken.FromObject(line.End, serializer));
                    break;
                case Arc arc:
                    jo.Add("Center", JToken.FromObject(arc.Center, serializer));
                    jo.Add("StartPoint", JToken.FromObject(arc.StartPoint, serializer));
                    jo.Add("EndPoint", JToken.FromObject(arc.EndPoint, serializer));
                    break;
                case Circle circle:
                    jo.Add("Center", JToken.FromObject(circle.Center, serializer));
                    jo.Add("Radius", circle.Radius);
                    break;
            }

            jo.WriteTo(writer);
        }
    }
    [JsonConverter(typeof(CurveConverter))]
    public abstract class Curve
    {
        public Curve() { }
    }
    [DataContract]
    public class Point2D
    {
        public Point2D()
        {
            
        }
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Z { get; set; }
    }
    [DataContract]
    public class Circle : Curve
    {
        public Circle() { }
        [DataMember]
        public Point2D Center { get; set; }
        [DataMember]
        public double Radius { get; set; }
    }
    [DataContract]
    public class Arc : Curve
    {
        public Arc() { }

        [DataMember]
        public Point2D Center { get; set; }
        [DataMember]
        public Point2D StartPoint { get; set; }
        [DataMember]
        public Point2D EndPoint { get; set; }
    }
    [DataContract]
    public class Line : Curve
    {
        public Line()
        {

        }
        [DataMember]
        public Point2D Start { get; set; }
        [DataMember]
        public Point2D End { get; set; }
    }
    [DataContract]
    public class Point
    {
        [DataMember]
        public double X { get; set; }
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double Z { get; set; }

        public Point() { }
    }
    [DataContract]
    public enum OpeningType
    {
        Window,
        Door,
        Hole
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
        public OpeningType Type { get; set; }
        [DataMember]
        public List<Point> Points { get; set; } = new List<Point>();
        [DataMember]
        public List<Curve> Curves { get; set; } = new List<Curve>();

        public Opening() { }
    }

    [DataContract]
    public class Loop
    {
        [DataMember]
        public double X { get; set; }
        public Loop() { }
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
        [DataMember]
        /// <summary>
        /// Актуально только для ЗД-5, 9 и 11 всегда горизонтальные и всегда наверху панели, 6 всегда горизонтальые, 7 всегда вертикальные
        /// </summary>
        public bool IsVertical { get; set; }
        public EmbeddedPart() { }
    }
    [DataContract]
    public class EmbeddedPart7
    {
        [DataMember]
        public double Y { get; set; }
        [DataMember]
        public double MinXAcad { get; set; }
        [DataMember]
        public double MaxXAcad { get; set; }
        public EmbeddedPart7() { }
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
