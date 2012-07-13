using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Media3D;
using System.ComponentModel;

namespace ShoeCurveEditor
{
    /// <summary>
    /// Статический класс, осуществляющий все преобразования между файлами
    /// </summary>
    static class ModelFileConverter
    {
        public struct FileNames { public string _infile, _outfile; }
        struct Tri
        {
            public int[] V;
        }
        struct Tri_D
        {
            public double[] V;
        }
        /// <summary>
        /// Конвертирует STL-файл в OBJ-файл
        /// </summary>
        /// <param name="stlfilename">имя STL-файла</param>
        /// <param name="objfilename">имя OBJ-файла</param>
        /// <returns>Получилось ли</returns>
        public static bool ConvertBinSTL2OBJ(string stlfilename, string objfilename, BackgroundWorker worker)
        {
            if (!File.Exists(stlfilename)) return false;

            BinaryReader reader = new BinaryReader(File.Open(stlfilename, FileMode.Open));
            try
            {
                float x, y, z;
                reader.ReadBytes(80);
                int N=reader.ReadInt32();
                SortedList<Double, Point3D> pt_lst = new SortedList<double, Point3D>(N);
                List<Tri_D> faces_d = new List<Tri_D>();
                for (int i = 0; i < N; i++)
                {
                    reader.ReadBytes(12);
                    Tri_D tmp_d;
                    tmp_d.V = new double[3];
                    for (int k = 0; k < 3; k++)
                    {
                        x = reader.ReadSingle();
                        y = reader.ReadSingle();
                        z = reader.ReadSingle();
                        Point3D cur_pt = new Point3D(x, y, z);
                        double ln = ((Vector3D)cur_pt).LengthSquared;
                        int id_match = pt_lst.IndexOfKey(ln);
                        if (id_match == -1) pt_lst.Add(ln, cur_pt);
                        tmp_d.V[k] = ln;
                    }
                    faces_d.Add(tmp_d);
                    reader.ReadBytes(2);
                    worker.ReportProgress(100 * i / N);
                }
                //----
                List<Tri> faces = new List<Tri>(faces_d.Count);
                int id;
                for (int i = 0; i < faces_d.Count; i++)
                {
                    Tri tmp;
                    tmp.V = new int[3];
                    id = pt_lst.IndexOfKey(faces_d[i].V[0]);
                    tmp.V[0] = id + 1;
                    id = pt_lst.IndexOfKey(faces_d[i].V[1]);
                    tmp.V[1] = id + 1;
                    id = pt_lst.IndexOfKey(faces_d[i].V[2]);
                    tmp.V[2] = id + 1;
                    faces.Add(tmp);
                }
                PrintObjDataToFile(objfilename, System.IO.Path.GetFileName(objfilename), pt_lst.Values, faces);
                return true;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Convertion failed!! :( Error:" + e.Message);
                return false;
            }
            finally
            {
                reader.Close();
            }
        }
        //--------------------------------------------------------------------------------
        /// <summary>
        /// Конвертирует STL-файл в OBJ-файл
        /// </summary>
        /// <param name="stlfilename">имя STL-файла</param>
        /// <param name="objfilename">имя OBJ-файла</param>
        /// <returns>Получилось ли</returns>
        public static bool ConvertASCII_STL2OBJ(string stlfilename, string objfilename, BackgroundWorker worker)
        {
            if (!File.Exists(stlfilename)) return false;

            StreamReader reader = new StreamReader(File.Open(stlfilename, FileMode.Open));
            try
            {
                double x, y, z;
                string name;
                List<Tri_D> faces_d = new List<Tri_D>();
                string line; int N = 0;
                //----Как-то так, топорно, считаем длину количество треугольников---
                reader.ReadLine();
                while ((line = reader.ReadLine()).IndexOf("endsolid") == -1)
                {   //цикл развернуть можно, но ладно уж
                    for (int i = 0; i < 6; i++) reader.ReadLine();
                    N++;
                }
                reader.Close();
                //-------
                SortedList<Double, Point3D> pt_lst = new SortedList<double, Point3D>(N);
                reader = new StreamReader(File.Open(stlfilename, FileMode.Open));
                int ii = 0;
                name = reader.ReadLine();
                name = name.Remove(0, 6);
                while ((line = reader.ReadLine()).IndexOf("endsolid") == -1)
                {
                    reader.ReadLine();
                    Tri_D tmp_d;
                    tmp_d.V = new double[3];
                    for (int k = 0; k < 3; k++)
                    {
                        line = reader.ReadLine();
                        line = line.Trim();
                        line = line.Remove(0, 6);
                        line = line.Trim();
                        string[] words = line.Split(' ');
                        if (!Double.TryParse(words[0], System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out x))
                            throw new CustomLogicException("Full of %@&*$! ", CustomLogicException.WTF.IncorrectData);
                        if (!Double.TryParse(words[1], System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out y))
                            throw new CustomLogicException("Full of %@&*$! ", CustomLogicException.WTF.IncorrectData);
                        if (!Double.TryParse(words[2], System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out z))
                            throw new CustomLogicException("Full of %@&*$! ", CustomLogicException.WTF.IncorrectData);
                        Point3D cur_pt = new Point3D(x, y, z);
                        double ln = ((Vector3D)cur_pt).LengthSquared;
                        int id_match = pt_lst.IndexOfKey(ln);
                        if (id_match == -1) pt_lst.Add(ln, cur_pt);
                        tmp_d.V[k] = ln;
                    }
                    faces_d.Add(tmp_d);
                    reader.ReadLine();
                    reader.ReadLine();
                    ii++; worker.ReportProgress(100 * ii / N);
                }
                //-----------------
                List<Tri> faces = new List<Tri>(faces_d.Count);
                int id;
                for (int i = 0; i < faces_d.Count; i++)
                {
                    Tri tmp;
                    tmp.V = new int[3]; 
                    id = pt_lst.IndexOfKey(faces_d[i].V[0]);
                    tmp.V[0] = id + 1;
                    id = pt_lst.IndexOfKey(faces_d[i].V[1]);
                    tmp.V[1] = id + 1;
                    id = pt_lst.IndexOfKey(faces_d[i].V[2]);
                    tmp.V[2] = id + 1;
                    faces.Add(tmp);
                }
                PrintObjDataToFile(objfilename, name, pt_lst.Values, faces);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Convertion failed!! :( Error:" + ex.Message);
                return false;
            }
            finally
            {
                reader.Close();
            }
        }
        //----------------------------internal functions----------------------------------
        private static void PrintObjDataToFile(string objfilename, string modelname, IList<Point3D> vertexes, List<Tri> faces)
        {
            StreamWriter writer = new StreamWriter(objfilename,false);
            writer.WriteLine("####\n#\n# OBJ File Generated by ShoeCurveEditor \n#\n####");
            StringBuilder title = new StringBuilder();
            title.AppendFormat("# Object {0} \n#\n# Vertices: {1}\n# Faces: {2}\n#\n####", modelname, vertexes.Count, faces.Count);
            writer.WriteLine(title.ToString());
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < vertexes.Count; i++)
            {
                line.Clear();
                line.AppendFormat(System.Globalization.NumberFormatInfo.InvariantInfo, "v {0} {1} {2}", vertexes[i].X, vertexes[i].Y, vertexes[i].Z);
                writer.WriteLine(line.ToString());
            }
            title.Clear();
            title.AppendFormat("# {0} vertices\n", vertexes.Count);
            writer.WriteLine(title.ToString());
            for (int i = 0; i < faces.Count; i++)
            {
                line.Clear();
                line.AppendFormat(System.Globalization.NumberFormatInfo.InvariantInfo, "f {0} {1} {2}", faces[i].V[0], faces[i].V[1], faces[i].V[2]);
                writer.WriteLine(line.ToString());
            }
            title.Clear();
            title.AppendFormat("# {0} faces\n", faces.Count);
            writer.WriteLine(title.ToString());
            writer.WriteLine("# End of File");
            writer.Close();
            writer.Close();
        }
    }
}
