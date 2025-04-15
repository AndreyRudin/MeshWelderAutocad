using Autodesk.AutoCAD.Runtime;
using MeshWelderAutocad.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                MessageBox.Show(e.ToString(),"Ошибка");
            }
            catch(System.Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace, "Системная ошибка");
            }
        }
    }
}
