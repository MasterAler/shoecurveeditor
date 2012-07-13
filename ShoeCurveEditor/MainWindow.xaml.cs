using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;

namespace ShoeCurveEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainAboutBox Info;
        private MainToolBox SettingsBox;
        private ParamSettingBox ParamBox;
        ModelLogicInteractor MainLogic;
        ModelObjViewer plane3D;
        SolePointCurve SoleCurve;

        private CurvePlane planeXZ;
        private DoubleCurve planeYZ,planeXY;   // YZ на самом деле Custom 
        public SectionSynchronizer MainSync;
        private SolidColorBrush SelectColor;
        private SolidColorBrush OldColor;

        public static RoutedCommand ArrowLeft = new RoutedCommand();
        public static RoutedCommand ArrowRight = new RoutedCommand();
        public static RoutedCommand SpaceSel = new RoutedCommand();
        public static RoutedCommand HSel = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();
            Info = new MainAboutBox();
            SettingsBox = new MainToolBox();
            ParamBox = new ParamSettingBox();
            Keyboard.AddKeyDownHandler(this, new KeyEventHandler(ActionKeyCheck));
            Keyboard.AddKeyUpHandler(this, new KeyEventHandler(ActionKeyCheck));
            SelectColor = Brushes.Orange;
            MainLogic = new ModelLogicInteractor();
            //------------------GUI Bindings---------------------------------------------------------
            this.CommandBindings.Add(new CommandBinding(ArrowLeft,LeftPressed));
            this.InputBindings.Add(new InputBinding(ArrowLeft, new KeyGesture(Key.Left, ModifierKeys.None)));
            this.CommandBindings.Add(new CommandBinding(ArrowRight, RightPressed));
            this.InputBindings.Add(new InputBinding(ArrowRight, new KeyGesture(Key.Right, ModifierKeys.None)));
            this.CommandBindings.Add(new CommandBinding(SpaceSel, SpacePressed));
            this.InputBindings.Add(new InputBinding(SpaceSel, new KeyGesture(Key.Space, ModifierKeys.None)));
            //--------
            this.CommandBindings.Add(new CommandBinding(HSel, HPressed));
            this.InputBindings.Add(new InputBinding(HSel, new KeyGesture(Key.H, ModifierKeys.Control)));
        }

        private void LeftPressed(object sender, ExecutedRoutedEventArgs e)
        {
            mPlane.IncMarkerTag(-SettingsBox.MarkSens);
            
        }

        private void RightPressed(object sender, ExecutedRoutedEventArgs e)
        {
            mPlane.IncMarkerTag(SettingsBox.MarkSens);
        }

        private void SpacePressed(object sender, ExecutedRoutedEventArgs e)
        {
            mPlane.sMarker.TryAddPoint(mPlane.GetMarkedPoint());
        }

        private void HPressed(object sender, ExecutedRoutedEventArgs e)
        {
            mPlane.sMarker.TryAddPoint(mPlane.GetMarkedPoint(),true);
        }

        private void ActionKeyCheck(Object sender, KeyEventArgs e)
        {
            StringBuilder mrk = new StringBuilder("Keys: ");
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) mrk.Append("LCtrl ");
            if (Keyboard.IsKeyDown(Key.LeftShift)) mrk.Append("LShift");
            KeyLabel.Content = mrk.ToString();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BindCurves()
        {
            cnvXZ.MouseLeftButtonDown += new MouseButtonEventHandler(planeXZ.MouseDown);
            cnvXZ.MouseMove += new MouseEventHandler(planeXZ.MouseMove);
            cnvXZ.MouseLeftButtonUp += new MouseButtonEventHandler(planeXZ.MouseUp);
            MainPrjWindow.SizeChanged += new SizeChangedEventHandler(planeXZ.SurfaceResize);

            cnvYZ.MouseLeftButtonDown += new MouseButtonEventHandler(planeYZ.MouseDown);
            cnvYZ.MouseMove += new MouseEventHandler(planeYZ.MouseMove);
            cnvYZ.MouseLeftButtonUp += new MouseButtonEventHandler(planeYZ.MouseUp);
            MainPrjWindow.SizeChanged += new SizeChangedEventHandler(planeYZ.SurfaceResize);

            cnvXY.MouseLeftButtonDown += new MouseButtonEventHandler(planeXY.MouseDown);
            cnvXY.MouseMove += new MouseEventHandler(planeXY.MouseMove);
            cnvXY.MouseLeftButtonUp += new MouseButtonEventHandler(planeXY.MouseUp);
            MainPrjWindow.SizeChanged += new SizeChangedEventHandler(planeXY.SurfaceResize);
        }

        private void ReCreateObjects()
        {
            planeXZ = new CurvePlane(cnvXZ, CurvePlane.PlaneType.XZ);
            planeYZ = new DoubleCurve(cnvYZ, CurvePlane.PlaneType.Custom);
            planeXY = new DoubleCurve(cnvXY, BasePlane.PlaneType.XY);
            planeXZ.CurveStyle = CurvePlane.LineMode.Segments;
            planeYZ.CurveStyle = CurvePlane.LineMode.Segments;
            planeXY.CurveStyle = BasePlane.LineMode.Segments;
            planeYZ.ShowCenter = false;
            planeXY.ShowCenter = true;
            planeXY.ShowSecSelect = false;
            planeXY.ShowLines = false;
            planeXY.SelVisible = false;
            planeXY.CurveShowMode = BasePlane.DrawMode.Edit;
            planeYZ.ShowCenter = true;
            planeYZ.ShowSecSelect = false;
            planeXZ.ShowGuidelines = false;
            BindCurves();
        }

        private void MainPrjWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainPrjWindow.WindowState = WindowState.Normal;
            System.Drawing.Rectangle scr = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            MainPrjWindow.Left = scr.Left;
            MainPrjWindow.Top = scr.Top;
            MainPrjWindow.Width = scr.Width;
            MainPrjWindow.Height = scr.Height;
            MainPrjWindow.Title = "ShoeCurveEditor Demo";
            //----------------
            ReCreateObjects();
            StyleItem.IsEnabled = false;
            SettingsBox.Owner=this;
            MainSync = new SectionSynchronizer(planeXZ, planeYZ, planeXY);
            SoleCurve = new SolePointCurve(cnvXY);
            SoleCurve.mSyncer = MainSync;
            IsShifted = false;
            //--------
            plane3D = new ModelObjViewer(ModelViewer);
            planeXY.IsVisible = false;
            ShowIt_Click(this, null);
        }

        private void MenuItemStyle_Click(object sender, RoutedEventArgs e)
        {
            if (planeXZ.CurveStyle == CurvePlane.LineMode.Spline) planeXZ.CurveStyle = CurvePlane.LineMode.Segments;
            else planeXZ.CurveStyle = CurvePlane.LineMode.Spline;

            if (planeYZ.CurveStyle == CurvePlane.LineMode.Spline) planeYZ.CurveStyle = CurvePlane.LineMode.Segments;
            else planeYZ.CurveStyle = CurvePlane.LineMode.Spline;

            if (planeXY.CurveStyle == CurvePlane.LineMode.Spline) planeXY.CurveStyle = CurvePlane.LineMode.Segments;
            else planeXY.CurveStyle = CurvePlane.LineMode.Spline;

            planeXZ.DrawXXCurve();
            planeYZ.DrawXXCurve();
            planeXY.DrawXXCurve();
        }

        private bool IsTurnedXZ, IsTurnedXY, IsTurnedYZ;
        //написано "угол", но он параметрический!!
        private double angle0;
        private Point m_pos, m_pos0;
        private bool IsShifted;


        private void MainPrjWindow_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown(0);
        }

        private void MainPrjWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (planeXZ != null) planeXZ.SurfaceResize(sender, null);
            if (planeYZ != null) planeYZ.SurfaceResize(sender, null);
            if (planeXY != null) planeXY.SurfaceResize(sender, null);
        }

        private void PlanesSignUp(bool xysign)
        {
            //-------------------------Подписки на данные----------------------------//
            planeXZ.UpdateDataPoints(MainLogic, null);
            planeXZ.SurfaceResize(this, null);
            planeYZ.UpdateDataPoints(MainLogic, null);
            planeYZ.SurfaceResize(this, null);
            planeXZ.FigureMoved += new EventHandler<BasePlane.MoveFigureEventArgs>(MainLogic.MoveKolodka);
            planeXZ.RotationCenterChanged += new EventHandler<BasePlane.RotationCenterEventArgs>(MainLogic.RotationCenterMove);
            planeXZ.FigureRotated += new EventHandler<BasePlane.RotateFigureEventArgs>(MainLogic.RotateModel);
            planeYZ.FigureMoved += new EventHandler<BasePlane.MoveFigureEventArgs>(MainLogic.MoveKolodka);
            planeYZ.RotationCenterChanged += new EventHandler<BasePlane.RotationCenterEventArgs>(MainLogic.RotationCenterMove);
            planeYZ.FigureRotated += new EventHandler<BasePlane.RotateFigureEventArgs>(MainLogic.RotateModel);
            planeXZ.DataResized += new EventHandler(MainSync.ResizeHappened);
            planeYZ.DataResized += new EventHandler(MainSync.ResizeHappened);

            if (xysign)
            {
                planeXY.UpdateDataPoints(MainLogic, null);
                planeXY.SurfaceResize(this, null);
                planeXY.FigureMoved += new EventHandler<BasePlane.MoveFigureEventArgs>(MainLogic.MoveKolodka);
                planeXY.RotationCenterChanged += new EventHandler<BasePlane.RotationCenterEventArgs>(MainLogic.RotationCenterMove);
                planeXY.FigureRotated += new EventHandler<BasePlane.RotateFigureEventArgs>(MainLogic.RotateModel);
                planeXY.DataResized += new EventHandler(MainSync.ResizeHappened);
            }

            ParamBox.ParamsChanged += new EventHandler(MainLogic.CnangeXYparams);
            planeXZ.sMarker.NewPointSelected+=new EventHandler<SelectMarker.PointSelectedEventArgs>(SoleCurve.PointAdded);
            planeYZ.sMarker.NewPointSelected += new EventHandler<SelectMarker.PointSelectedEventArgs>(SoleCurve.PointAdded);
            MainSync.UserCurve = SoleCurve;
        }

        private void MenuOpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooser = new OpenFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"Mesh Files|*.obj|PS Files|*.psmodel|STL Files|*.stl|DMT Files|*.dmt|All Files|*.*";
            chooser.FilterIndex = 0;
            chooser.Multiselect = false;
            if (chooser.ShowDialog().Value)
            {
                MainLogic.LoadFile(chooser.FileName);
                //MainLogic.input_move();
                MainLogic.DataChanged += new EventHandler(planeXZ.UpdateDataPoints);
                //MainLogic.DataChanged += new EventHandler(planeXY.UpdateDataPoints);
                MainLogic.DataChanged += new EventHandler(planeYZ.UpdateDataPoints);
                MainLogic.DataChanged += new EventHandler(plane3D.Update3DPicture);
                PlanesSignUp(false);
                StyleItem.IsEnabled = true;
                MainPrjWindow.WindowState = WindowState.Maximized;
                MainPrjWindow.ResizeMode = ResizeMode.CanResize;

                plane3D.LoadViewData(MainLogic.GetFaces(), MainLogic.GetPoints());
                MainSync.ResizeHappened(planeXZ, null);
            }
        }

        private void ImportItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooser = new OpenFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"Mesh Files|*.obj|PS Files|*.psmodel|STL Files|*.stl|DMT Files|*.dmt|All Files|*.*";
            chooser.FilterIndex = 0;
            chooser.Multiselect = false;
            if (chooser.ShowDialog().Value)
            {
                MainLogic.LoadFile(chooser.FileName);
                MainLogic.input_move();
                MainLogic.DataChanged += new EventHandler(planeXZ.UpdateDataPoints);
                MainLogic.DataChanged += new EventHandler(planeXY.UpdateDataPoints);
                MainLogic.DataChanged += new EventHandler(planeYZ.UpdateDataPoints);
                MainLogic.DataChanged += new EventHandler(plane3D.Update3DPicture);
                PlanesSignUp(true);
                StyleItem.IsEnabled = true;
                sel2.IsEnabled = true;
                mPlane = planeYZ;
                MainPrjWindow.WindowState = WindowState.Maximized;
                MainPrjWindow.ResizeMode = ResizeMode.CanResize;

                plane3D.LoadViewData(MainLogic.GetFaces(), MainLogic.GetPoints());
                MainSync.ResizeHappened(planeXZ, null);
            }
        }
        
        #region MouseStuff
        //Хитро перепроектировано в пользу компактности и сопровождаемости!

        private void cnv_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Canvas)) return;
            BasePlane cur_plane;
            System.Windows.Controls.Canvas cur_cnv = (System.Windows.Controls.Canvas)sender;
            string sTag = (cur_cnv).Tag.ToString();
            switch (sTag)
            {
                case ("XZ"):
                    {
                        cur_plane = planeXZ;
                        if (IsTurnedXZ)
                        {
                            IsTurnedXZ = false;
                            double angle;
                            Point l_mpos = e.GetPosition(cur_cnv);
                            angle = Math.Sign(l_mpos.X - planeXZ.RotationPoint.X) * (l_mpos.Y - angle0) * SettingsBox.TurnSens;
                            cur_plane.CurrentCurveColor = OldColor;
                            cur_plane.DrawXXCurve();
                            cur_plane.MakeFigureTurned(angle);
                            AngleLabel.Content = "Rotation Angle: 0";
                            MessageBox.Show("Tuned on " + Convert.ToString(angle));
                            ActionKeyCheck(this, null);
                        }
                        break;
                    }
                case ("YZ"):
                    {
                        cur_plane = planeYZ;
                        if (IsTurnedYZ)
                        {
                            IsTurnedYZ = false;
                            double angle;
                            Point l_mpos = e.GetPosition(cur_cnv);
                            angle = Math.Sign(-l_mpos.Y + planeYZ.RotationPoint.Y) * (l_mpos.X - angle0) * SettingsBox.TurnSens;
                            cur_plane.CurrentCurveColor = OldColor;
                            cur_plane.DrawXXCurve();
                            cur_plane.MakeFigureTurned(angle);
                            AngleLabel.Content = "Rotation Angle: 0";
                            MessageBox.Show("Tuned on " + Convert.ToString(angle));
                            ActionKeyCheck(this, null);
                        }
                        break;
                    }
                case ("XY"):
                    {
                        cur_plane = planeXY;
                        if (IsTurnedXY)
                        {
                            IsTurnedXY = false;
                            double angle;
                            Point l_mpos = e.GetPosition(cur_cnv);
                            angle = (l_mpos.Y - angle0) * SettingsBox.TurnSens;
                            cur_plane.CurrentCurveColor = OldColor;
                            cur_plane.DrawXXCurve();
                            cur_plane.MakeFigureTurned(angle);
                            AngleLabel.Content = "Rotation Angle: 0";
                            MessageBox.Show("Tuned on " + Convert.ToString(angle));
                            ActionKeyCheck(this, null);
                        }
                        break;
                    }
                default: return;
            }
            if (IsShifted)
            {
                IsShifted = false;
                m_pos = e.GetPosition(cur_cnv);
                if ((m_pos.X == m_pos0.X) && (m_pos.Y == m_pos0.Y)) return;
                cur_plane.MakeFigureMoved(SettingsBox.ShiftSens * (m_pos.X - m_pos0.X),
                    SettingsBox.ShiftSens * (m_pos.Y - m_pos0.Y));
                cur_plane.CurrentCurveColor = OldColor;
                cur_plane.DrawXXCurve();
                ActionKeyCheck(this, null);
            }
        }

        private void cnv_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Canvas)) return;
            BasePlane cur_plane;
            System.Windows.Controls.Canvas cur_cnv=(System.Windows.Controls.Canvas)sender;
            string sTag = (cur_cnv).Tag.ToString();
            switch (sTag)
            {
                case ("XZ"):
                    {
                        cur_plane = planeXZ;
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            IsTurnedXZ = true;
                            angle0 = e.GetPosition(cur_cnv).Y;
                            OldColor = cur_plane.CurrentCurveColor;
                            cur_plane.CurrentCurveColor = SelectColor;
                            cur_plane.DrawXXCurve();
                        }
                        break;
                    }
                case ("YZ"):
                    {
                        cur_plane = planeYZ;
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            IsTurnedYZ = true;
                            angle0 = e.GetPosition(cur_cnv).X;
                            OldColor = cur_plane.CurrentCurveColor;
                            cur_plane.CurrentCurveColor = SelectColor;
                            cur_plane.DrawXXCurve();
                        }
                        break;
                    }
                case ("XY"):
                    {
                        cur_plane = planeXY;
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            IsTurnedXY = true;
                            angle0 = e.GetPosition(cur_cnv).Y;
                            OldColor = cur_plane.CurrentCurveColor;
                            cur_plane.CurrentCurveColor = SelectColor;
                            cur_plane.DrawXXCurve();
                        }
                        break;
                    }
                default: return;
            }
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                IsShifted = true;
                m_pos = e.GetPosition(cur_cnv);
                m_pos0 = m_pos;
                OldColor = cur_plane.CurrentCurveColor;
                cur_plane.CurrentCurveColor = SelectColor;
                cur_plane.DrawXXCurve();
            }
        }

        private void cnv_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Canvas)) return;
            BasePlane cur_plane;
            System.Windows.Controls.Canvas cur_cnv = (System.Windows.Controls.Canvas)sender;
            string sTag = (cur_cnv).Tag.ToString();
            switch (sTag)
            {
                case ("XZ"):
                    {
                        cur_plane = planeXZ;
                        if (IsTurnedXZ)
                        {
                            double cur_angle;
                            Point mpos = e.GetPosition(cur_cnv);
                            cur_angle = Math.Sign(mpos.X - planeXZ.RotationPoint.X) * (mpos.Y - angle0) * SettingsBox.TurnSens;
                            AngleLabel.Content = "Rotation Angle " + cur_angle.ToString();
                            planeXZ.DrawXXCurve();
                            planeXZ.ImgAngle = cur_angle;
                        }
                        break;
                    }
                case ("YZ"):
                    {
                        cur_plane = planeYZ;
                        if (IsTurnedYZ)
                        {
                            double cur_angle;
                            Point mpos = e.GetPosition(cur_cnv);
                            cur_angle = Math.Sign(-mpos.Y + planeYZ.RotationPoint.Y) * (mpos.X - angle0) * SettingsBox.TurnSens;
                            AngleLabel.Content = "Rotation Angle " + cur_angle.ToString();
                            planeYZ.DrawXXCurve();
                            planeYZ.ImgAngle = cur_angle;
                        }
                        break;
                    }
                case ("XY"):
                    {
                        cur_plane = planeXY;
                        if (IsTurnedXY)
                        {
                            double cur_angle;
                            Point mpos = e.GetPosition(cur_cnv);
                            cur_angle = Math.Sign(mpos.X - planeXY.RotationPoint.X) * (mpos.Y - angle0) * SettingsBox.TurnSens;
                            AngleLabel.Content = "Rotation Angle " + cur_angle.ToString();
                            planeXY.DrawXXCurve();
                            planeXY.ImgAngle = cur_angle;
                        }
                        break;
                    }
                default: return;
            }
            if (IsShifted)
            {
                double k = SettingsBox.ShiftSens;
                Point pt = e.GetPosition(cur_cnv);
                cur_plane.MoveY(k * (-m_pos.Y + pt.Y));
                cur_plane.MoveX(k * (-m_pos.X + pt.X));
                m_pos = pt;
                cur_plane.DrawXXCurve();
            }
        }

        private void cnv_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MainLogic == null) return;
            if (!(sender is System.Windows.Controls.Canvas)) return;
            string sTag = ((System.Windows.Controls.Canvas)sender).Tag.ToString();
            switch (sTag)
            {
                case ("XZ"):
                    {
                        planeXZ.CurrentCurveColor = Brushes.Brown;
                        planeXZ.DrawXXCurve();
                        cnv_MouseLeftButtonUp(cnvXZ, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
                        break;
                    }
                case ("YZ"):
                    {
                        planeYZ.CurrentCurveColor = Brushes.Brown;
                        planeYZ.DrawXXCurve();
                        cnv_MouseLeftButtonUp(cnvYZ, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
                        break;
                    }
                case ("XY"):
                    {
                        planeXY.CurrentCurveColor = Brushes.Brown;
                        planeXY.DrawXXCurve();
                        cnv_MouseLeftButtonUp(cnvXY, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
                        break;
                    }
                default: return;
            }
            IsShifted = false;
            IsTurnedXY = false;
            IsTurnedXZ = false;
            IsTurnedYZ = false;
        }

        #endregion

        #region ContextMenus
        private void GridXZ_Click(object sender, RoutedEventArgs e)
        {
            planeXZ.ShowGrid = !planeXZ.ShowGrid;
            planeXZ.SurfaceResize(this, null);
        }

        private void PointsXZ_Click(object sender, RoutedEventArgs e)
        {
            if (planeXZ.CurveShowMode == BasePlane.DrawMode.Edit) planeXZ.CurveShowMode = BasePlane.DrawMode.Show;
            else planeXZ.CurveShowMode = BasePlane.DrawMode.Edit;
            planeXZ.SurfaceResize(this, null);
        }

        private void GridYZ_Click(object sender, RoutedEventArgs e)
        {
            planeYZ.ShowGrid = !planeYZ.ShowGrid;
            planeYZ.SurfaceResize(this, null);
        }

        private void PointsYZ_Click(object sender, RoutedEventArgs e)
        {
            if (planeYZ.CurveShowMode == BasePlane.DrawMode.Edit) planeYZ.CurveShowMode = BasePlane.DrawMode.Show;
            else planeYZ.CurveShowMode = BasePlane.DrawMode.Edit;
            planeYZ.SurfaceResize(this, null);
        }

        private void GridXY_Click(object sender, RoutedEventArgs e)
        {
            planeXY.ShowGrid = !planeXY.ShowGrid;
            planeXY.SurfaceResize(this, null);
        }

        private void PointsXY_Click(object sender, RoutedEventArgs e)
        {
            if (planeXY.CurveShowMode == BasePlane.DrawMode.Edit) planeXY.CurveShowMode = BasePlane.DrawMode.Show;
            else planeXY.CurveShowMode = BasePlane.DrawMode.Edit;
            planeXY.SurfaceResize(this, null);
        }

        private void GuidelinesXZ_Click(object sender, RoutedEventArgs e)
        {
            planeXZ.ShowGuidelines = !planeXZ.ShowGuidelines;
            planeXZ.DrawXXCurve();
        }

        private void GuidelinesYZ_Click(object sender, RoutedEventArgs e)
        {
            planeYZ.ShowGuidelines = !planeYZ.ShowGuidelines;
            planeYZ.DrawXXCurve();
        }

        private void GuidelinesXY_Click(object sender, RoutedEventArgs e)
        {
            planeXY.ShowGuidelines = !planeXY.ShowGuidelines;
            planeXY.DrawXXCurve();
        }
        #endregion

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            Info.Show();
        }

        private void MenuItemPrefs_Click(object sender, RoutedEventArgs e)
        {
            SettingsBox.Show();
        }

        private void ShowSection_Click(object sender, RoutedEventArgs e)
        {
            if (planeYZ.Viewtype == BasePlane.PlaneType.Custom)
            {
                planeYZ.Viewtype = BasePlane.PlaneType.Section;
                planeYZ.ShowCenter = false;
                cnvYZ.MouseLeftButtonDown -= new MouseButtonEventHandler(planeYZ.MouseDown);
                cnvYZ.MouseMove -= new MouseEventHandler(planeYZ.MouseMove);
                cnvYZ.MouseLeftButtonUp -= new MouseButtonEventHandler(planeYZ.MouseUp);
            }
            else
            {
                planeYZ.Viewtype = BasePlane.PlaneType.Custom;
                planeYZ.ShowCenter = true;
                cnvYZ.MouseLeftButtonDown += new MouseButtonEventHandler(planeYZ.MouseDown);
                cnvYZ.MouseMove += new MouseEventHandler(planeYZ.MouseMove);
                cnvYZ.MouseLeftButtonUp += new MouseButtonEventHandler(planeYZ.MouseUp);
            }
            planeYZ.UpdateDataPoints(MainLogic, null);
        }

        private void params_Click(object sender, RoutedEventArgs e)
        {
            ParamBox.Show();
        }

        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog chooser = new SaveFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"Mesh Files|*.obj|All Files|*.*";
            chooser.FilterIndex = 0;
            if (chooser.ShowDialog().Value)
            {
                MainLogic.SaveToFile(chooser.FileName);
            }
        }

        private void bSTL2OBJ_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooser = new OpenFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"STL Files|*.stl";
            chooser.FilterIndex = 0;
            if (chooser.ShowDialog().Value)
            {
                SaveFileDialog saver = new SaveFileDialog();
                saver.CheckPathExists = true;
                saver.AddExtension = true;
                saver.Filter = @"Mesh Files|*.obj";
                saver.FilterIndex = 0;
                if (saver.ShowDialog().Value)
                {
                    StatusProgress.Value = 0;
                    StatusLabel.Content = "Convertion in progress.Wait, please...";
                    //-----------------------
                    BackgroundWorker worker = new BackgroundWorker();
                    // Ниже - это не ламерство, это _карринг_, попытка.
                    worker.DoWork += delegate(object wsender, DoWorkEventArgs arg)
                    { arg.Result = ModelFileConverter.ConvertBinSTL2OBJ(chooser.FileName, saver.FileName, wsender as BackgroundWorker); };
                    worker.RunWorkerCompleted += delegate(object wsender, RunWorkerCompletedEventArgs arg)
                    {
                        if (arg.Error != null) { MessageBox.Show(arg.Error.Message); return; }
                        if ((bool)arg.Result) MessageBox.Show("Done!", "Convertion");
                        else MessageBox.Show("Converting failed!((");
                        StatusLabel.Content = "";
                        StatusProgress.Value = 0;
                    };
                    worker.WorkerReportsProgress = true;
                    worker.ProgressChanged += delegate(object wsender, ProgressChangedEventArgs arg) { StatusProgress.Value = arg.ProgressPercentage; };
                    worker.RunWorkerAsync();
                }
            }
        }

        private void tSTL2OBJ_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog chooser = new OpenFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"STL Files|*.stl";
            chooser.FilterIndex = 0;
            if (chooser.ShowDialog().Value)
            {
                SaveFileDialog saver = new SaveFileDialog();
                saver.CheckPathExists = true;
                saver.AddExtension = true;
                saver.Filter = @"Mesh Files|*.obj";
                saver.FilterIndex = 0;
                if (saver.ShowDialog().Value)
                {
                    StatusProgress.Value = 0;
                    StatusLabel.Content = "Convertion in progress.Wait, please...";
                    //-----------------------
                    BackgroundWorker worker = new BackgroundWorker();
                    // Ниже - это не ламерство, это _карринг_, попытка.
                    worker.DoWork += delegate(object wsender, DoWorkEventArgs arg)
                    { arg.Result = ModelFileConverter.ConvertASCII_STL2OBJ(chooser.FileName, saver.FileName, wsender as BackgroundWorker); };
                    worker.RunWorkerCompleted += delegate(object wsender, RunWorkerCompletedEventArgs arg)
                    {
                        if (arg.Error != null) { MessageBox.Show(arg.Error.Message); return; }
                        if ((bool)arg.Result) MessageBox.Show("Done!", "Convertion");
                        else MessageBox.Show("Converting failed!((");
                        StatusLabel.Content = "";
                        StatusProgress.Value = 0;
                    };
                    worker.WorkerReportsProgress = true;
                    worker.ProgressChanged += delegate(object wsender, ProgressChangedEventArgs arg) { StatusProgress.Value = arg.ProgressPercentage; };
                    worker.RunWorkerAsync();
                }
            }
        }

        private void Shift_Click(object sender, RoutedEventArgs e)
        {
            planeYZ.HasShift = Shift.IsChecked;
            planeYZ.SurfaceResize(planeYZ, null);
        }

        private void OtherScale_Click(object sender, RoutedEventArgs e)
        {
            planeXZ.StrictScale = !planeXZ.StrictScale;
            planeYZ.StrictScale = !planeYZ.StrictScale;
            planeXZ.SetMins();
            planeYZ.SetMins();
            planeYZ.SurfaceResize(planeXZ, null);
            planeYZ.SurfaceResize(planeYZ, null);
        }

        private void ShowIt_Click(object sender, RoutedEventArgs e)
        {
            if (ShowIt.IsChecked)
            {
                cnvXY.MouseLeftButtonDown += new MouseButtonEventHandler(planeXY.MouseDown);
                cnvXY.MouseMove += new MouseEventHandler(planeXY.MouseMove);
                cnvXY.MouseLeftButtonUp += new MouseButtonEventHandler(planeXY.MouseUp);
                MainLogic.DataChanged += new EventHandler(planeXY.UpdateDataPoints);
                planeXY.FigureMoved += new EventHandler<BasePlane.MoveFigureEventArgs>(MainLogic.MoveKolodka);
                planeXY.RotationCenterChanged += new EventHandler<BasePlane.RotationCenterEventArgs>(MainLogic.RotationCenterMove);
                planeXY.FigureRotated += new EventHandler<BasePlane.RotateFigureEventArgs>(MainLogic.RotateModel);
                planeXY.DataResized += new EventHandler(MainSync.ResizeHappened);
                planeXY.IsVisible = true;
            }
            else
            {
                MainLogic.DataChanged -= new EventHandler(planeXY.UpdateDataPoints);
                cnvXY.MouseLeftButtonDown -= new MouseButtonEventHandler(planeXY.MouseDown);
                cnvXY.MouseMove -= new MouseEventHandler(planeXY.MouseMove);
                cnvXY.MouseLeftButtonUp -= new MouseButtonEventHandler(planeXY.MouseUp);
                planeXY.FigureMoved -= new EventHandler<BasePlane.MoveFigureEventArgs>(MainLogic.MoveKolodka);
                planeXY.RotationCenterChanged -= new EventHandler<BasePlane.RotationCenterEventArgs>(MainLogic.RotationCenterMove);
                planeXY.FigureRotated -= new EventHandler<BasePlane.RotateFigureEventArgs>(MainLogic.RotateModel);
                planeXY.DataResized -= new EventHandler(MainSync.ResizeHappened);
                planeXY.Clear();
                planeXY.IsVisible = false;
            }
        }

        private void MarkerYZ_Click(object sender, RoutedEventArgs e)
        {
            planeYZ.SelVisible = MarkerYZ.IsChecked;
        }

        private void MarkerXZ_Click(object sender, RoutedEventArgs e)
        {
            planeYZ.SelVisible = MarkerXZ.IsChecked;
        }

        BasePlane mPlane;

        private void sXZ_Click(object sender, RoutedEventArgs e)
        {
            sYZ.IsChecked = !sXZ.IsChecked;
            if (sYZ.IsChecked) mPlane = planeYZ;
            else mPlane = planeXZ;
        }

        private void sYZ_Click(object sender, RoutedEventArgs e)
        {
            sXZ.IsChecked = !sYZ.IsChecked;
            mPlane = planeXZ;
            if (sYZ.IsChecked) mPlane = planeYZ;
            else mPlane = planeXZ;
        }

        private void sole_mode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)sender;
            if (mi.Name == "sdPoints")
            {
                sdLines.IsChecked = !sdPoints.IsChecked;
                sdCurves.IsChecked = !sdPoints.IsChecked;
                SoleCurve.DrawingMode = PointCurve.DrawMode.Points;
            }
            else if (mi.Name == "sdLines")
            {
                sdPoints.IsChecked = !sdLines.IsChecked;
                sdCurves.IsChecked = !sdLines.IsChecked;
                SoleCurve.DrawingMode = PointCurve.DrawMode.Lines;
            }
            else
            {
                sdPoints.IsChecked = !sdCurves.IsChecked;
                sdLines.IsChecked = !sdCurves.IsChecked;
                SoleCurve.DrawingMode = PointCurve.DrawMode.Curves;
            }
            SoleCurve.DrawCurve();
        }

        private void SaveSole_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog chooser = new SaveFileDialog();
            chooser.CheckPathExists = true;
            chooser.AddExtension = true;
            chooser.Filter = @"Text Files|*.txt|All Files|*.*";
            chooser.FilterIndex = 0;
            chooser.FileName = "Sole.txt";
            if (chooser.ShowDialog().Value)
            {
                SoleCurve.SaveToFile(chooser.FileName);
            }
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            mgnALL.IsEnabled = !mgnALL.IsEnabled;
            if (mgnALL.IsEnabled) mgnALL.Visibility = System.Windows.Visibility.Visible;
            else mgnALL.Visibility = System.Windows.Visibility.Hidden;
        }
    //-------
    }
}
