using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

namespace ShoeCurveEditor
{
    /// <summary>
    ///  Класс, отвечающий только непосредственно за область вывода.
    ///  Здесь собрано всё, что не касается кривых и данных непосредственно.
    ///  Детали насчет представления кривых и их количества - в наследниках
    /// </summary>
    /// <remarks> Возможно, имело смысл сделать единый класс, поддерживающий
    /// работу с несколькими кривыми...но это расточительно, грубовато, а главное - так разрабатывать 
    /// проблематичнее и падает гибкость. 
    /// </remarks>
    public abstract class BasePlane
    {
        /// <remarks>
        /// Вот тут было несколько способов реализовать одно и то же. Можно было
        /// отнаследоваться от Canvas и создавать на окне наследников. Но тогда код точно весь уйдет в 1 класс,
        /// а с новым компонентом возня...хотя мб идея неплоха. Можно было вынести всю логику мышки наружу, в интерфейс.
        /// Но это точно плохо - понадобятся внутренние данные и конец инкапсуляции. Потому вариант компромиссный и, по
        ///  идее, надежный.
        /// </remarks> 

        // -----------настройки внешнего вида------------------
        protected SolidColorBrush AxisColor, CurveColor;
        protected SolidColorBrush CenterColor, MarkerColor;
        protected const double PointRadius = 3;
        protected const int gridnum = 5; 
        //---------------------------------------

        protected Canvas DrawingSurface;

        protected Double maxX, maxY, minX, minY;
        protected double ScreenSize;
        protected double surfH, surfW;  //Впоследствии надо б оставить 1 только
        protected const double Offset = 0.1;  //"Поля", %
        //!!! Координаты элементов управления НЕ экранные должны быть, иначе при любом движении всё "поползет" !!!
        protected Point RotationCenter;
        protected Point sh1_pos;
        protected Point sh2_pos;
        protected double sec_angle;

        //-------------------Детали чертежа, мелкие и не очень---------------------------
        protected const double RotElpsR = 7;
        protected const double shR = 5;
        protected Ellipse RotElps;
        protected Ellipse Zero;
        protected Label lbl;
        protected Line Yaxis, Xaxis;
        protected double chk_rszY, chk_rszX; // Проверка высоты и ширины
        protected List<Line> Xgrid, Ygrid;
        protected List<Label> markups;
        protected Line markerX, markerY;
        protected Ellipse sec_sh1, sec_sh2;
        protected Line sec_ln;
        protected double sec_len;
        //-----выбиралка точек--------------------------
        private SelectMarker selector;
        //----------------------------------------------

        /// <summary>
        /// Конструктор базы, окна вывода
        /// </summary>
        /// <param name="surface">На чем рисуем</param>
        /// <param name="view">Тип плоскости</param>
        public BasePlane(Canvas surface, PlaneType view)
        {
            DrawingSurface = surface;
            viewtype = view;
            is_dragged = false;
            mode = DrawMode.Show;
            mode_lines = LineMode.Spline;
            gridon = true;
            Xgrid = new List<Line>();
            Ygrid = new List<Line>();
            markups = new List<Label>();
            sec_angle = 45;
            center_on = true;
            sec_len = 70;
            sec_shape_moved = false;
            section_on = true;
            guides_on = false;
            lines_on = true;
            max_scale = false;
            visible = true;
            selvis = true;
            if (view == PlaneType.XY) selvis = false;

            selector = new SelectMarker(surface);

            MarkerColor = Brushes.OrangeRed;
            CenterColor = Brushes.Lime;
            AxisColor = Brushes.AntiqueWhite;
            CurveColor = Brushes.Brown;

            chk_rszY = DrawingSurface.ActualHeight;
            chk_rszX = DrawingSurface.ActualWidth;
            RotationCenter = new Point(0, 0);
            PrevCenter= new Point(0, 0);

            sh1_pos = new Point(RotationCenter.X - sec_len * Math.Cos(Math.PI * sec_angle / 180),
    RotationCenter.Y - sec_len * Math.Sin(Math.PI * sec_angle / 180));
            sh2_pos = new Point(RotationCenter.X + sec_len * Math.Cos(Math.PI * sec_angle / 180),
                RotationCenter.Y + sec_len * Math.Sin(Math.PI * sec_angle / 180));
        }

        /// <summary>
        /// Рисует кривую.
        /// </summary>
        public abstract void DrawXXCurve();

        private bool sec_shape_moved;

        protected void DrawRotationCenter()
        {
            if (RotElps != null) DrawingSurface.Children.Remove(RotElps);
            if (sec_ln != null) DrawingSurface.Children.Remove(sec_ln);
            if (sec_sh1 != null) DrawingSurface.Children.Remove(sec_sh1);
            if (sec_sh2 != null) DrawingSurface.Children.Remove(sec_sh2);
            if (!center_on) return;

            sec_ln = new Line();
            sec_ln.Stroke = MarkerColor;
            sec_ln.X1 = RescaleX(sh1_pos.X);
            sec_ln.Y1 = RescaleY(sh1_pos.Y);
            sec_ln.X2 = RescaleX(sh2_pos.X);
            sec_ln.Y2 = RescaleY(sh2_pos.Y);
            sec_ln.StrokeThickness = 1;

            sec_sh1 = new Ellipse();
            sec_sh1.Fill = CenterColor;
            sec_sh1.StrokeThickness = 0;
            Canvas.SetTop(sec_sh1, RescaleY(sh1_pos.Y) - shR);
            Canvas.SetLeft(sec_sh1, RescaleX(sh1_pos.X) - shR);
            sec_sh1.Height = 2 * shR; sec_sh1.Width = 2 * shR;
            sec_sh1.Tag = "sh1";

            sec_sh2 = new Ellipse();
            sec_sh2.Fill = CenterColor;
            sec_sh2.StrokeThickness = 0;
            Canvas.SetTop(sec_sh2, RescaleY(sh2_pos.Y) - shR);
            Canvas.SetLeft(sec_sh2, RescaleX(sh2_pos.X) - shR);
            sec_sh2.Height = 2 * shR; sec_sh2.Width = 2 * shR;
            sec_sh2.Tag = "sh2";

            DrawingSurface.Children.Add(sec_ln);
            DrawingSurface.Children.Add(sec_sh1);
            DrawingSurface.Children.Add(sec_sh2);
            sec_sh1.MouseLeftButtonDown += delegate(Object sender, MouseButtonEventArgs e) { sec_shape_moved = true; };
            sec_sh2.MouseLeftButtonDown += delegate(Object sender, MouseButtonEventArgs e) { sec_shape_moved = true; };
            sec_sh1.MouseLeftButtonUp += delegate(Object sender, MouseButtonEventArgs e) { sec_shape_moved = false; OnRotationCenterChanged(new RotationCenterEventArgs(RotationCenter, SectionAngle)); };
            sec_sh2.MouseLeftButtonUp += delegate(Object sender, MouseButtonEventArgs e) { sec_shape_moved = false; OnRotationCenterChanged(new RotationCenterEventArgs(RotationCenter, SectionAngle)); };

            if (!section_on)
            {
                sec_sh1.Visibility = Visibility.Hidden;
                sec_sh2.Visibility = Visibility.Hidden;
                sec_ln.Visibility = Visibility.Hidden;
            }

            RotElps = new Ellipse();
            RotElps.StrokeThickness = 2;
            RotElps.Stroke = MarkerColor;
            RotElps.Fill = CenterColor;
            Canvas.SetLeft(RotElps, RescaleX(RotationCenter.X) - RotElpsR);
            Canvas.SetTop(RotElps, RescaleY(RotationCenter.Y) - RotElpsR);
            RotElps.Height = 2 * RotElpsR; RotElps.Width = 2 * RotElpsR;
            RotElps.Tag = "RotationCenter";
            RotElps.MouseLeftButtonDown += delegate(Object sender, MouseButtonEventArgs e) 
            {
                if (Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    sh1_pos.X = RotationCenter.X;
                    sh2_pos.X = RotationCenter.X;
                    sh1_pos.Y = RotationCenter.Y - sec_len;
                    sh2_pos.Y = RotationCenter.Y + sec_len; 
                }
            };
            DrawingSurface.Children.Add(RotElps);
            RotElps.MouseLeftButtonUp += new MouseButtonEventHandler(MouseUp);
        }

        #region Scaling
        //-------------------------------Пересчеты координат-------------------------------------------------//

        private double deltaX = 0;   // дополнительное, центровочное смещение
        protected double deltaY = 0;   // По высоте есть как принцип, но оно не используется, не нужно просто

        public double DeltaX
        {
            get { return deltaX; }
            set { deltaX = value; }
        }

        public Point ScaleReal(Point pt_local)
        {
            Point pt = new Point();
            pt.X = RealX(pt_local.X);
            pt.Y = RealY(pt_local.Y);
            return pt;
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
            Point pt = new Point();
            pt.X = RescaleX(pt_global.X);
            pt.Y = RescaleY(pt_global.Y);
            return pt;
        }

        public double RescaleX(double x)
        {
            return deltaX + ScreenSize * (x - minX) / surfW;
        }

        public double RescaleY(double y)
        {
            return deltaY + ScreenSize * (y - minY) / surfH;
        }

        /// <summary>
        /// Ключевой расчет параметров масштабирования
        /// </summary>
        abstract public void SetMins();
        //---------------------------------------------------------------------------------------------------//
        #endregion

        #region VariousProperties

        public enum DrawMode { Show, Edit }
        public enum LineMode { Segments, Spline }

        private bool is_dragged;
        private bool gridon;
        private bool center_on;
        private bool section_on;
        private bool guides_on;
        private bool lines_on;
        protected DrawMode mode;
        protected LineMode mode_lines;
        private bool max_scale;
        private bool visible;
        private bool selvis;

        /// <summary>
        ///  Маркер сейчас 1шт.
        /// </summary>
        public SelectMarker sMarker
        {
            get { return selector; }
            set { selector = value; }
        }
        /// <summary>
        /// Видимость маркеров выбора
        /// </summary>
        public bool SelVisible
        {
            get { return selvis; }
            set 
            {
                selvis = value;
                sMarker.Visible = value;
                sMarker.DrawMarker();
            }
        }

        public bool IsVisible
        {
            get { return visible; }
            set { visible = value; }
        }

        public bool StrictScale
        {
            get { return max_scale; }
            set { max_scale = value; }
        }

        public bool ShowLines
        {
            get { return lines_on; }
            set { lines_on = value; }
        }

        public SolidColorBrush CurrentCurveColor
        {
            get { return CurveColor; }
            set { CurveColor = value; }
        }

        public bool IsDragged
        {
            get { return is_dragged; }
        }

        public bool ShowGrid
        {
            get { return gridon; }
            set { gridon = value; }
        }

        /// <summary>
        ///  Ломаная или безье-сплайн
        /// </summary>
        public LineMode CurveStyle
        {
            get { return mode_lines; }
            set { mode_lines = value; }
        }

        /// <summary>
        /// Показываем ли точки данных
        /// </summary>
        public DrawMode CurveShowMode
        {
            get { return mode; }
            set { mode = value; }
        }

        public Point RotationPoint
        {
            get { return RescalePoint(RotationCenter); }
            set { RotationCenter = ScaleReal(value); }
        }

        private Point PrevCenter;

        public double SectionAngle
        {
            get 
            {
                Point shpos;
                if (sh1_pos.Y > sh2_pos.Y) shpos = sh1_pos;
                else shpos = sh2_pos;
                return 180 * (Math.Atan((shpos.Y - RotationCenter.Y) / (shpos.X - RotationCenter.X))) / Math.PI;
            }
           // set { sec_angle = value; }
        }

        public bool ShowCenter
        {
            get { return center_on; }
            set { center_on = value; if (!value) section_on = false; }
        }

        /// <summary>
        ///  Показывать ли отрезок для задания произвольного сечения
        /// </summary>
        public bool ShowSecSelect
        {
            get { return section_on; }
            set { section_on = value; }
        }

        public bool ShowGuidelines
        {
            get { return guides_on; }
            set { guides_on = value; }
        }

        public enum PlaneType { XY, XZ, YZ, Custom, Section }
        private PlaneType viewtype;

        public PlaneType Viewtype
        {
            get { return viewtype; }
            set { viewtype = value; }
        }


        public struct ScaleInfo
        {
            /// <summary>
            ///  Заполняет информацию о масштабах
            /// </summary>
            /// <param name="X0">Х-min</param>
            /// <param name="X1">X-max</param>
            /// <param name="Y0">Y-min</param>
            /// <param name="Y1">Y-max</param>
            /// <param name="screen">Экранный параметр</param>
            /// <param name="surf">Масштабный параметр</param>
            public ScaleInfo(double X0, double X1, double Y0, double Y1, double screen, double surf)
            {
                X_min = X0; X_max = X1; Y_min = Y0; Y_max = Y1; SizeOfScreen = screen; SurfLen = surf;
            }
            public double X_min, X_max;
            public double Y_min, Y_max;
            public double SizeOfScreen;
            public double SurfLen;
        }

        public ScaleInfo ScaleParams
        {
            get { return new ScaleInfo(minX, maxX, minY, maxY, ScreenSize, surfH); }
            set
            {
                minX = value.X_min; minY = value.Y_min; maxX = value.X_max; maxX = value.X_max;
                ScreenSize = value.SizeOfScreen; surfH = value.SurfLen; surfW = value.SurfLen;
            }
        }

        #endregion

        #region Handlers
        //--------------------------------------Обработчики--------------------------------------------------//
        private Point c_pos0;

        public void MouseDown(Object sender, MouseEventArgs e)
        {
            is_dragged = CurveHitTest(e.GetPosition(DrawingSurface));  //MessageBox.Show("Yeahh from curve!!!");
            c_pos0 = e.GetPosition(DrawingSurface);
        }

        public void MouseMove(Object sender, MouseEventArgs e)
        {
            if (is_dragged)
            {
                RotationCenter = ScaleReal(e.GetPosition(DrawingSurface));

                if (markerX != null) DrawingSurface.Children.Remove(markerX);
                if (markerY != null) DrawingSurface.Children.Remove(markerY);

                markerX = new Line();
                markerX.X1 = 0;
                markerX.X2 = DrawingSurface.ActualWidth;
                markerX.Y1 = RescaleY(RotationCenter.Y);
                markerX.Y2 = RescaleY(RotationCenter.Y);
                markerX.Stroke = MarkerColor;
                DoubleCollection dash = new DoubleCollection(2);
                dash.Add(5); dash.Add(3);
                markerX.StrokeDashArray = dash;

                markerY = new Line();
                markerY.X1 = RescaleX(RotationCenter.X);
                markerY.X2 = RescaleX(RotationCenter.X);
                markerY.Y1 = 0;
                markerY.Y2 = DrawingSurface.ActualHeight;
                markerY.Stroke = MarkerColor;
                markerY.StrokeDashArray = dash;

                DrawingSurface.Children.Add(markerX);
                DrawingSurface.Children.Add(markerY);

                //-----
                Point dr = e.GetPosition(DrawingSurface);
                dr.X -= c_pos0.X; dr.Y -= c_pos0.Y;
                sh1_pos.X = RealX(RescaleX(sh1_pos.X) + dr.X); sh1_pos.Y = RealY(RescaleY(sh1_pos.Y) + dr.Y);
                sh2_pos.X = RealX(RescaleX(sh2_pos.X) + dr.X); sh2_pos.Y = RealY(RescaleY(sh2_pos.Y) + dr.Y);
                c_pos0 = e.GetPosition(DrawingSurface);
                DrawRotationCenter();
            }

            if (sec_shape_moved)
            {
                Point pos=ScaleReal(e.GetPosition(DrawingSurface));
                double dx = -pos.X + 2 * RotationCenter.X;
                double dy = -pos.Y + 2 * RotationCenter.Y;
                sh1_pos = pos;
                sh2_pos = new Point(dx, dy);
                DrawRotationCenter();
            }
        }

        public void MouseUp(Object sender, MouseEventArgs e)
        {
            if (is_dragged)
            {
                is_dragged = false;
                if (markerX != null) DrawingSurface.Children.Remove(markerX);
                if (markerY != null) DrawingSurface.Children.Remove(markerY);

                RotationCenter = ScaleReal(e.GetPosition(DrawingSurface));
                Point pos =sh1_pos;
                double dx = -pos.X + 2 * RotationCenter.X;
                double dy = -pos.Y + 2 * RotationCenter.Y;
                sh1_pos = pos;
                sh2_pos = new Point(dx, dy);
                DrawRotationCenter();
                OnRotationCenterChanged(new RotationCenterEventArgs(RotationCenter,SectionAngle));
            }
            sec_shape_moved = false;
        }

        /// <summary>
        /// Проверяет, попал ли пользователь куда-нибудь, когда тыкал
        /// </summary>
        /// <param name="m_pt">точка - координаты мыши</param>
        /// <returns>Попали или нет</returns>
        public bool CurveHitTest(Point m_pt)
        {
            //Проверка попадания по тому, что можно нажимать
            //Пока нажимать можно только на центр вращения, потому переменная одна, 
            // усложнение возможно
            resfortest = false;
            VisualTreeHelper.HitTest(DrawingSurface, null, new HitTestResultCallback(HitTestCallback), new PointHitTestParameters(m_pt));
            return resfortest;
        }

        private bool resfortest;

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            // Lazy (ленивое) вычисление условия ниже, приятная возможность языка
            if ((result.VisualHit.GetType() == typeof(Ellipse)) && (((Ellipse)result.VisualHit).Tag.ToString() == "RotationCenter"))
            {
                    resfortest = true;
                    return HitTestResultBehavior.Stop;
            }
            //else if ((result.VisualHit.GetType() == typeof(Ellipse)) && (((Ellipse)result.VisualHit).Tag.ToString()=="usr"))
            //{
            //    if (Keyboard.IsKeyDown(Key.LeftAlt)) selector.TryAddPoint(GetMarkedPoint());
            //    return HitTestResultBehavior.Stop;
            //}
            else return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// Непосредственно перерисовка
        /// </summary>
        /// <remarks>Сделана отдельно, чтоб не исхитряться с одним resize</remarks>
        public void RedrawAll()
        {
            ScaleTransform st = new ScaleTransform();
            st.ScaleY = -1;

            if (lbl != null)
            {
                DrawingSurface.Children.Remove(lbl);
                DrawingSurface.Children.Remove(Zero);
                DrawingSurface.Children.Remove(Yaxis);
                DrawingSurface.Children.Remove(Xaxis);
            }

            //Y-scaling steps

            const double R = 5;

            Zero = new Ellipse();
            Zero.Width = 2 * R;
            Zero.Height = 2 * R;
            Zero.Fill = AxisColor;
            Zero.Stroke = AxisColor;
            Canvas.SetLeft(Zero, RescaleX(0) - R);
            Canvas.SetTop(Zero, RescaleY(0) - R);
            DrawingSurface.Children.Add(Zero);

            Yaxis = new Line();
            Yaxis.StrokeThickness = 2;
            Yaxis.Stroke = AxisColor;
            Yaxis.X1 = RescaleX(0);
            Yaxis.X2 = RescaleX(0);
            Yaxis.Y1 = 0;
            Yaxis.Y2 = DrawingSurface.ActualHeight;
            DrawingSurface.Children.Add(Yaxis);

            lbl = new Label();
            lbl.Content = viewtype.ToString();
            Canvas.SetTop(lbl, DrawingSurface.ActualHeight);
            Canvas.SetLeft(lbl, 0);
            lbl.VerticalAlignment = VerticalAlignment.Top;
            lbl.HorizontalAlignment = HorizontalAlignment.Right;
            lbl.RenderTransform = st;
            lbl.FontWeight = FontWeights.Bold;
            DrawingSurface.Children.Add(lbl);

            chk_rszY = DrawingSurface.ActualHeight;

            //-------------------------------------------------------

            //X-scaling steps

            Xaxis = new Line();
            Xaxis.StrokeThickness = 2;
            Xaxis.Stroke = AxisColor;
            Xaxis.X1 = 0;
            Xaxis.X2 = DrawingSurface.ActualWidth;
            Xaxis.Y1 = RescaleY(0);
            Xaxis.Y2 = RescaleY(0);
            DrawingSurface.Children.Add(Xaxis);

            chk_rszX = DrawingSurface.ActualWidth;

            //------------------------------------------------------
            DrawXXCurve();
            DrawRotationCenter();
            SetMarker(sMarker.iTag);
            if (SelVisible) sMarker.DrawMarker();

            DrawGrid(xdefaultstep, ydefaultstep, true);
            if (gridon) DrawGrid(xdefaultstep, ydefaultstep, false);
        }
        /// <summary>
        /// Обработчик изменения размеров области вывода, можно подписываться
        /// на соответствующее событие, хотя используется много где вручную
        /// </summary>
        /// <param name="sender">отправитель</param>
        /// <param name="e">параметры</param>
        /// <remarks>
        /// В интерфейсе пока resize=redraw. Используется там для перерисовки, соответственно.
        /// При окончательной чистке кода надо разделить
        /// </remarks>
        public void SurfaceResize(object sender, SizeChangedEventArgs e)
        {
            if ((e == null) || (chk_rszY != DrawingSurface.ActualHeight) || (chk_rszX != DrawingSurface.ActualWidth))
            {
                DrawingSurface.Children.Clear();
                SetMins();
                OnDataResized(new EventArgs());
                RedrawAll();
            }
        }

        //---------------------------------------------------------------------------------------------------//
        #endregion

        #region Grid_Stuff
        //------------------То, что относится к сетке----------------------------//

        protected double xdefaultstep, ydefaultstep;

        public Point GridSteps
        {
            get { return new Point(xdefaultstep, ydefaultstep); }
            set { xdefaultstep = value.X; ydefaultstep = value.Y; }
        }

        protected double CalcStep(double x, int N)
        {
            double xx = 0;
            int k = 1;
            if (x < N) k = 100;
            while (xx < Math.Floor(k * x / N)) { xx += 5; }
            return xx / k;
        }

        /// <summary>
        /// Рисует сетку
        /// </summary>
        /// <param name="erase">Стереть ли сетку вместо рисования?</param>
        public void DrawGrid(double xstep, double ystep, bool erase)
        {
            foreach (Line ln in Xgrid)
            {
                DrawingSurface.Children.Remove(ln);
            }
            foreach (Line ln in Ygrid)
            {
                DrawingSurface.Children.Remove(ln);
            }
            foreach (Label mk in markups)
            {
                DrawingSurface.Children.Remove(mk);
            }

            if (!erase)
            {
                ScaleTransform st = new ScaleTransform();
                st.ScaleY = -1;

                Xgrid.Clear();
                Ygrid.Clear();
                markups.Clear();
                double xpos = xstep;
                Line ln;
                Label mlbl;
                DoubleCollection dash = new DoubleCollection(2);
                dash.Add(5); dash.Add(3);
                //Y
                while (xpos < RealX(DrawingSurface.ActualWidth))
                {
                    ln = new Line();
                    ln.X1 = RescaleX(xpos); ln.X2 = RescaleX(xpos);
                    ln.Y1 = 0; ln.Y2 = DrawingSurface.ActualHeight;
                    ln.Stroke = AxisColor;
                    ln.StrokeDashArray = dash;
                    ln.StrokeThickness = 1;
                    Ygrid.Add(ln);
                    //-------
                    mlbl = new Label();
                    mlbl.Content = xpos.ToString();
                    mlbl.FontWeight = FontWeights.UltraLight;
                    mlbl.FontSize = 8;
                    mlbl.Foreground = AxisColor;
                    Canvas.SetLeft(mlbl, RescaleX(xpos));
                    Canvas.SetTop(mlbl, 15);
                    mlbl.RenderTransform = st;
                    markups.Add(mlbl);

                    xpos += xstep;
                }
                xpos = 0;
                while (xpos > RealX(0))
                {
                    ln = new Line();
                    ln.X1 = RescaleX(xpos); ln.X2 = RescaleX(xpos);
                    ln.Y1 = 0; ln.Y2 = DrawingSurface.ActualHeight;
                    ln.Stroke = AxisColor;
                    ln.StrokeDashArray = dash;
                    ln.StrokeThickness = 1;
                    Ygrid.Add(ln);
                    //-------
                    mlbl = new Label();
                    mlbl.Content = xpos.ToString();
                    mlbl.FontWeight = FontWeights.UltraLight;
                    mlbl.FontSize = 8;
                    mlbl.Foreground = AxisColor;
                    Canvas.SetLeft(mlbl, RescaleX(xpos));
                    Canvas.SetTop(mlbl, 15);
                    mlbl.RenderTransform = st;
                    markups.Add(mlbl);

                    xpos -= xstep;
                }
                //X
                double ypos = ystep;
                while (ypos < RealY(DrawingSurface.ActualHeight))
                {
                    ln = new Line();
                    ln.X1 = 0; ln.X2 = DrawingSurface.ActualWidth;
                    ln.Y1 = RescaleY(ypos); ln.Y2 = RescaleY(ypos);
                    ln.Stroke = AxisColor;
                    ln.StrokeDashArray = dash;
                    ln.StrokeThickness = 1;
                    Xgrid.Add(ln);
                    //---
                    mlbl = new Label();
                    mlbl.Content = ypos.ToString();
                    mlbl.FontWeight = FontWeights.UltraLight;
                    mlbl.FontSize = 8;
                    mlbl.Foreground = AxisColor;
                    Canvas.SetTop(mlbl, RescaleY(ypos));
                    Canvas.SetLeft(mlbl, 0);
                    mlbl.RenderTransform = st;
                    markups.Add(mlbl);

                    ypos += ystep;
                }
                ypos = 0;
                while (ypos > RealY(0))
                {
                    ln = new Line();
                    ln.X1 = 0; ln.X2 = DrawingSurface.ActualWidth;
                    ln.Y1 = RescaleY(ypos); ln.Y2 = RescaleY(ypos);
                    ln.Stroke = AxisColor;
                    ln.StrokeDashArray = dash;
                    ln.StrokeThickness = 1;
                    Xgrid.Add(ln);
                    //---
                    mlbl = new Label();
                    mlbl.Content = ypos.ToString();
                    mlbl.FontWeight = FontWeights.UltraLight;
                    mlbl.FontSize = 8;
                    mlbl.Foreground = AxisColor;
                    Canvas.SetTop(mlbl, RescaleY(ypos));
                    Canvas.SetLeft(mlbl, 0);
                    mlbl.RenderTransform = st;
                    markups.Add(mlbl);

                    ypos -= ystep;
                }
                //Add
                foreach (Line lnl in Xgrid)
                {
                    DrawingSurface.Children.Add(lnl);
                }
                foreach (Line lnl in Ygrid)
                {
                    DrawingSurface.Children.Add(lnl);
                }
                foreach (Label mk in markups)
                {
                    DrawingSurface.Children.Add(mk);
                }
            }
        }

        //-----------------------------------------------------------------------//
        #endregion

        #region Events
        //--------------------------События, связывают интерфейс и данные------------------------------------//
        /// <summary>
        /// <c>Angle</c> - новый угол сечения
        /// <c>CenterDPos</c> - положение центра поворота
        /// </summary>
        public class RotationCenterEventArgs : EventArgs
        {
            private double alf;
            private Point dpos;
            /// <summary>
            /// Конструктор события смещения центра
            /// </summary>
            /// <param name="newpos">Новое положение центра</param>
            /// <param name="newangle">Новый угол сечения</param>
            public RotationCenterEventArgs(Point newdpos, double newangle)
            {
                alf = newangle;
                dpos = newdpos;
            }

            public double Angle { get { return alf; } set { alf = value; } }
            public Point CenterDPos { get { return dpos; } set { dpos = value; } }
        }

        /// <summary>
        /// Параметры сдвига фигуры
        /// </summary>
        public class MoveFigureEventArgs : EventArgs
        {
            private double _dx;
            private double _dy;

            /// <summary>
            ///  Сдвиг фигуры
            /// </summary>
            /// <param name="dx">смещение по горизонтальной оси</param>
            /// <param name="dy">смещение по вертикальной оси</param>
            public MoveFigureEventArgs(double dx, double dy)
            {
                _dx = dx;
                _dy = dy;
            }
            public Double DX { get { return _dx; } set { _dx = value; } }
            public Double DY { get { return _dy; } set { _dy = value; } }
        }

        /// <summary>
        /// Параметры поворота фигуры
        /// <c>RotationAngle - угол поворота</c>
        /// </summary>
        public class RotateFigureEventArgs : EventArgs
        {
            private double _angle;
            public RotateFigureEventArgs(double angle) { _angle = angle; }

            public double RotationAngle
            {
                get { return _angle; }
                set { _angle = value; }
            }
        }

        /// <summary>
        ///  Событие смещения центра вращения, можно узнать угол
        ///  сечения и положение центра. <seealso cref="RotationCenterEventArgs"/>
        /// </summary>
        public event EventHandler<RotationCenterEventArgs> RotationCenterChanged;
        /// <summary>
        /// Событие смещения фигуры, сообщает, на сколько сместили
        /// </summary>
        public event EventHandler<MoveFigureEventArgs> FigureMoved;
        /// <summary>
        /// Событие поворота фигуры, сообщает, на сколько повернули
        /// </summary>
        public event EventHandler<RotateFigureEventArgs> FigureRotated;
        /// <summary>
        /// Границы данных изменены, надо как-то вызвать синхронизацию осей
        /// </summary>
        public event EventHandler DataResized;

        //Венгерская нотация, и так всё понятно
        protected virtual void OnRotationCenterChanged(RotationCenterEventArgs e)
        {
            EventHandler<RotationCenterEventArgs> handler = RotationCenterChanged;

            //По сути сделано так, что всё равно, какие параметры передаются, выставляются они именно тут
            if (handler != null)
            {
                e.Angle = SectionAngle;
                e.CenterDPos = new Point(RotationCenter.X - PrevCenter.X, RotationCenter.Y - PrevCenter.Y);
                PrevCenter = RotationCenter;
                handler(this, e);
            }
        }

        protected virtual void OnFigureMoved(MoveFigureEventArgs e)
        {
            EventHandler<MoveFigureEventArgs> handler = FigureMoved;

            if (handler != null) { handler(this, e); }
        }

        protected virtual void OnFigureRotated(RotateFigureEventArgs e)
        {
            EventHandler<RotateFigureEventArgs> handler = FigureRotated;

            if (handler != null) { handler(this, e); }
        }

        protected virtual void OnDataResized(EventArgs e)
        {
            EventHandler handler = DataResized;
            if (handler != null) { handler(this, e); }
        }

        //---------------------------------------------------------------------------------------------------//
        #endregion

        /// <summary>
        /// Создает кривую по набору экранных точек. Если надо - с прорисовкой точек.
        /// </summary>
        /// <remarks>Не совсем совпадает с философией абстрактного класса, но так удобно</remarks>
        /// <param name="LinePoints">Экранные точки кривой</param>
        /// <param name="ViewCurve">Объект Path, в котором будет содержаться рисунок</param>
        /// <param name="PointShapes">Массив для маркировки точек, пополняемый список эллипсов</param>
        protected void FillPathFromData(Point[] LinePoints, out System.Windows.Shapes.Path ViewCurve, ref List<Ellipse> PointShapes)
        {
            int size = LinePoints.Count();
            PathFigure pfg = new PathFigure();
            pfg.StartPoint = LinePoints[0];
            ViewCurve = new System.Windows.Shapes.Path();
            PathGeometry Curve = new PathGeometry();

            PathSegmentCollection segments = new PathSegmentCollection(size);
            if (mode_lines == LineMode.Segments)
            {
                LineSegment[] lsg = new LineSegment[size];
                for (int i = 0; i < size; i++)
                {
                    lsg[i] = new LineSegment(LinePoints[i], true);
                    segments.Add(lsg[i]);
                }
                segments.Add(new LineSegment(LinePoints[0], true));
            }
            else
            {
                PointCollection beziercollection = new PointCollection(size);
                Point[] firstcp, secondcp;

                // Draw curve by Bezier.
                ClosedBezierSpline.GetCurveControlPoints(LinePoints, out firstcp, out secondcp);
                for (int i = 1; i < firstcp.Length; ++i)
                {
                    segments.Add(new BezierSegment(firstcp[i - 1], secondcp[i], LinePoints[i], true));
                }
                segments.Add(new BezierSegment(firstcp[firstcp.Length - 1], secondcp[0], LinePoints[0], true));
            }
            pfg.Segments = segments;
            PathFigureCollection pfgc = new PathFigureCollection();
            pfgc.Add(pfg);
            Curve.Figures = pfgc;
            ViewCurve.Data = Curve;

            if (mode == DrawMode.Edit)
            {
                Ellipse elps;
                for (int i = 0; i < size; i++)
                {
                    elps = new Ellipse();
                    Canvas.SetLeft(elps, LinePoints[i].X - PointRadius);
                    Canvas.SetTop(elps, LinePoints[i].Y - PointRadius);
                    Canvas.SetZIndex(elps, 0);
                    elps.Height = 2 * PointRadius; elps.Width = 2 * PointRadius;
                    elps.Stroke = CurveColor;
                    elps.Fill = CurveColor;
                    elps.Tag = i;
                    PointShapes.Add(elps);
                }
            }
        }

        #region Abstracts
        //---------------------------------------------------------------------

        /// <summary>
        /// Непосредственно грузит данные из Delcam в нужный момент
        /// </summary>
        ///  <param name="sender">Отправитель события, если надо будет использовать метод именно так</param>
        /// <param name="DataBinder">Источник данных</param>
        abstract public void UpdateDataPoints(object sender, EventArgs e);
        /// <summary>
        /// Обновляет экранные точки на основе точек данных
        /// </summary>
        abstract protected void RecountSurfacePoints();
        /// <summary>
        /// Сдвиг по X
        /// </summary>
        abstract public void MoveX(double dx);
        /// <summary>
        /// Сдвиг по Y
        /// </summary>
        abstract public void MoveY(double dy);
        /// <summary>
        /// Позволяет выставлять маркер так, чтобы он не сбивался
        /// </summary>
        /// <param name="where">Индекс точки в каком-нить массиве</param>
        abstract public void SetMarker(int where);
        /// <summary>
        ///  Получает в зависимости от типа кривой выбранную точку
        /// </summary>
        /// <returns>Выбранная точка</returns>
        abstract public Point3D GetMarkedPoint();
        /// <summary>
        ///  Циклически изменяет положение выбирающего маркера
        /// </summary>
        /// <param name="ii">На сколько сместить маркер</param>
        abstract public void IncMarkerTag(int ii);

        //---------------------------------------------------------------------
        #endregion

        //--------------------wrappers, обертки---------------------
        public void MakeFigureMoved(double dx, double dy)
        {
            OnFigureMoved(new MoveFigureEventArgs(dx, dy));
            //OnFigureMoved(new MoveFigureEventArgs(surfW * dx / ScreenSize, surfH * dy / ScreenSize));
        }

        public void MakeFigureTurned(double angle)
        {
            OnFigureRotated(new RotateFigureEventArgs(angle));
        }

        public void RotatePoints(ref Point[] points, double angle)
        {
            Matrix T = new Matrix();
            T.RotateAt(angle, RotationCenter.X, RotationCenter.Y);
            for (int i = 0; i < points.Count(); i++)
            {
                points[i]=Point.Multiply(points[i], T);
            }
        }
    }

    /// <summary>
    /// Опорный класс для взаимодействия чертежа,
    /// логики его функционирования (все возможности, в общем)
    /// и пользовательского интерфейса. Содержит работу с одной кривой, остальное - наследуется.
    /// </summary>
    public class CurvePlane : BasePlane
    {
        private System.Windows.Shapes.Path SurfaceCurve;
        protected List<Ellipse> SurfaceShapes;
        //А зачем нам именно геометрия? А затем, что при режиме редактирования будет
        // недурно воткнуть кривые безье. Ибо так сделано в EasyLast и удобно.

        protected Rectangle Guides;
        private double img_angle;

        /// <summary>
        /// Угол отклонения изображения кривой от самой кривой
        /// </summary>
        public double ImgAngle
        {
            get { return img_angle; }
            set 
            {
                img_angle = value;
                SurfaceCurve.RenderTransform = new RotateTransform(img_angle, RescaleX(RotationCenter.X), RescaleY(RotationCenter.Y));
            }
        }

        Point[] DataPoints;     //ну я завел 2 массива на случай необходимости в обратной связи
        Point[] SurfacePoints;  // и вообще её предусматриваю, ой не зря...
        Point3D[] SpacePoints;

        public override void SetMins()
        {   //Масштабирование обеих осей сделано одинаковым, иметь в виду сокращение 
            //разновидностей масштабирования потом 

            if (DataPoints == null) throw new CustomLogicException("No Points yet", CustomLogicException.WTF.EmptyData);

            minX = DataPoints[0].X;
            minY = DataPoints[0].Y;
            maxX = DataPoints[0].X;
            maxY = DataPoints[0].Y;

            foreach (Point pt in DataPoints)
            {
                minX = Math.Min(pt.X, minX);
                maxX = Math.Max(pt.X, maxX);
                minY = Math.Min(pt.Y, minY);
                maxY = Math.Max(pt.Y, maxY);
            }
            minX = Math.Min(0, minX);
            maxX = Math.Max(0, maxX);
            minY = Math.Min(0, minY);
            maxY = Math.Max(0, maxY);

            surfH = (maxY - minY) * (1 + Offset);
            surfW = (maxX - minX) * (1 + Offset);
            if (!StrictScale)
            {
                if (surfH > surfW)
                {
                    surfW = surfH;
                    ScreenSize = DrawingSurface.ActualHeight * (1 - Offset);
                }
                else
                {
                    surfH = surfW;
                    ScreenSize = DrawingSurface.ActualWidth * (1 - Offset);
                }
                //ScreenSize = Math.Min(DrawingSurface.ActualHeight, DrawingSurface.ActualWidth) * (1 - Offset);
            }
            else
            {
                surfH = Math.Max(surfH, surfW);
                surfW = surfH;
                ScreenSize = Math.Min(DrawingSurface.ActualHeight, DrawingSurface.ActualWidth) * (1 - Offset);
            }
            minX -= (Offset / 2) * surfW;
            maxX += (Offset / 2) * surfW;
            minY -= (Offset / 2) * surfH;
            maxY += (Offset / 2) * surfH;

            if (surfH == 0) surfH = 100;
            if (surfW == 0) surfW = 100;

            DeltaX = 0; deltaY = 0;
            //DeltaX = (DrawingSurface.ActualWidth - RescaleX(maxX)) / 2;
            //deltaY = (DrawingSurface.ActualHeight - RescaleY(maxY)) * (1 - Offset / 2);
        }

        /// <summary>
        ///  Проверяет, вылез ли чертеж за рамки окна вывода
        /// </summary>
        /// <returns>Вылез/не вылез</returns>
        public bool AmITooBig()
        {   //obsolete
            return (RescaleX(maxX) > DrawingSurface.ActualWidth) || (RescaleY(maxY) > DrawingSurface.ActualHeight);
            //return (SurfaceCurve.DesiredSize.Height> DrawingSurface.ActualHeight) || (SurfaceCurve.DesiredSize.Width> DrawingSurface.ActualWidth);
            //return (SurfaceCurve.Data.Bounds.Height > DrawingSurface.ActualHeight) || (SurfaceCurve.Data.Bounds.Width > DrawingSurface.ActualWidth);
        }


        #region Transform
        //--------------------------------------Трансформации------------------------------------------------//
        public override void MoveY(double dy)
        {
            for(int i=0; i<DataPoints.Count(); i++)
            {
                DataPoints[i].Y += dy;
            }
            RecountSurfacePoints();
        }

        public override void MoveX(double dx)
        {
            for (int i = 0; i < DataPoints.Count(); i++)
            {
                DataPoints[i].X += dx;
            }
            RecountSurfacePoints();
        }

        //---------------------------------------------------------------------------------------------------//
       #endregion

        /// <summary>
        /// Перерисовывает кривую, конечно
        /// </summary>
        public override void DrawXXCurve()
        {
            if (!IsVisible) return;

            if (DataPoints == null) return;
           
            foreach (Ellipse elps in SurfaceShapes)
            {
                DrawingSurface.Children.Remove(elps);
            }

            for (int i = 0; i < DataPoints.Count(); i++)
            {
                SurfacePoints[i] = RescalePoint(DataPoints[i]);
            }

            if (SurfaceCurve != null) DrawingSurface.Children.Remove(SurfaceCurve);
            SurfaceShapes.Clear();
            FillPathFromData(SurfacePoints, out SurfaceCurve, ref SurfaceShapes);
            if (ShowLines)
            {
                SurfaceCurve.StrokeThickness = 1;
                SurfaceCurve.Stroke = CurrentCurveColor;
                DrawingSurface.Children.Add(SurfaceCurve);
            }

            //Размерные линии
            Point Right = SurfacePoints[0];
            Point Left = SurfacePoints[0];
            foreach (Point pt in SurfacePoints) 
            {
                if (Right.X < pt.X) Right = pt;
                if (Left.X > pt.X) Left = pt;
            }
            //Гайды (гайдлайны)
            if (Guides != null) DrawingSurface.Children.Remove(Guides);
            if (ShowGuidelines)
            {
                Guides = new Rectangle();
                Guides.Stroke = MarkerColor;
                Guides.Fill = null;
                Guides.IsHitTestVisible = false;
                Guides.StrokeDashArray = DashStyles.Dash.Dashes;
                Guides.StrokeThickness = 1;
                Rect bnd = SurfaceCurve.Data.Bounds;
                Guides.Height = bnd.Height;
                Guides.Width = bnd.Width;
                Canvas.SetLeft(Guides, bnd.Left);
                Canvas.SetTop(Guides, bnd.Top);
                DrawingSurface.Children.Add(Guides);
            }

            if (mode == DrawMode.Edit)
            {

                foreach(Ellipse elps in SurfaceShapes)
                {
                    DrawingSurface.Children.Add(elps);
                }
            }
            //------------------------------
        }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="surface">Канва, на которой рисовать будем</param>
        /// <param name="view">Вид сечения, служит чтоб эти их виды различать</param>
        public CurvePlane(Canvas surface, PlaneType view) : base(surface, view) { SurfaceShapes = new List<Ellipse>(); }

        /* Вообще, это уже немного ужасно: функционал разных частей 
         * в одном-двух здоровенных классах. Можно докатиться до
         * анти-паттерна "божественный объект". Если станет совсем
         * завалено - то можно вынести побочный функционал в интерфейсы.
         * Это относится к маркерам, элементам выбора и т.д. */

        #region Marker

        public override void SetMarker(int where)
        {
            sMarker.iTag = where;
            sMarker.SpaceValue = SpacePoints[where];
            sMarker.DrawPosition = SurfacePoints[where];
        }

        public override Point3D GetMarkedPoint()
        {
            return SpacePoints[sMarker.iTag];
        }

        public override void IncMarkerTag(int ii)
        {
            sMarker.iTag = (SpacePoints.Count()+sMarker.iTag+ii) % SpacePoints.Count();
            SetMarker(sMarker.iTag);
        }
        #endregion

        /// <summary>
        /// Непосредственно грузит данные из Delcam в нужный момент
        /// </summary>
        ///  <param name="sender">Отправитель события, если надо будет использовать метод именно так</param>
        /// <param name="e">Не существенные параметры события</param>
        public override void UpdateDataPoints(object sender, EventArgs e)
        {
            try
            {
                SpacePoints = ((ModelLogicInteractor)sender).GetXXCurve(Viewtype)[0];
                DataPoints = new Point[SpacePoints.Count()];
                switch (Viewtype)
                {
                    case (PlaneType.XZ):
                        {
                            for (int i = 0; i < SpacePoints.Count(); i++) DataPoints[i] = new Point(SpacePoints[i].X, SpacePoints[i].Z);
                            break;
                        }
                    case (PlaneType.XY):
                        {
                            for (int i = 0; i < SpacePoints.Count(); i++) DataPoints[i] = new Point(SpacePoints[i].X, SpacePoints[i].Y);
                            break;
                        }
                    case (PlaneType.YZ):
                        {
                            for (int i = 0; i < SpacePoints.Count(); i++) DataPoints[i] = new Point(SpacePoints[i].Y, SpacePoints[i].Z);
                            break;
                        }
                    case (PlaneType.Custom):
                        {
                            for (int i = 0; i < SpacePoints.Count(); i++) DataPoints[i] = new Point(SpacePoints[i].Y, SpacePoints[i].Z);
                            break;
                        }
                    case (PlaneType.Section):
                        {
                            for (int i = 0; i < SpacePoints.Count(); i++) DataPoints[i] = new Point(SpacePoints[i].Y, SpacePoints[i].Z);
                            break;
                        }
                    default: throw new CustomLogicException("Sorcery", CustomLogicException.WTF.IncorrectData);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("No XZ section, sorry!((");
                return;
            }
            SetMins();
            RecountSurfacePoints();
            if (SelVisible) SetMarker(0);

            //----------------------------------------------------
            SurfaceShapes = new List<Ellipse>(SurfacePoints.Count());

            xdefaultstep = CalcStep(RealX(DrawingSurface.ActualWidth) - RealX(0), gridnum);
            ydefaultstep = CalcStep(RealY(DrawingSurface.ActualHeight) - RealY(0), gridnum);

            SurfaceResize(this, null); 
        }

        protected override void RecountSurfacePoints()
        {
            SurfacePoints = new Point[DataPoints.Count()];
            for (int i = 0; i < DataPoints.Count(); i++)
            {
                SurfacePoints[i] = RescalePoint(DataPoints[i]);
            }
            if (SelVisible) SetMarker(sMarker.iTag);
        }
    }

    /// <summary>
    ///  Класс, обечпечивающий вывод чертежа с 1 либо 2 кривыми в сечении. Функционал от базового класса
    /// </summary>
    /// <remarks>Две кривые реализованы "намертво", их арядл будет больше, но если что - просто надо добавить
    /// обычную работу с набором этих кривых-массивов
    /// </remarks>
    public class DoubleCurve : BasePlane
    {
        private System.Windows.Shapes.Path FirstSurfaceCurve;
        private System.Windows.Shapes.Path SecondSurfaceCurve;
        protected List<Ellipse> SurfaceShapes;
        protected Rectangle Guides;
        private double img_angle;
        private bool has_shift;

        /// <summary>
        ///  Включение центровки
        /// </summary>
        public bool HasShift
        {
            get { return has_shift; }
            set { has_shift = value; }
        }

        /// <summary>
        /// Угол отклонения изображения кривой от самой кривой
        /// </summary>
        public double ImgAngle
        {
            get { return img_angle; }
            set
            {
                img_angle = value;
                FirstSurfaceCurve.RenderTransform = new RotateTransform(img_angle, RescaleX(RotationCenter.X), RescaleY(RotationCenter.Y));
                if (SecondSurfaceCurve!=null)
                SecondSurfaceCurve.RenderTransform = new RotateTransform(img_angle, RescaleX(RotationCenter.X), RescaleY(RotationCenter.Y));
            }
        }

        Point[] FirstDataPoints;     // Если кривых в сечении в какой-то момент окажется >2, я
        Point[] FirstSurfacePoints;  // буду долго биться головой об стену
        Point[] SecondDataPoints;    
        Point[] SecondSurfacePoints;
        Point3D[] FirstSpacePoints;
        Point3D[] SecondSpacePoints;

        public DoubleCurve(Canvas surface, PlaneType view)
            : base(surface, view) { SurfaceShapes = new List<Ellipse>(); has_shift = true; }

        public override void SetMins()
        {
            if (FirstDataPoints == null) return;
                //throw new CustomLogicException("No Points yet", CustomLogicException.WTF.EmptyData);

            minX = FirstDataPoints[0].X;
            minY = FirstDataPoints[0].Y;
            maxX = FirstDataPoints[0].X;
            maxY = FirstDataPoints[0].Y;

            foreach (Point pt in FirstDataPoints)
            {
                minX = Math.Min(pt.X, minX);
                maxX = Math.Max(pt.X, maxX);
                minY = Math.Min(pt.Y, minY);
                maxY = Math.Max(pt.Y, maxY);
            }

            if (SecondDataPoints!=null)
            foreach (Point pt in SecondDataPoints)
            {
                minX = Math.Min(pt.X, minX);
                maxX = Math.Max(pt.X, maxX);
                minY = Math.Min(pt.Y, minY);
                maxY = Math.Max(pt.Y, maxY);
            }
            minX = Math.Min(0, minX);
            maxX = Math.Max(0, maxX);
            minY = Math.Min(0, minY);
            maxY = Math.Max(0, maxY);

            surfH = (maxY - minY) * (1 + Offset);
            surfW = (maxX - minX) * (1 + Offset);
            if (!StrictScale)
            {
                if (surfH > surfW)
                {
                    surfW = surfH;
                    ScreenSize = DrawingSurface.ActualHeight * (1 - Offset);
                }
                else
                {
                    surfH = surfW;
                    ScreenSize = DrawingSurface.ActualWidth * (1 - Offset);
                }
                //ScreenSize = Math.Min(DrawingSurface.ActualHeight, DrawingSurface.ActualWidth) * (1 - Offset);
            }
            else
            {
                surfH = Math.Max(surfH, surfW);
                surfW = surfH;
                ScreenSize = Math.Min(DrawingSurface.ActualHeight, DrawingSurface.ActualWidth) * (1 - Offset);
            }
            minX -= (Offset / 2) * surfW;
            maxX += (Offset / 2) * surfW;
            minY -= (Offset / 2) * surfH;
            maxY += (Offset / 2) * surfH;

            if (surfH == 0) surfH = 100;
            if (surfW == 0) surfW = 100;

            DeltaX = 0; deltaY = 0;
            if (HasShift)
            if ((Viewtype == PlaneType.Custom) || (Viewtype == PlaneType.Section)) DeltaX = Math.Abs(DrawingSurface.ActualWidth - RescaleX(maxX) + RescaleX(0)) / 2;  
            //if (Viewtype == PlaneType.XY) deltaY = (DrawingSurface.ActualHeight - RescaleX(maxY)) / 2;
        }

        #region Transform
        //--------------------------------------Трансформации------------------------------------------------//

        public override void MoveY(double dy)
        {
            for (int i = 0; i < FirstDataPoints.Count(); i++)
            {
                FirstDataPoints[i].Y += dy;
            }
            if (SecondDataPoints != null)
            {
                for (int i = 0; i < SecondDataPoints.Count(); i++)
                {
                    SecondDataPoints[i].Y += dy;
                }
            }
            RecountSurfacePoints();
        }

        public override void MoveX(double dx)
        {
            for (int i = 0; i < FirstDataPoints.Count(); i++)
            {
                FirstDataPoints[i].X += dx;
            }
            if (SecondDataPoints != null)
            {
                for (int i = 0; i < SecondDataPoints.Count(); i++)
                {
                    SecondDataPoints[i].X += dx;
                }
            }
            RecountSurfacePoints();
        }

        //---------------------------------------------------------------------------------------------------//
        #endregion

        /// <summary>
        /// Рисует кривую, естественно)
        /// </summary>
        public override void DrawXXCurve()
        {
            if (!IsVisible) return;
            foreach (Ellipse elps in SurfaceShapes)
            {
                DrawingSurface.Children.Remove(elps);
            }

            if (FirstSurfaceCurve != null) DrawingSurface.Children.Remove(FirstSurfaceCurve);
            if (SecondSurfaceCurve != null) DrawingSurface.Children.Remove(SecondSurfaceCurve);
            if (FirstDataPoints == null) return;

            for (int i = 0; i < FirstDataPoints.Count(); i++)
            {
                FirstSurfacePoints[i] = RescalePoint(FirstDataPoints[i]);
            }
            SurfaceShapes.Clear();
            FillPathFromData(FirstSurfacePoints, out FirstSurfaceCurve, ref SurfaceShapes);

            if (SecondDataPoints != null)
            {
                for (int i = 0; i < SecondDataPoints.Count(); i++)
                {
                    SecondSurfacePoints[i] = RescalePoint(SecondDataPoints[i]);
                }
                FillPathFromData(SecondSurfacePoints, out SecondSurfaceCurve, ref SurfaceShapes);
            }
            if (ShowLines)
            {                
                //FillPathFromData(FirstSurfacePoints, out FirstSurfaceCurve, ref SurfaceShapes);
                FirstSurfaceCurve.StrokeThickness = 1;
                FirstSurfaceCurve.Stroke = CurrentCurveColor;
                DrawingSurface.Children.Add(FirstSurfaceCurve);
                
                if (SecondDataPoints != null)
                {
                    SecondSurfaceCurve.StrokeThickness = 1;
                    SecondSurfaceCurve.Stroke = CurrentCurveColor;
                    DrawingSurface.Children.Add(SecondSurfaceCurve);
                }
            }
            //рамочка
            if (Guides != null) DrawingSurface.Children.Remove(Guides);
            if (ShowGuidelines)
            {
                Guides = new Rectangle();
                Guides.Stroke = MarkerColor;
                Guides.Fill = null;
                Guides.IsHitTestVisible = false;
                Guides.StrokeDashArray = DashStyles.Dash.Dashes;
                Guides.StrokeThickness = 1;
                Rect bnd1 = FirstSurfaceCurve.Data.Bounds;
                Rect bnd2;
                if (SecondSurfaceCurve != null) bnd2 = SecondSurfaceCurve.Data.Bounds;
                else bnd2 = new Rect(bnd1.Left, bnd1.Top, 0, 0);
                Canvas.SetLeft(Guides, Math.Min(bnd1.Left,bnd2.Left));
                Canvas.SetTop(Guides, Math.Min(bnd1.Top, bnd2.Top));
                Guides.Width = Math.Max(bnd1.Right, bnd2.Right) - Math.Min(bnd1.Left, bnd2.Left);
                Guides.Height = Math.Max(bnd1.Bottom, bnd2.Bottom) - Math.Min(bnd1.Top, bnd2.Top);
                DrawingSurface.Children.Add(Guides);
            }

            if (mode == DrawMode.Edit)
            {

                foreach (Ellipse elps in SurfaceShapes)
                {
                    DrawingSurface.Children.Add(elps);
                }
            }
            //------------------------------
        }

        #region Marker
        public override void SetMarker(int where)
        {
            sMarker.iTag = where;
            sMarker.SpaceValue = FirstSpacePoints[where];
            sMarker.DrawPosition = FirstSurfacePoints[where];
        }

        public override Point3D GetMarkedPoint()
        {
            return FirstSpacePoints[sMarker.iTag];
        }

        public override void IncMarkerTag(int ii)
        {
            sMarker.iTag = (FirstSpacePoints.Count()+sMarker.iTag + ii) % FirstSpacePoints.Count();
            SetMarker(sMarker.iTag);
        }
        #endregion

        /// <summary>
        /// Непосредственно грузит данные из Delcam в нужный момент
        /// </summary>
        ///  <param name="sender">Отправитель события, если надо будет использовать метод именно так</param>
        /// <param name="e">Не существенные параметры события</param>
        public override void UpdateDataPoints(object sender, EventArgs e)
        {
            Point3D[][] res =((ModelLogicInteractor)sender).GetXXCurve(Viewtype);
            if (res != null)
            {
                FirstSpacePoints = res[0];
                FirstDataPoints = new Point[FirstSpacePoints.Count()];
            }
            else
            {
                FirstDataPoints = null;
                return;
            }
            if ((res.Count() > 1) && (res[1].Count() > 0))
            {
                SecondSpacePoints = res[1];
                SecondDataPoints = new Point[SecondSpacePoints.Count()];
            }
            else SecondDataPoints = null;
            switch (Viewtype)
            {
                case (PlaneType.XZ):
                    {
                        for (int i = 0; i < FirstSpacePoints.Count(); i++) FirstDataPoints[i] = new Point(FirstSpacePoints[i].X, FirstSpacePoints[i].Z);
                        if (SecondSpacePoints!=null)
                            for (int i = 0; i < SecondSpacePoints.Count(); i++) SecondDataPoints[i] = new Point(SecondSpacePoints[i].X, SecondSpacePoints[i].Z);
                        break;
                    }
                case (PlaneType.XY):
                    {
                        for (int i = 0; i < FirstSpacePoints.Count(); i++) FirstDataPoints[i] = new Point(FirstSpacePoints[i].X, FirstSpacePoints[i].Y);
                        if (SecondSpacePoints != null)
                            for (int i = 0; i < SecondSpacePoints.Count(); i++) SecondDataPoints[i] = new Point(SecondSpacePoints[i].X, SecondSpacePoints[i].Y);
                        break;
                    }
                case (PlaneType.YZ):
                    {
                        for (int i = 0; i < FirstSpacePoints.Count(); i++) FirstDataPoints[i] = new Point(FirstSpacePoints[i].Y, FirstSpacePoints[i].Z);
                        if (SecondSpacePoints != null)
                            for (int i = 0; i < SecondSpacePoints.Count(); i++) SecondDataPoints[i] = new Point(SecondSpacePoints[i].Y, SecondSpacePoints[i].Z);
                        break;
                    }
                case (PlaneType.Custom):
                    {
                        for (int i = 0; i < FirstSpacePoints.Count(); i++) FirstDataPoints[i] = new Point(FirstSpacePoints[i].Y, FirstSpacePoints[i].Z);
                        if (SecondSpacePoints != null)
                            for (int i = 0; i < SecondSpacePoints.Count(); i++) SecondDataPoints[i] = new Point(SecondSpacePoints[i].Y, SecondSpacePoints[i].Z);
                        break;
                    }
                case (PlaneType.Section):
                    {
                        for (int i = 0; i < FirstSpacePoints.Count(); i++) FirstDataPoints[i] = new Point(FirstSpacePoints[i].Y, FirstSpacePoints[i].Z);
                        if (SecondSpacePoints != null)
                            for (int i = 0; i < SecondSpacePoints.Count(); i++) SecondDataPoints[i] = new Point(SecondSpacePoints[i].Y, SecondSpacePoints[i].Z);
                        break;
                    }
                default: throw new CustomLogicException("Sorcery", CustomLogicException.WTF.IncorrectData);
            }
            SetMins();
            RecountSurfacePoints();
            if (SelVisible) SetMarker(0);

            //----------------------------------------------------
            SurfaceShapes = new List<Ellipse>(2*FirstSurfacePoints.Count()); 
            //Лучше грубо оценить capacity, чем лишний if писать

            xdefaultstep = CalcStep(RealX(DrawingSurface.ActualWidth) - RealX(0), gridnum);
            ydefaultstep = CalcStep(RealY(DrawingSurface.ActualHeight) - RealY(0), gridnum);

            SurfaceResize(this, null); 
        }

        protected override void RecountSurfacePoints()
        {
            FirstSurfacePoints = new Point[FirstDataPoints.Count()];
            for (int i = 0; i < FirstDataPoints.Count(); i++)
            {
                FirstSurfacePoints[i] = RescalePoint(FirstDataPoints[i]);
            }
            if (SecondDataPoints != null)
            {
                SecondSurfacePoints = new Point[SecondDataPoints.Count()];
                for (int i = 0; i < SecondDataPoints.Count(); i++)
                {
                    SecondSurfacePoints[i] = RescalePoint(SecondDataPoints[i]);
                }
            }
            else SecondSurfacePoints = null;
            if (SelVisible) SetMarker(sMarker.iTag);
        }

        /// <summary>
        ///  Очищает канву вывода от всего, что на ней есть
        /// </summary>
        public void Clear() { DrawingSurface.Children.Clear(); SurfaceShapes.Clear(); }
    }

    /// <summary>
    /// Для масштабов специально. Вообще в класс удобно вынести
    /// любые синхронизации, которые трудоемко и витиевато будет писать в основном коде.
    /// </summary>
    public sealed class SectionSynchronizer
    {
        private const double Offset = 0.05;  //"Поля", %
        private BasePlane _XZ, _YZ, _XY;
        /// <summary>
        /// Конструктор, смотрим названия переменных и задаем соответственно.
        /// </summary>
        /// <param name="XZ">1е</param>
        /// <param name="YZ">2е</param>
        /// <param name="XY">3е</param>
        public SectionSynchronizer(BasePlane XZ, BasePlane YZ, BasePlane XY)
        {
            _XZ = XZ; _YZ = YZ; _XY = XY; UserPoints = null;
        }
        /// <summary>
        /// Синхронизирует масштабы сечений
        /// </summary>
        void SyncAllPlanes()
        {
            BasePlane.ScaleInfo tmp; Point gs;
            //А вот тут с переменными начинается жжеесть...
            //Выполнена тут неполностью синхронизация, только _необходимая_. Можно дополнить потом, если нужно

            tmp = _XZ.ScaleParams;
            gs = _XZ.GridSteps;
            if (_YZ.Viewtype != BasePlane.PlaneType.Section)
            {
                _XZ.SetMins();
                _YZ.SetMins();
                tmp = _YZ.ScaleParams;
                double Ymx = Math.Max(_XZ.ScaleParams.Y_max, tmp.Y_max); Ymx = Math.Max(Ymx, 0);
                double Ymn = Math.Min(_XZ.ScaleParams.Y_min, tmp.Y_min); Ymn = Math.Min(Ymn, 0);
                //double slen = Math.Max(_XZ.ScaleParams.SurfLen, tmp.SurfLen);
                double slen = (Ymx - Ymn) * (1 + Offset);
                tmp.SurfLen = slen;
                Ymn -= tmp.SurfLen * (Offset / 2);
                Ymx += tmp.SurfLen * (Offset / 2);
                tmp.Y_max = Ymx;
                tmp.Y_min = Ymn;
                double scr = Math.Min(_XZ.ScaleParams.SizeOfScreen, tmp.SizeOfScreen);
                //tmp.SizeOfScreen = _XZ.ScaleParams.SizeOfScreen;
                tmp.SizeOfScreen = scr;
                _YZ.ScaleParams = tmp;

                tmp = _XZ.ScaleParams;
                tmp.Y_max = Ymx;
                tmp.Y_min = Ymn;
                tmp.SurfLen = slen;
                tmp.SizeOfScreen = scr;
                _XZ.ScaleParams = tmp;

                gs = _YZ.GridSteps;
                gs.Y = _XZ.GridSteps.Y;
                _YZ.GridSteps = gs;
            }

            tmp = _XY.ScaleParams;
            tmp.X_max = _XZ.ScaleParams.X_max;
            tmp.X_min = _XZ.ScaleParams.X_min;
            tmp.SurfLen = _XZ.ScaleParams.SurfLen;
            tmp.SizeOfScreen = _XZ.ScaleParams.SizeOfScreen;
            _XY.ScaleParams = tmp;
            gs = _XY.GridSteps;
            gs.X = _XZ.GridSteps.X;
            _XY.GridSteps = gs;
            //---------------------
            _XZ.SetMarker(_XZ.sMarker.iTag);
            _YZ.SetMarker(_YZ.sMarker.iTag);

            ((CurvePlane)_XZ).StrictScale = (((CurvePlane)_XZ).AmITooBig());

            _XZ.RedrawAll();
            _XY.RedrawAll();
            _YZ.RedrawAll();
            if (UserCurve != null) UserCurve.DrawCurve();

        }
        public void ResizeHappened(object sender, EventArgs e) { SyncAllPlanes(); /*Вынесено в 2 ф-ции на случай дополнительных действий*/ }

        //-------------------------------------
        public BasePlane.ScaleInfo SyncUserSelection()
        {
            BasePlane.ScaleInfo scale;
            scale.X_max = _XZ.ScaleParams.X_max;
            scale.X_min = _XZ.ScaleParams.X_min;
            scale.SurfLen = Math.Max(_XZ.ScaleParams.SurfLen, _XY.ScaleParams.SurfLen);
            scale.SizeOfScreen = Math.Min(_XZ.ScaleParams.SizeOfScreen, _XY.ScaleParams.SizeOfScreen);
            scale.Y_max = _XY.ScaleParams.Y_max;
            scale.Y_min = _XY.ScaleParams.Y_min;
            return scale;
        }
        private SolePointCurve UserPoints;

        internal SolePointCurve UserCurve
        {
            get { return UserPoints; }
            set { UserPoints = value; }
        }
    }
}

