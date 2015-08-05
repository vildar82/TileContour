using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;

namespace vil.acad.TileContour
{
   public static class Options
   {
      static int _lenTile;
      static int _lenSeam;
      static RegistryKey _key;
      const string _regSubKey = @"Software\Vildar\TileContour";
      const string _regNameLenTile = "LenTile";
      const string _regNameLenSeam = "LenSeam";

      public static int LenTile
      {
         get { return _lenTile; }
         set
         {
            _lenTile = value;
            Save(_regNameLenTile, _lenTile);
         }
      }
      public static int LenSeam
      {
         get { return _lenSeam; }
         set
         {
            _lenSeam = value;
            Save(_regNameLenSeam, _lenSeam);
         }
      }

      static Options()
      {
         // Загрузка значений из реестра
         Load();
      }
      
      private static void Save(string valueName, int val)
      {
         // Запись значений в реестр.
         if (_key == null)
            _key = Registry.CurrentUser.CreateSubKey(_regSubKey);
         _key.SetValue(valueName, val, Microsoft.Win32.RegistryValueKind.DWord);
      }

      private static void Load()
      {         
         if (_key == null)
            _key = Registry.CurrentUser.CreateSubKey(_regSubKey);
         _lenTile = (int)_key.GetValue(_regNameLenTile, 288);
         _lenSeam = (int)_key.GetValue(_regNameLenSeam, 12);
      }
   }
}
