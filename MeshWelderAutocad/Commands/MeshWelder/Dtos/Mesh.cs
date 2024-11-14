using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad
{
    public class Mesh
    {
        public string DwgName { get; set; }
        public string RevitModelName { get; set; }
        public string PanelName { get; set; }
        public string PanelCode { get; set; }
        public List<MyRebar> Rebars { get; set; } = new List<MyRebar>();
    }
}
