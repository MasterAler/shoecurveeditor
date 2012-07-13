using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows;

namespace ShoeCurveEditor
{
    /// <summary>
    /// Самодельный класс исключений для удобства
    /// </summary>
    [Serializable]
    public class CustomLogicException : Exception
    { //Поможет отлаживаться потом, можно усложнить если плохо пойдет
        public WTF FailureType;
        public enum WTF { NotConnected, ErrorCommand, Unhandled, EmptyData, IncorrectData, NotApplicable }

        public CustomLogicException(string reason) : base(reason) { FailureType = WTF.Unhandled; }
        public CustomLogicException(string reason, WTF type) : base(reason) { FailureType = type; }

        public CustomLogicException(string message, Exception inner) : base(message, inner) { }
        protected CustomLogicException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    /// <summary>
    /// Логика трехмерных операций над моделью и работа над данными
    /// </summary>
    class ModelLogicInteractor
    {   /*Заменитель делкама*/
        private Vector3D[] DataPoints;
        private Triangle[] Faces;
        private Vector3D[] XZ_Curve, Custom_Curve;
        private Vector3D[][] XY_Curve;
        private Point3D Rotation_CenterXZ, Rotation_CenterXY, Rotation_CenterCustom;
        private double InSec_Angle = 45;

        public Triangle[] GetFaces() { return Faces; }
        public Vector3D[] GetPoints() { return DataPoints; }

        public class Triangle
        {
            public int this[int index]
            {
                get 
                {
                    if ((index < 0) || (index > 2)) throw new CustomLogicException("Wrong index in Triangle!", CustomLogicException.WTF.IncorrectData);
                    return V[index];
                }
                set
                {
                    if ((index < 0) || (index > 2)) throw new CustomLogicException("Wrong index in Triangle!", CustomLogicException.WTF.IncorrectData);
                    V[index]=value;
                }
            }
            public int[] V;
            public Triangle() 
            {
                V = new int[3]; Neighbours = new int?[3]; Neighcount = 0;
                for (int i = 0; i < 3; i++) Neighbours[i] = null;
            }

            public int?[] Neighbours;
            public int Neighcount;

            public Vector3D norm;
            public Vector3D Norm(Vector3D[] source)
            {
                Vector3D p1 = source[V[1] - 1] - source[V[0] - 1]; //проверить номера
                Vector3D p2 = source[V[2] - 1] - source[V[1] - 1];
                norm = Vector3D.CrossProduct(p1, p2);
                norm.Normalize();
                return norm;
            }
        }

        public ModelLogicInteractor()
        {
            XY_Curve = new Vector3D[2][];
            maxz = 0; minz = 1000; max_norm_z_up = 0.9; max_norm_z_down = -0.8; min_norm_z_up = 0.8; min_norm_z_down = -0.9;
        }

        public void LoadFile(string filename)
        {
            int nv = 0; int nf = 0;
            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                if (line.IndexOf('#') > -1) continue;
                if (line.IndexOf('v') > -1) nv++;
                if (line.IndexOf('f') > -1) nf++;
            }
            file.Close();

            DataPoints = new Vector3D[nv];
            Faces = new Triangle[nf];

            nv = 0; nf = 0;
            file = new System.IO.StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                if (line.IndexOf('#') > -1) continue;
                if (line.IndexOf('v') > -1)
                {
                    string[] words = line.Split(' ');

                    DataPoints[nv].X = double.Parse(words[1], System.Globalization.NumberFormatInfo.InvariantInfo);
                    DataPoints[nv].Y = double.Parse(words[2], System.Globalization.NumberFormatInfo.InvariantInfo);
                    DataPoints[nv].Z = double.Parse(words[3], System.Globalization.NumberFormatInfo.InvariantInfo);
                    nv++;
                }
                if (line.IndexOf('f') > -1)
                {
                    Faces[nf] = new Triangle();
                    string[] words = line.Split(' ');
                    Faces[nf].V[0] = int.Parse(words[1]);
                    Faces[nf].V[1] = int.Parse(words[2]);
                    Faces[nf].V[2] = int.Parse(words[3]);
                    nf++;
                }
            }
            file.Close();

            //CalculateNeighboursQuick();

            Rotation_CenterXY = new Point3D(0, 0, 0);
            Rotation_CenterXZ = new Point3D(0, 0, 0);
            Rotation_CenterCustom = new Point3D(0, 0, 0);
            GenerateXYCurve();
        }

        #region NeighbourCounting

        public class EdgeComparer : IComparer<Edge>
        {
            int IComparer<Edge>.Compare(Edge x, Edge y)
            {
                Edge e1 = (Edge)x; Edge e2 = (Edge)y;
                if ((e1.P1 == e2.P1) && (e1.P2 == e2.P2)) return 0;

                if (e2.P1 > e1.P1) return 1;
                if (e2.P1 < e1.P1) return -1;
                if (e2.P2 > e1.P2) return 1;
                if (e2.P2 < e1.P2) return -1;
                throw new ArgumentException("OMG!! Sorcery!!");
            }
        }

        struct Edge
        {
            public int P1, P2; // P1>P2
            public int TrId;
        }

        private void CalculateNeighboursQuick()
        {
            Edge[] edges = new Edge[3 * Faces.Count()];
            for (int i = 0; i < Faces.Count(); i++)
            {
                edges[3 * i].TrId = i;
                edges[3 * i].P1 = Math.Max(Faces[i][0], Faces[i][1]);
                edges[3 * i].P2 = Math.Min(Faces[i][0], Faces[i][1]);

                edges[3 * i + 1].TrId = i;
                edges[3 * i + 1].P1 = Math.Max(Faces[i][2], Faces[i][1]);
                edges[3 * i + 1].P2 = Math.Min(Faces[i][2], Faces[i][1]);

                edges[3 * i + 2].TrId = i;
                edges[3 * i + 2].P1 = Math.Max(Faces[i][0], Faces[i][2]);
                edges[3 * i + 2].P2 = Math.Min(Faces[i][0], Faces[i][2]);
            }
            Array.Sort(edges, new EdgeComparer());
            int en=edges.Count();
            for (int i = 1; i < en; i+=2)
            {
                Faces[edges[i].TrId].Neighbours[Faces[edges[i].TrId].Neighcount] = edges[i-1].TrId;
                Faces[edges[i - 1].TrId].Neighbours[Faces[edges[i - 1].TrId].Neighcount] = edges[i].TrId;
                Faces[edges[i].TrId].Neighcount++;
                Faces[edges[i-1].TrId].Neighcount++;
            }
        }

        //---------------------------------------------------------------------------

        #endregion

        /// <summary>
        /// Сохраняет модель в файл
        /// </summary>
        /// <param name="filename">Имя файла</param>
        public void SaveToFile(string filename)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(filename,false);
            file.WriteLine("####\n#\n# OBJ File Generated by ShoeCurveEditor \n#\n####");
            StringBuilder title = new StringBuilder();
            title.AppendFormat("# Object {0} \n#\n# Vertices: {1}\n# Faces: {2}\n#\n####", System.IO.Path.GetFileName(filename), DataPoints.Count(), Faces.Count());
            file.WriteLine(title.ToString());
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < DataPoints.Count(); i++)
			{
                line.Clear();
                line.AppendFormat(System.Globalization.NumberFormatInfo.InvariantInfo,"v {0} {1} {2}", DataPoints[i].X, DataPoints[i].Y, DataPoints[i].Z);
                file.WriteLine(line.ToString());
			}
            title.Clear();
            title.AppendFormat("# {0} vertices\n", DataPoints.Count());
            file.WriteLine(title.ToString());
            for (int i = 0; i < Faces.Count(); i++)
            {
                line.Clear();
                line.AppendFormat(System.Globalization.NumberFormatInfo.InvariantInfo, "f {0} {1} {2}", Faces[i][0], Faces[i][1], Faces[i][2]);
                file.WriteLine(line.ToString());
            }
            title.Clear();
            title.AppendFormat("# {0} faces\n", Faces.Count());
            file.WriteLine(title.ToString());
            file.WriteLine("# End of File");
            file.Close();
        }

        struct EPair { public Vector3D P1, P2;}

        private EPair? FindPoints(int TrNumber, Vector3D R0, Vector3D N)
        {
            EPair epr = new EPair();
            int pc = 0;
            Vector3D a = new Vector3D();
            Vector3D b = new Vector3D();
            bool find_triangle = true;
            double t;
            Triangle tr = Faces[TrNumber];

            for (int i = 0; i < 3; i++)
            {
                a = R0 - DataPoints[tr.V[i] - 1];
                b = DataPoints[tr.V[(i + 1) % 3] - 1] - DataPoints[tr.V[i] - 1];
                if (Vector3D.DotProduct(b, N) != 0)
                {
                    t = Vector3D.DotProduct(a, N) / Vector3D.DotProduct(b, N);
                    if (t * (1 - t) > 0)
                    {
                        if (find_triangle)
                        {
                            find_triangle = false;
                            epr.P1 = (DataPoints[tr.V[i] - 1] + t * (DataPoints[tr.V[(i + 1) % 3] - 1] - DataPoints[tr.V[i] - 1]));
                            pc++;
                        }
                        else
                        {
                            epr.P2 = (DataPoints[tr.V[i] - 1] + t * (DataPoints[tr.V[(i + 1) % 3] - 1] - DataPoints[tr.V[i] - 1]));
                            pc++;
                        }
                    }

                }
            }
            if (pc==2) return epr;
            else return null;
        }

        private void GenerateXZCurveN()
        {
            List<Vector3D> lst = new List<Vector3D>();
            Vector3D R0 = new Vector3D(0, 0, 0);
            Vector3D N = new Vector3D(0, 1, 0);

            EPair? res = null; int tr0=-1; 
            for (int i = 0; i < Faces.Count(); i++)
			{
			    res=FindPoints(i, R0, N);
                if (res.HasValue)
                {
                    tr0 = i;
                    break;
                }
			}
            if (!res.HasValue) return;

            lst.Add(res.Value.P1);
            lst.Add(res.Value.P2);
            int tr_id;
            do
            {
                tr_id = tr0;
                for (int i = 0; i < 3; i++)
                {
                    if (!Faces[tr0].Neighbours[i].HasValue) continue;
                    res=FindPoints(Faces[tr0].Neighbours[i].Value, R0, N);
                    if (res.HasValue)
                    {
                        lst.Add(res.Value.P1);
                        lst.Add(res.Value.P2);
                        tr0 = Faces[tr0].Neighbours[i].Value;
                    }
                }
                //if (!res.HasValue) break;
            }
            while (tr0!=tr_id);

            XZ_Curve = lst.ToArray<Vector3D>();
        }

        private bool VectorsAreEqual(Vector3D v1, Vector3D v2)
        {
            const double Eps = 1e-5;
            return (Math.Abs(v1.X - v2.X) < Eps && Math.Abs(v1.Y - v2.Y) < Eps && Math.Abs(v1.Z - v2.Z) < Eps);
        }

        private Vector3D[] GenerateSortedSection(Vector3D R0, Vector3D N)
        {
            const double Eps = 1e-7;
            List<Vector3D> lst = new List<Vector3D>();   
            Vector3D a = new Vector3D();
            Vector3D b = new Vector3D();
            double t;
            //Find all triangles

            List<Edge> edg_lst = new List<Edge>();
            List<Vector3D> pts = new List<Vector3D>();

            foreach (Triangle tr in Faces)
            {
                int p1 = -42, p2 = -42;
                for (int i = 0; i < 3; i++)
                {
                    a = R0 - DataPoints[tr[i] - 1];
                    b = DataPoints[tr[(i + 1) % 3] - 1] - DataPoints[tr[i] - 1];
                    if (Vector3D.DotProduct(b, N) != 0)
                    {
                        t = Vector3D.DotProduct(a, N) / Vector3D.DotProduct(b, N);
                        if (t * (1 - t) >= 0)
                        {
                            Vector3D npt = DataPoints[tr[i] - 1] + t * (DataPoints[tr[(i + 1) % 3] - 1] - DataPoints[tr[i] - 1]);
                            int ind = -1;
                            for (int k = 0; k < pts.Count; k++)
                            {
                                Vector3D loc = pts[k];
                                if (Math.Abs(loc.X - npt.X) < Eps && Math.Abs(loc.Y - npt.Y) < Eps && Math.Abs(loc.Z - npt.Z) < Eps) { ind = k; break; }
                            }
                            if (ind == -1)
                            {
                                pts.Add(npt);
                                if (p1 >= 0) p2 = pts.Count - 1;
                                else p1 = pts.Count - 1;
                            }
                            else
                            {
                                if (p1 >= 0) p2 = ind;
                                else p1 = ind;
                            }
                        }
                    }
                }
                if (p1 >= 0)
                {
                    Edge ce = new Edge();
                    ce.P1 = p1; ce.P2 = p2;
                    edg_lst.Add(ce);
                }
            }

            if (edg_lst.Count == 0) return null;

            int chain_pt = edg_lst.First().P1;
            while (edg_lst.Count > 0)
            {
                int rem = -1;
                for (int i = 0; i < edg_lst.Count; i++)
                {
                    if (edg_lst[i].P1 == chain_pt)
                    {
                        lst.Add(pts[edg_lst[i].P2]);
                        chain_pt = edg_lst[i].P2;
                        rem = i;
                        break;
                    }
                    if (edg_lst[i].P2 == chain_pt)
                    {
                        lst.Add(pts[edg_lst[i].P1]);
                        chain_pt = edg_lst[i].P1;
                        rem = i;
                        break;
                    }
                }
                if (rem >= 0) edg_lst.RemoveAt(rem); else break;
                //if (rem >= 0) edg_lst.RemoveAt(rem);
                //else
                //{
                //    if (edg_lst.Count > 0) chain_pt = edg_lst.First().P1;
                //    else break;
                //}
            }

            return lst.ToArray<Vector3D>();
        }

        private void GenerateXZCurve()
        {          
            Vector3D R0 = new Vector3D(0, 0, 0);
            Vector3D N = new Vector3D(0, 1, 0);
            XZ_Curve = GenerateSortedSection(R0, N);
        }


        double maxz,  minz ,  max_norm_z_up ,  max_norm_z_down,  min_norm_z_up ,  min_norm_z_down;

        public void CnangeXYparams(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(ParamSettingBox)) return;
            ParamSettingBox box = (ParamSettingBox)sender;
            maxz = box.MaxZ;
            minz = box.MinZ;
            max_norm_z_down = box.Max_ZNorm_Down;
            max_norm_z_up = box.Max_ZNorm_Up;
            min_norm_z_down = box.Min_ZNorm_Down;
            min_norm_z_up = box.Min_ZNorm_Up;
            GenerateXYCurve();
            CallForReDraw();
        }

        private void GenerateXYCurve()
        {
            List<Vector3D> lst1 = new List<Vector3D>();
            List<Vector3D> lst2 = new List<Vector3D>();

            XY_Curve = new Vector3D[2][];

            foreach (Triangle tr in Faces)
            {
                tr.Norm(DataPoints);
                if ((DataPoints[tr.V[0] - 1].Z < minz) && (min_norm_z_down <= tr.norm.Z) && (tr.norm.Z <= max_norm_z_down))
                {
                    lst1.Add((DataPoints[tr.V[0] - 1] + DataPoints[tr.V[1] - 1] + DataPoints[tr.V[2] - 1]) / 3);
                }
                if ((DataPoints[tr.V[0] - 1].Z > maxz) && (min_norm_z_up <= tr.norm.Z) && (tr.norm.Z <= max_norm_z_up))
                {
                    lst2.Add((DataPoints[tr.V[0] - 1] + DataPoints[tr.V[1] - 1] + DataPoints[tr.V[2] - 1]) / 3);
                }
            }

            XY_Curve[0] = lst1.ToArray<Vector3D>();
            XY_Curve[1] = lst2.ToArray<Vector3D>();
        }


        private void GenerateCustomCurve()
        {
            Vector3D R0 = (Vector3D)Rotation_CenterXZ;
            double alpha = Math.PI * (InSec_Angle - 90) / 180; //проверить!!!
            Vector3D N = new Vector3D(Math.Cos(alpha), 0, Math.Sin(alpha));
            N.Normalize();
            Custom_Curve = GenerateSortedSection(R0, N);
        }

        /// <summary>
        ///  Должно смещать колодку так, чтобы XZ-сечение существовало
        /// </summary>
        public void input_move()
        {
            int count = DataPoints.Count();

            double max_X = DataPoints[0].X;
            double min_X = DataPoints[0].X;
            double max_Y = DataPoints[0].Y;
            double min_Y = DataPoints[0].Y;
            double max_Z = DataPoints[0].Z;
            double min_Z = DataPoints[0].Z;

            for (int i = 1; i < count - 1; i++)
            {
                max_X = Math.Max(max_X, DataPoints[i].X);
                min_X = Math.Min(min_X, DataPoints[i].X);
                max_Y = Math.Max(max_Y, DataPoints[i].Y);
                min_Y = Math.Min(min_Y, DataPoints[i].Y);
                max_Z = Math.Max(max_Z, DataPoints[i].Z);
                min_Z = Math.Min(min_Z, DataPoints[i].Z);
            }

            for (int j = 0; j < DataPoints.Count() - 1; j++)
            {
                DataPoints[j].X -= min_X;
                DataPoints[j].Y = DataPoints[j].Y - min_Y - (max_Y - min_Y) / 2;
                DataPoints[j].Z -= min_Z;
            }
        }
                
        public void MoveKolodka(Object sender, CurvePlane.MoveFigureEventArgs e)
        {
            if ((sender.GetType() != typeof(CurvePlane)) && (sender.GetType() != typeof(DoubleCurve))) return;
            BasePlane.PlaneType histype = ((BasePlane)sender).Viewtype;

            switch (histype)
            {
                case (BasePlane.PlaneType.XZ):
                    {
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j].X += e.DX;
                            DataPoints[j].Z += e.DY;
                        }
                        //for (int j = 0; j < XY_Curve.Count() - 1; j++)
                        //{
                        //    XY_Curve[0][j].X += e.DX;
                        //    XY_Curve[0][j].Z += e.DY;
                        //    XY_Curve[1][j].X += e.DX;
                        //    XY_Curve[1][j].Z += e.DY;

                        //}
                        break;
                    }
                case (BasePlane.PlaneType.XY):
                    {
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j].X += e.DX;
                            DataPoints[j].Y += e.DY;
                        }
                        for (int j = 0; j < XY_Curve.Count() - 1; j++)
                        {
                            XY_Curve[0][j].X += e.DX;
                            XY_Curve[0][j].Y += e.DY;
                            if (XY_Curve[1] == null) continue;
                            XY_Curve[1][j].X += e.DX;
                            XY_Curve[1][j].Y += e.DY;

                        }
                        break;
                    }
                case (BasePlane.PlaneType.Custom):
                    {
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j].Y += e.DX;
                            DataPoints[j].Z += e.DY;
                        }
                        //for (int j = 0; j < XY_Curve.Count() - 1; j++)
                        //{
                        //    XY_Curve[0][j].Y += e.DX;
                        //    XY_Curve[0][j].Z += e.DY;
                        //    XY_Curve[1][j].Y += e.DX;
                        //    XY_Curve[1][j].Z += e.DY;

                        //}
                        break;
                    }

                default: throw new CustomLogicException("Holy crap!", CustomLogicException.WTF.Unhandled);
            }
            CallForReDraw();
        }

        public void RotationCenterMove(Object sender, CurvePlane.RotationCenterEventArgs e)
        {
            if ((sender.GetType() != typeof(CurvePlane)) && (sender.GetType() != typeof(DoubleCurve))) return;
            BasePlane.PlaneType histype = ((BasePlane)sender).Viewtype;
            switch (histype)
            {
                case (BasePlane.PlaneType.XZ):
                    {
                        InSec_Angle = e.Angle;
                        Rotation_CenterXZ += new Vector3D(e.CenterDPos.X, 0, e.CenterDPos.Y);
                        break;
                    }
                case (BasePlane.PlaneType.XY):
                    {
                        Rotation_CenterXY += new Vector3D(e.CenterDPos.X, e.CenterDPos.Y, 0);
                        break;
                    }
                case (BasePlane.PlaneType.Custom):
                    {
                        Rotation_CenterCustom += new Vector3D(0, e.CenterDPos.X, e.CenterDPos.Y);
                        break;
                    }
                default: throw new CustomLogicException("Holy crap!", CustomLogicException.WTF.Unhandled);
            }
            CallForReDraw();
        }

        public void RotateModel(Object sender, CurvePlane.RotateFigureEventArgs e)
        {
            if ((sender.GetType() != typeof(CurvePlane)) && (sender.GetType() != typeof(DoubleCurve))) return;
            BasePlane.PlaneType histype = ((BasePlane)sender).Viewtype;

            Matrix3D T = new Matrix3D(1, 0, 0, 0,
                                       0, 1, 0, 0,
                                       0, 0, 1, 0,
                                       0, 0, 0, 1);
            Quaternion TT = new Quaternion();
            switch (histype)
            {
                case (BasePlane.PlaneType.XZ):
                    {
                        double alf = -Math.PI * e.RotationAngle / 360;
                        TT = new Quaternion(0, Math.Sin(alf), 0, Math.Cos(alf));
                        T.RotateAt(TT, Rotation_CenterXZ);
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j] = (Vector3D)((Point3D)DataPoints[j] * T);
                        }
                        //for (int j = 0; j < XY_Curve[0].Count() - 1; j++) XY_Curve[0][j] *= T;
                        //for (int j = 0; j < XY_Curve[1].Count() - 1; j++) XY_Curve[1][j] *= T;

                        break;

                    }
                case (BasePlane.PlaneType.XY):
                    {

                        double alf = Math.PI * e.RotationAngle / 360;
                        TT = new Quaternion(0, 0, Math.Sin(alf), Math.Cos(alf));
                        T.RotateAt(TT, (Point3D)Rotation_CenterXY);
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j] = (Vector3D)((Point3D)DataPoints[j] * T);
                        }
                        for (int j = 0; j < XY_Curve[0].Count() - 1; j++) XY_Curve[0][j] *= T;
                        for (int j = 0; j < XY_Curve[1].Count() - 1; j++) XY_Curve[1][j] *= T;
                        break;
                    }

                case (BasePlane.PlaneType.Custom):
                    {

                        double alf = Math.PI * e.RotationAngle / 360;
                        TT = new Quaternion(Math.Sin(alf), 0, 0, Math.Cos(alf));
                        T.RotateAt(TT, (Point3D)Rotation_CenterCustom);
                        for (int j = 0; j < DataPoints.Count() - 1; j++)
                        {
                            DataPoints[j] = (Vector3D)((Point3D)DataPoints[j] * T);
                        }
                        for (int j = 0; j < XY_Curve[0].Count() - 1; j++) XY_Curve[0][j] *= T;
                        for (int j = 0; j < XY_Curve[1].Count() - 1; j++) XY_Curve[1][j] *= T;
                        break;
                    }
                default: throw new CustomLogicException("Holy crap!", CustomLogicException.WTF.Unhandled);
            }
            CallForReDraw();
        }

        //------------------------Наследство от делкама-----------------------------------//

        public event EventHandler DataChanged;

        /// <summary>
        ///  Событие изменения значений точек
        /// </summary>
        /// <param name="e">event parameter</param>
        protected virtual void OnDataChanged(EventArgs e)
        {
            EventHandler handler = DataChanged;
            if (handler != null) { handler(this, e); }
        }
        private void CallForReDraw() { OnDataChanged(null); }

        public Point3D[][] GetXXCurve(ShoeCurveEditor.BasePlane.PlaneType xxType)
        {
        	
         Point3D[][] Fakedata = new Point3D[1][];
         Fakedata[0] = new Point3D[2];
         Fakedata[0][0]= new Point3D(-200,-200,-200);
         Fakedata[0][1]= new Point3D(500,200,300);
          
         switch (xxType)
            
            {
                case BasePlane.PlaneType.XZ:
                    {
                        GenerateXZCurve();
                        if (XZ_Curve == null) return null;

                        int N = XZ_Curve.Count();
                        Point3D[][] data = new Point3D[1][];
                        data[0] = new Point3D[N];
                        for (int i = 0; i < N; i++)
                        {
                            data[0][i] = (Point3D)XZ_Curve[i];
                        }
                        return data;
                    }
                case BasePlane.PlaneType.XY:
                    {
                        Point3D[][] data = null;

                        GenerateXYCurve();
                        int N1 = XY_Curve[0].Count();
                        int N2 = XY_Curve[1].Count();
                        data = new Point3D[2][];
                        data[0] = new Point3D[N1];
                        data[1] = new Point3D[N2];

                        for (int i = 0; i < N1; i++)
                        {
                            data[0][i] = (Point3D)XY_Curve[0][i];
                        }
                        for (int i = 0; i < N2; i++)
                        {
                            data[1][i] = (Point3D)XY_Curve[1][i];
                        }
                        return data;
                    }
                case BasePlane.PlaneType.Custom:
                    {

                        GenerateCustomCurve();
                        if (Custom_Curve == null) return null;
                        int N = Custom_Curve.Count();
                        // int N2 = 0;
                        Point3D[][] data = new Point3D[1][];
                        data[0] = new Point3D[N];

                        for (int i = 0; i < N; i++)
                        {
                            data[0][i] = (Point3D)Custom_Curve[i];
                        }
                        //   for (int i = 0; i < N2; i++)
                        //   {
                        //        data[1][i] = new Point(Custom_Curve[1][i].Y, Custom_Curve[1][i].Z);
                        //   }                    
                        return data;
                    }

                case BasePlane.PlaneType.Section:
                    {
                        GenerateCustomCurve();
                        int N = Custom_Curve.Count();
                        // int N2 = 0;
                        Point3D[][] data = new Point3D[1][];
                        data[0] = new Point3D[N];

                        Matrix3D T = new Matrix3D(1, 0, 0, 0,
                               0, 1, 0, 0,
                               0, 0, 1, 0,
                               0, 0, 0, 1);
                        Quaternion TT = new Quaternion();
                        double alf = Math.PI * (InSec_Angle - 90) / 180; //проверить!!!
                        TT = new Quaternion(0, Math.Sin(alf), 0, Math.Cos(alf));
                        T.RotateAt(TT, (Point3D)Rotation_CenterXZ);


                        for (int i = 0; i < N; i++)
                        {
                            Custom_Curve[i] *= T;
                            data[0][i] = (Point3D)Custom_Curve[i];
                        }
                        //   for (int i = 0; i < N2; i++)
                        //   {
                        //        data[1][i] = new Point(Custom_Curve[1][i].Y, Custom_Curve[1][i].Z);
                        //   }                    
                        return data;

                    }
                default:
                    {
                        throw new CustomLogicException("Sorcery", CustomLogicException.WTF.Unhandled);
                    }
            }
        }

    }

}