using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows;

namespace ShoeCurveEditor
{
    public static class CurveSorter
    {
        [DllImport("curvesorter.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void SortCurve(ref double xarr,ref double yarr,ref double zarr, int count);

        /// <summary>
        /// Пытается сортировать точки кривой
        /// </summary>
        /// <param name="points">массив точек</param>
        public static void SortCurvePoints(ref Point3D[] points)
        {
            int count=points.Count();
            double[] ar1 = new double[count];
            double[] ar2 = new double[count];
            double[] ar3 = new double[count];
            for (int i = 0; i < points.Count(); i++)
            {
                ar1[i] = points[i].X;
                ar2[i] = points[i].Y;
                ar3[i] = points[i].Z;
            }
            try { SortCurve(ref ar1[0], ref ar2[0], ref ar3[0], count); }
            catch (Exception e) { MessageBox.Show("Sorting failed(( Reason:\n " + e.Message); return; }
            for (int i = 0; i < points.Count(); i++)
            {
                points[i].X = ar1[i];
                points[i].Y = ar2[i];
                points[i].Z = ar3[i];
            }
        }
    }
}
