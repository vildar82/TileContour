using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

[assembly: CommandClass(typeof(vil.acad.TileContour.Commands))]

namespace vil.acad.TileContour
{
   public static class Commands
   {
      [CommandMethod("TileContour")]
      public static void TileContour()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         Editor ed = doc.Editor;

         // Jig отрисовки полилинии по задаваемым точкам.
         TileLineJig jigTest = new TileLineJig();
         PromptResult jigRes;
         bool status = true;
         do
         {
            jigRes = ed.Drag(jigTest);
            if (jigRes.Status == PromptStatus.OK)
            {
               // Добавление указанной точки
               jigTest.AddNewPromptPoint();
            }
            else if (jigRes.Status == PromptStatus.Cancel || jigRes.Status == PromptStatus.Error)
            {
               return;
            }
            else if (jigRes.Status == PromptStatus.Other)
            {
               status = false;
            }
         } while (status);

         // Добавление полилинии в чертеж.
         using (Transaction tr = db.TransactionManager.StartTransaction())
         {
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            Autodesk.AutoCAD.DatabaseServices.Polyline pl = new Autodesk.AutoCAD.DatabaseServices.Polyline();
            pl.SetDatabaseDefaults();
            for (int i = 0; i < jigTest.AllVertex.Count; i++)
            {
               Point3d pt3d = jigTest.AllVertex[i];
               Point2d pt2d = new Point2d(pt3d.X, pt3d.Y);
               pl.AddVertexAt(i, pt2d, 0, db.Plinewid, db.Plinewid);
            }
            pl.TransformBy(jigTest.UCS);
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            tr.Commit();
         }
      }
   }  
}