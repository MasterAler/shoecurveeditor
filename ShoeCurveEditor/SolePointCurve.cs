using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
//using System.Drawing;
using System.Windows.Input;
using System.Windows;

namespace ShoeCurveEditor
{
    /// <summary>
    ///  Класс задумывался как <c>lightweight-версия</c> для отображения всяческих
    ///  самодельно создаваемых кривых из точек.
    /// </summary>
    abstract class PointCurve
    {
        private SectionSynchronizer syncer;
        private Path UserPath;
        // -----------настройки внешнего вида------------------
        private SolidColorBrush ptColor;
        private SolidColorBrush lnColor;
        private SolidColorBrush mrkColor; 
        protected const double PointRadius = 5;
        //---------------------------------------
        public enum DrawMode { Points, Lines, Curves }
        protected Canvas DrawingSurface;
        protected List<Point3D> DataPoints;
        protected List<Point> DrawPoints;
        protected List<Ellipse> SurfaceShapes;
        private bool isvis;
        private DrawMode dMode;

        #region ViewProperties
        //---------------------------------------
        internal DrawMode DrawingMode
        {
            get { return dMode; }
            set { dMode = value; }
        }

        protected SolidColorBrush PointColor
        {
            get { return ptColor; }
            set { ptColor = value; }
        }

        public SolidColorBrush LineColor
        {
            get { return lnColor; }
            set { lnColor = value; }
        }

        protected bool Visible
        {
            get { return isvis; }
            set { isvis = value; }
        }

        private int iheel;

        public int iHeel
        {
            get { return iheel; }
            set { iheel = value; }
        }
        #endregion

        public PointCurve(Canvas outsurface)
        {
            DrawingSurface = outsurface;
            ptColor = System.Windows.Media.Brushes.ForestGreen;
            lnColor = System.Windows.Media.Brushes.ForestGreen;
            mrkColor = System.Windows.Media.Brushes.Red;
            DataPoints = new List<Point3D>();
            DrawPoints = new List<Point>();
            SurfaceShapes = new List<Ellipse>();
            isvis = true;
            dMode = DrawMode.Points;
            UserPath = new Path();
            iheel = -1;
        }

        private const double DEps = 1E-7;

        /// <summary>
        ///  Сравнивает 2 точки. Есть встроенные методы Equals() у всех типов, но
        ///  там черт его знает, когда они отработают, а когда нет. А если написать руками, то
        ///  точность регулируется и не надо отлаживаться в случае нелосмотра долго.
        /// </summary>
        /// <param name="pt1">1я точка</param>
        /// <param name="pt2">2я точка</param>
        /// <returns></returns>
        protected bool PointsAreEquals(Point3D pt1, Point3D pt2)
        {
            Vector3D v1 = (Vector3D)pt1;
            Vector3D v2 = (Vector3D)pt2; ;
            return Math.Abs(v1.LengthSquared - v2.LengthSquared) <= DEps;
        }

        protected bool PointsAreEquals(Point pt1, Point pt2)
        {
            return ((pt1.X == pt2.X) && (pt1.Y == pt2.Y));
        }

        private void FillVisualData()
        {
            Ellipse elps;
            for (int i = 0; i < DrawPoints.Count; i++)
            {
                elps = new Ellipse();
                Canvas.SetLeft(elps, DrawPoints[i].X - PointRadius);
                Canvas.SetTop(elps, DrawPoints[i].Y - PointRadius);
                Canvas.SetZIndex(elps, 1);
                elps.Height = 2 * PointRadius; elps.Width = 2 * PointRadius;
                if (i == iheel) elps.Stroke = mrkColor;
                else elps.Stroke = PointColor;
                elps.Fill = PointColor;
                elps.Tag = i;
                elps.MouseLeftButtonDown += new MouseButtonEventHandler(PointDeleter);
                elps.IsHitTestVisible = true;
                SurfaceShapes.Add(elps);
            }
        }

        /// <summary>
        /// Сугубо обработчик удаления точек
        /// </summary>
        /// <param name="sender">Какой эллипс удаляем</param>
        /// <param name="e">Параметры, как положено</param>
        private void PointDeleter(object sender, MouseButtonEventArgs e)
        {
            int tg = (int)((Ellipse)sender).Tag;
            if (Keyboard.IsKeyDown(Key.LeftAlt)) RemoveAt(tg);
            DrawCurve();
        }

        /// <summary>
        /// Своего рода дубль с функцией-аналогом из класса BasePlane, но
        /// так больше шансов не запутаться во взаимодействии классов.
        /// </summary>
        private void GeneratePath()
        {
            if (dMode==DrawMode.Points) 
            {
                UserPath=null;
                return;
            }
            int size = DrawPoints.Count();
            PathFigure pfg = new PathFigure();
            pfg.StartPoint = DrawPoints[0];
            UserPath = new System.Windows.Shapes.Path();
            PathGeometry Curve = new PathGeometry();

            PathSegmentCollection segments = new PathSegmentCollection(size);
            if (dMode == DrawMode.Lines)
            {
                LineSegment[] lsg = new LineSegment[size];
                for (int i = 0; i < size; i++)
                {
                    lsg[i] = new LineSegment(DrawPoints[i], true);
                    segments.Add(lsg[i]);
                }
                segments.Add(lsg[0]);
            }
            else
            {
                PointCollection beziercollection = new PointCollection(size);
                Point[] firstcp, secondcp;

                // Draw curve by Bezier.
                ClosedBezierSpline.GetCurveControlPoints(DrawPoints.ToArray(), out firstcp, out secondcp);
                for (int i = 1; i < firstcp.Length; ++i)
                {
                    segments.Add(new BezierSegment(firstcp[i - 1], secondcp[i], DrawPoints[i], true));
                }
                segments.Add(new BezierSegment(firstcp[firstcp.Length - 1], secondcp[0], DrawPoints[0], true));
            }
            pfg.Segments = segments;
            PathFigureCollection pfgc = new PathFigureCollection();
            pfgc.Add(pfg);
            Curve.Figures = pfgc;
            UserPath.Data = Curve;
            UserPath.StrokeThickness = 1;
            UserPath.Stroke = lnColor;
        }

        public void DrawCurve()
        {
            if (!Visible) return;
            if (DrawPoints.Count == 0) return;

            GetScaleSynced();
            foreach (Ellipse elps in SurfaceShapes) DrawingSurface.Children.Remove(elps);
            SurfaceShapes.Clear();
            FillVisualData();
            if (UserPath != null) DrawingSurface.Children.Remove(UserPath);
            GeneratePath();
            if (UserPath != null) DrawingSurface.Children.Add(UserPath);
            foreach (Ellipse elps in SurfaceShapes) DrawingSurface.Children.Add(elps);
        }

        public SectionSynchronizer mSyncer
        {
            get { return syncer; }
            set { syncer = value; }
        }

        #region Scaling
        //-------------------------------Пересчеты координат-------------------------------------------------//
        protected const double Offset = 0.1;  
        private void SetYmins()
        {
            if ((DrawPoints==null) || (DrawPoints.Count == 0)) return;

            minY = DrawPoints[0].Y;
            maxY = DrawPoints[0].Y;
            foreach (Point pt in DrawPoints)
            {
                minY = Math.Min(pt.Y, minY);
                maxY = Math.Max(pt.Y, maxY);
            }
            minY = Math.Min(0, minY);
            maxY = Math.Max(0, maxY);
            surfH = (maxY - minY) * (1 + Offset);
            minY -= (Offset / 2) * surfH;
            maxY += (Offset / 2) * surfH;
            if (surfH == 0) surfH = 100;
            if (surfW == 0) surfW = 100;

            surfH = Math.Max(surfH, surfW);
            surfW = surfH;
            ScreenSize = Math.Min(DrawingSurface.ActualHeight, DrawingSurface.ActualWidth) * (1 - Offset);

            DeltaX = 0; deltaY = 0;
        }

        public void GetScaleSynced()
        {
            ScaleParams=syncer.SyncUserSelection();
            //SetYmins();
        }

        private double deltaX = 0;   // дополнительное, центровочное смещение
        protected double deltaY = 0;   // По высоте есть как принцип, но оно не используется, не нужно просто

        double minX, maxX, minY, maxY, ScreenSize, surfW, surfH;

        public BasePlane.ScaleInfo ScaleParams
        {
            get { return new BasePlane.ScaleInfo(minX, maxX, minY, maxY, ScreenSize, surfH); }
            set
            {
                minX = value.X_min; minY = value.Y_min; maxX = value.X_max; maxX = value.X_max;
                ScreenSize = value.SizeOfScreen; surfH = value.SurfLen; surfW = value.SurfLen;
            }
        }

        public double DeltaX
        {
            get { return deltaX; }
            set { deltaX = value; }
        }

        public Point ScaleReal(Point pt_local)
        {
            return new Point(RealX(pt_local.X),RealY(pt_local.Y));
        }

        public double RealX(double x)
        {
            return minX + surfW * (x - deltaX) / ScreenSize;
        }

        public double RealY(double y)
        {
            return minY + surfH * (y - deltaY) / ScreenSize;
        }

        public Point RescalePoint(Point pt_global)
        {
            return new Point(RescaleX(pt_global.X),RescaleY(pt_global.Y));
        }

        public double RescaleX(double x)
        {
            return deltaX + ScreenSize * (x - minX) / surfW;
        }

        public double RescaleY(double y)
        {
            return deltaY + ScreenSize * (y - minY) / surfH;
        }
        #endregion

        /// <summary>
        /// Удаляет точку с определенным индексом
        /// </summary>
        /// <param name="index">Индекс удаляемого элемента</param>
        public void RemoveAt(int index)
        {
            if ((index < 0) || (index >= DataPoints.Count)) return;
            DataPoints.RemoveAt(index);
            DrawPoints.RemoveAt(index);
        }


        public class XSorter : IComparer<Point3D>
        {
            int IComparer<Point3D>.Compare(Point3D x, Point3D y)
            {
                if (Double.Equals(x.X, y.X)) return 0;
                else
                {
                    if (x.X > y.X) return -1;
                    else return 1;
                }
            }
        }
        /// <summary>
        ///  Сохраняет набор точек следа в файл
        /// </summary>
        /// <remarks>
        ///  Сохранение точек, адекватная версия
        ///  </remarks>
        /// <param name="filename">имя файла, куда сохранять</param>
        public void SaveToFile(string filename)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false);
            file.WriteLine("#Format: X1;Y1;Z1");
            Point3D[] outmas = DataPoints.ToArray();
            Array.Sort(outmas, new XSorter());
            for (int i = 0; i < outmas.Count(); i++)
            {
                file.WriteLine(outmas[i].ToString());
            }
            file.Close();
        }

        //-----------------------------------------------------------
        /// <summary>
        /// Добавить точку из пространства
        /// </summary>
        /// <param name="pt">Какую добавить</param>
        public abstract void AddPoint3D(Point3D pt);
        /// <summary>
        /// Удалить точку с заданными координатами
        /// </summary>
        /// <param name="pt">Какую удалить</param>
        public abstract void RemovePoint3D(Point3D pt);
    }

    /// <summary>
    /// Часть функций вынесены в потомка, чтобы необщие случаи и решения можно было менять
    /// Создавать, естественно, придется потомка.
    /// </summary>
    class SolePointCurve : PointCurve
    {
        // Считаю, что след - это XY, Z не учитываем

        public SolePointCurve(Canvas outsurface) : base(outsurface) { }

        /// <summary>
        /// Втыкает точку в нужное по порядку месту в набор
        /// </summary>
        /// <param name="pt">добавляемая точка</param>
        private void InsertPointInOrder(Point3D pt)
        {
            int ii;
            if (pt.Y > DataPoints[iHeel].Y)
            {
                ii = DataPoints.Count-1;
                while ((ii > 0) && (DataPoints[ii].X > pt.X) && (DataPoints[ii].Y > 0)) ii--;
                DataPoints.Insert(ii+1, pt);
                DrawPoints.Insert(ii+1, RescalePoint(new Point(pt.X, pt.Y)));
                if (switchtoheel) iHeel = ii + 1;
            }
            else
            {
                ii = 0;
                while ((ii < DataPoints.Count) && (DataPoints[ii].X > pt.X) && (DataPoints[ii].Y <= 0)) ii++;
                DataPoints.Insert(ii, pt);
                DrawPoints.Insert(ii, RescalePoint(new Point(pt.X, pt.Y)));
                if (switchtoheel) iHeel = ii;   
                else iHeel++;
            }
        }

        /// <summary>
        /// Добавляет трехмерную точку в набор
        /// </summary>
        /// <param name="pt">Добавляемая точка</param>
        public override void AddPoint3D(Point3D pt)
        {
            bool match = false;
            for (int i = 0; i < DataPoints.Count; i++)
            {
                if (PointsAreEquals(DataPoints[i], pt))
                {
                    match = true;
                    break;
                }
            }
            if (!match)
            {
                GetScaleSynced();
                if (DataPoints.Count > 0) InsertPointInOrder(pt);
                else
                {
                    if (switchtoheel) iHeel = 0;
                    DataPoints.Add(pt);
                    DrawPoints.Add(RescalePoint(new Point(pt.X, pt.Y)));
                }
            }
        }

        /// <summary>
        ///  Удаляет трехмерную точку из набора при её наличии
        /// </summary>
        /// <param name="pt">Удаляемая точка</param>
        public override void RemovePoint3D(Point3D pt)
        {
            for (int i = 0; i < DataPoints.Count; i++)
            {
                if (PointsAreEquals(DataPoints[i], pt))
                {
                    DataPoints.RemoveAt(i);
                    DrawPoints.RemoveAt(i);
                    break;
                }
            }
        }

        private bool switchtoheel;

        public void PointAdded(object sender, SelectMarker.PointSelectedEventArgs e)
        {
            switchtoheel = e.IsHeel;
            AddPoint3D(e.SelPoint);
            DrawCurve();
        }
    }

    /// <summary>
    ///  Маркер-выбиралка вынесен отдельно,
    ///  чтобы не было путаницы с окнами ввода-вывода.
    ///  Так, возможно, код будет читабельнее.
    /// </summary>
    public class SelectMarker
    {
        private Canvas DrawingSurface;
        //private Ellipse elps;
        private Line ln1, ln2;

        public SelectMarker():this(null) {}

        public SelectMarker(Canvas insurface)
        {
            DrawingSurface = insurface;
            mColor = System.Windows.Media.Brushes.Red;
            vis = true;
        }

        public void MoveX(double dx)
        {
            Pos.X += dx;
            //Canvas.SetLeft(elps, Pos.X - mRadius);
        }

        public void MoveY(double dy)
        {
            Pos.X += dy;
            //Canvas.SetLeft(elps, Pos.X - mRadius);
        }

        public void TryAddPoint(Point3D pt)
        {
            OnPointSelected(new PointSelectedEventArgs(pt));
        }

        public void TryAddPoint(Point3D pt, bool isheel)
        {
            OnPointSelected(new PointSelectedEventArgs(pt,isheel));
        }

        /// <summary>
        ///  Стереть маркер
        /// </summary>
        public void RemoveMarker()
        {
            //DrawingSurface.Children.Remove(elps);
            DrawingSurface.Children.Remove(ln1);
            DrawingSurface.Children.Remove(ln2);
        }

        public void DrawMarker()
        {
            /* Хорошо бы маркер опредлять тоже
             * в независимых координатах, но пересчеты
             * масшатба - в классах кривых, вот пусть там и остаются */
            //DrawingSurface.Children.Remove(elps);
            DrawingSurface.Children.Remove(ln1);
            DrawingSurface.Children.Remove(ln2);
            if (!vis) return;
            ln1 = new Line();
            ln1.X1 = Pos.X - dr; ln1.X2 = Pos.X + dr;
            ln1.Y1 = Pos.Y; ln1.Y2 = Pos.Y;
            ln1.StrokeThickness = 0.5;
            ln1.Stroke = mColor;
            ln2 = new Line();
            ln2.X1 = Pos.X; ln2.X2 = Pos.X;
            ln2.Y1 = Pos.Y -  dr; ln2.Y2 = Pos.Y +  dr;
            ln2.StrokeThickness = 0.5;
            ln2.Stroke = mColor;
            //elps = new Ellipse();
            //elps.Fill = mColor; elps.Stroke = mColor;
            //Canvas.SetLeft(elps, Pos.X - mRadius);
            //Canvas.SetTop(elps, Pos.Y -  mRadius);
            //elps.Height = 2 * mRadius; elps.Width = 2 * mRadius;
            //elps.Tag = "selmrk";
            //DrawingSurface.Children.Add(elps);
            DrawingSurface.Children.Add(ln1);
            DrawingSurface.Children.Add(ln2);
        }

        #region Events
        //-------------------------------
        public class PointSelectedEventArgs : EventArgs
        {
            private Point3D _PT;
            private bool _heel;
            /// <summary>
            ///  Является ли точка крайней, пяточной
            /// </summary>
            public bool IsHeel
            {
                get { return _heel; }
                set { _heel = value; }
            }
            /// <summary>
            /// Точка, которая отсылается на добавление
            /// </summary>
            public Point3D SelPoint
            {
                get { return _PT; }
                set { _PT = value; }
            }
            public PointSelectedEventArgs(Point3D pt) { _PT = pt; _heel = false; }
            public PointSelectedEventArgs(Point3D pt, bool isheel) { _PT = pt; _heel = isheel; }
        }
        /// <summary>
        /// Событие возникает, когда хотим добавить
        ///  новую точку в набор следа.
        /// </summary>
        public event EventHandler<PointSelectedEventArgs> NewPointSelected;

        /// <summary>
        /// Событие
        /// </summary>
        /// <param name="e"></param>
        protected void OnPointSelected(PointSelectedEventArgs e)
        {
            EventHandler<PointSelectedEventArgs> handler = NewPointSelected;

            if (handler != null) { handler(this, e); }
        }

        #endregion

        #region Parameters
        //-------------------------------------
        private System.Windows.Point Pos;
        private Point3D Val;
        private SolidColorBrush mColor;
        protected const double mRadius = 3;
        protected const double dr = 9;
        private int _tag;
        private bool vis;

        /// <summary>
        ///  Видно/Не видно
        /// </summary>
        public bool Visible
        {
            get { return vis; }
            set { vis = value; }
        }

        /// <summary>
        /// Можно хранить номер точки
        /// </summary>
        public int iTag
        {
            get { return _tag; }
            set { _tag = value;  DrawMarker(); }
        }

        /// <summary>
        /// То, на чем рисуем
        /// </summary>
        public Canvas OutSurface
        {
            get { return DrawingSurface; }
            set { DrawingSurface = value; }
        }

        /// <summary>
        /// Какое значение хранит
        /// </summary>
        public Point3D SpaceValue
        {
            get { return Val; }
            set { Val = value; }
        }

        /// <summary>
        /// Где рисуем, экранно
        /// </summary>
        public System.Windows.Point DrawPosition
        {
            get { return Pos; }
            set { Pos = value; }
        }

        #endregion
    }
 //----EOF-----
}
