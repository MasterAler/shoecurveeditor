using System.Windows;
using System;

namespace ShoeCurveEditor
{
    /// <summary>
    /// Interaction logic for MainToolBox.xaml
    /// </summary>
    public partial class ParamSettingBox : Window
    {
        public ParamSettingBox()
        {
            InitializeComponent();
            //80 40 0.88 -0.8 0.85 -0.81
            maxz=80;  minz=42;  max_norm_z_up=0.88;  max_norm_z_down=-0.8;  min_norm_z_up=0.85;  min_norm_z_down=-0.81;
            UDmaxz.Value = maxz;
            UDminz.Value = minz;
            UDmaxunorm.Value = max_norm_z_up;
            UDmaxdnorm.Value = max_norm_z_down;
            UDminunorm.Value = min_norm_z_up;
            UDmindnorm.Value = min_norm_z_down;
        }

        double maxz, minz, max_norm_z_up, max_norm_z_down, min_norm_z_up, min_norm_z_down;

        public event EventHandler ParamsChanged;

        protected virtual void OnParamsChanged(EventArgs e)
        {
            EventHandler handler = ParamsChanged;
            if (handler != null) { handler(this, e); }
        }

        #region SixProperties

        public double Min_ZNorm_Down
        {
            get { return min_norm_z_down; }
            set { min_norm_z_down = value; }
        }

        public double Min_ZNorm_Up
        {
            get { return min_norm_z_up; }
            set { min_norm_z_up = value; }
        }

        public double Max_ZNorm_Down
        {
            get { return max_norm_z_down; }
            set { max_norm_z_down = value; }
        }

        public double Max_ZNorm_Up
        {
            get { return max_norm_z_up; }
            set { max_norm_z_up = value; }
        }

        public double MinZ
        {
            get { return minz; }
            set { minz = value; }
        }

        public double MaxZ
        {
            get { return maxz; }
            set { maxz = value; }
        }
        #endregion

        private void TokBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UDmaxz.Value != null) maxz = UDmaxz.Value.Value;
            if (UDminz.Value != null) minz = UDminz.Value.Value;
            if (UDmaxunorm.Value != null) max_norm_z_up = UDmaxunorm.Value.Value;
            if (UDmaxdnorm.Value != null) max_norm_z_down = UDmaxdnorm.Value.Value;
            if (UDminunorm.Value != null) min_norm_z_up = UDminunorm.Value.Value;
            if (UDmindnorm.Value != null) min_norm_z_down = UDmindnorm.Value.Value;
            ParamWnd.Hide();
            OnParamsChanged(null);
        }

        private void ToolWnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ParamWnd.Hide();
            e.Cancel = true;
        }

        private void ToolWnd_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ParamWnd.IsVisible)
            {
                UDmaxz.Value = maxz;
                UDminz.Value = minz;
                UDmaxunorm.Value = max_norm_z_up;
                UDmaxdnorm.Value = max_norm_z_down;
                UDminunorm.Value = min_norm_z_up;
                UDmindnorm.Value = min_norm_z_down;
            }
        }
    }
}
