using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace vil.acad.TileContour
{
   public class TileLineJig : DrawJig
   {
      Point3dCollection _allVertex = new Point3dCollection();
      Point3d _lastVertex;

      public TileLineJig() { }
      ~TileLineJig() { }

      public Point3d LastVertex
      {
         get { return _lastVertex; }
         set { _lastVertex = value; }
      }
      public Point3dCollection AllVertex
      {
         get { return _allVertex; }
      }
      public Matrix3d UCS
      {
         get { return Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem; }
      }

      // Добавление указанной пользователем точки
      public void AddNewPromptPoint()
      {
         if (_allVertex.Count == 0)
         {
            // Первую указанную точку добавляем как первую вершину полилинии.
            _allVertex.Add(_lastVertex);
         }
         else
         {
            // Обработка последующих точек.
            Point3d pt1 = _allVertex[_allVertex.Count - 1]; // Последняя вершина полилинии.
            // Ортогональная точка по длинному растоянию от последней вершины полилинии к текущей точке указания.
            _lastVertex = GetPointOrhto(pt1, _lastVertex);
            // Получение точек для вершин полилинии от последней точки к текущей указанной.
            Point3dCollection _tempNewSectionPts = GetContourVertex(pt1, _lastVertex);
            // Добавление этих точек в список вершин полилинии.
            foreach (Point3d pt in _tempNewSectionPts)
               _allVertex.Add(pt);
         }
      }

      protected override bool WorldDraw(WorldDraw draw)
      {
         WorldGeometry geo = draw.Geometry;
         if (geo != null)
         {
            geo.PushModelTransform(UCS); // Не очень понятно, что это.

            if (_allVertex.Count != 0)
            {
               // Копирование точек полилинии во временную колекцию
               Point3dCollection tempAllVertex = new Point3dCollection();
               foreach (Point3d pt in _allVertex)
                  tempAllVertex.Add(pt);
               // Последняя вершина полилинии.
               Point3d pt1 = _allVertex[_allVertex.Count - 1];
               // Точка отрогональная по длинной стороне к текущей указанной.
               Point3d ptNew = GetPointOrhto(pt1, _lastVertex);
               // Получение вершин для полилинии текущего участка.
               Point3dCollection _tempNewSectionPts = GetContourVertex(pt1, ptNew);
               // Добавление вершин последнего участка к остальным.
               if (_tempNewSectionPts.Count > 0)
               {
                  foreach (Point3d pt in _tempNewSectionPts)
                     tempAllVertex.Add(pt);
               }
               // Отрисовка полилинии.
               geo.Polyline(tempAllVertex, Vector3d.ZAxis, IntPtr.Zero);
            }
            geo.PopModelTransform(); // Что это?
         }
         return true;
      }

      protected override SamplerStatus Sampler(JigPrompts prompts)
      {
         JigPromptPointOptions prOpt = new JigPromptPointOptions("\nУкажите точку");
         prOpt.Keywords.Add("Tile" + Options.LenTile, "Плитка" + Options.LenTile);
         prOpt.Keywords.Add("Seam" + Options.LenSeam,"Шов" + Options.LenSeam);
         //prOpt.AppendKeywordsToMessage = true;
         prOpt.UserInputControls = UserInputControls.AcceptOtherInputString;

         PromptPointResult prRes = prompts.AcquirePoint(prOpt);
         if (prRes.Status == PromptStatus.Error || prRes.Status == PromptStatus.Cancel)
         {
            return SamplerStatus.Cancel;
         }
         else if (prRes.Status == PromptStatus.Keyword)
         {
            if (prRes.StringResult.StartsWith("Tile"))
               Options.LenTile = GetLenPrompt(prRes.StringResult, Options.LenTile);
            else if (prRes.StringResult.StartsWith("Seam"))
               Options.LenSeam = GetLenPrompt(prRes.StringResult, Options.LenSeam);
         }
         else
         {
            _lastVertex = prRes.Value.TransformBy(UCS.Inverse());
         }
         return SamplerStatus.OK;
      }
      private int GetLenPrompt(string msg, int defaultVal)
      {
         int res = defaultVal;
         var prOpt = new PromptIntegerOptions(msg);
         prOpt.AllowZero = false;
         prOpt.AllowNegative = false;
         prOpt.DefaultValue = defaultVal;
         prOpt.UseDefaultValue = true;
         var resPrompt = Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(prOpt);
         if (resPrompt.Status == PromptStatus.OK)
            res = resPrompt.Value;
         return res;
      }

      private Point3dCollection GetContourVertex(Point3d p1, Point3d p2)
      {
         // Возвращает массив точек контура полилинии плитки.
         Point3dCollection pts = new Point3dCollection();
         var vec = p2 - p1;
         var vecSingle = vec.GetNormal();
         var vecSinglePerpend = vecSingle.GetPerpendicularVector();
         double len = vec.Length;//длина участка

         double curLen = len - Options.LenTile;
         if (curLen >= 0)
         {
            // Первый сегент - первая плитка 
            pts.Add(p1);
            _lastVertex = p1 + vecSingle * Options.LenTile;
            pts.Add(_lastVertex);

            // Второй и последующие сегменты
            do
            {
               curLen = GetTileSection(pts, vecSingle, vecSinglePerpend, curLen);
            } while (curLen > 0);
         }
         return pts;
      }

      private double GetTileSection(Point3dCollection pts, Vector3d vecSingle, Vector3d vecSinglePerpend, double curLen)
      {
         curLen = curLen - (Options.LenTile + Options.LenSeam);
         if (curLen >= 0)
         {
            // Первая точка шва
            _lastVertex = GetPoint(_lastVertex, vecSinglePerpend,Options.LenSeam);
            pts.Add(_lastVertex);
            // Вторая точка шва
            _lastVertex = GetPoint(_lastVertex, vecSingle, Options.LenSeam);
            pts.Add(_lastVertex);
            // Первая точка плитки
            _lastVertex = GetPoint(_lastVertex, vecSinglePerpend, -Options.LenSeam);
            pts.Add(_lastVertex);
            // Вторая точка плитки
            _lastVertex = GetPoint(_lastVertex, vecSingle, Options.LenTile);
            pts.Add(_lastVertex);
         }
         return curLen;
      }

      private Point3d GetPoint(Point3d p1, Vector3d vecSingle, int len)
      {
         // Точка в заданном направлении с заданной длиной
         return p1 + vecSingle * len;
      }

      private Point3d GetPointOrhto(Point3d pt1, Point3d ptNew)
      {
         Point3d ptRes;
         if (Math.Abs(ptNew.X - pt1.X) > Math.Abs(ptNew.Y - pt1.Y))
            ptRes = new Point3d(ptNew.X, pt1.Y, ptNew.Z);
         else
            ptRes = new Point3d(pt1.X, ptNew.Y, ptNew.Z);
         return ptRes;
      }
   }
}
