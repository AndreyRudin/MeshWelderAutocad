using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MeshWelderAutocad.Commands.Test
{
    internal partial class Command
    {
        [CommandMethod("InitSlabMaster")]
        public static void InitSlabMaster()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) 
                return;
            AddModifierHandlerToPolylinesWithXData(doc);
        }
        private static void AddModifierHandlerToPolylinesWithXData(Document doc)
        {
            List<string> plIds = new List<string>();
            List<string> handles = new List<string>();
            HashSet<ObjectId> slabPolylineIds = new HashSet<ObjectId>();
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in btr)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                    if (obj is Polyline polyline)
                    {
                        plIds.Add(polyline.Id.ToString());
                        handles.Add(polyline.Id.Handle.ToString());
                        ResultBuffer xData = polyline.GetXDataForApplication(appName);
                        if (xData != null)
                        {
                            XData parsedXData = ParseXDataJSON(xData);
                            //slabPolylineIds.Add(GetObjectIdByLong(parsedXData.SlabPolylineHandle));
                        }
                    }
                }
                tr.Commit();
            }
            //using (DocumentLock docLock = doc.LockDocument())
            //{
            //    using (Transaction tr = db.TransactionManager.StartTransaction())
            //    {
            //        foreach (var slabPolylineId in slabPolylineIds)
            //        {
            //            DBObject slabPolyline = tr.GetObject(slabPolylineId, OpenMode.ForWrite);
            //            slabPolyline.Modified += OnElementModified;
            //        }
            //        tr.Commit();
            //    }
            //}
        }
    }
}
