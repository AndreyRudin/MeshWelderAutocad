using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings
{
    internal class Command
    {
        [CommandMethod("ChangeSettingsDNS")]
        public static void ChangeSettingsDNS()
        {
            try
            {
                ViewModel viewModel = new ViewModel();
                viewModel.View.ShowDialog();
            }
            catch(CustomException e)
            {
                //
            }
            catch(System.Exception e)
            {
                //
            }
        }
    }
}
