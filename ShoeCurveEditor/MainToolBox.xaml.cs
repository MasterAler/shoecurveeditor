using System.Windows;

namespace ShoeCurveEditor
{
    /// <summary>
    /// Interaction logic for MainToolBox.xaml
    /// </summary>
    public partial class MainToolBox : Window
    {
        public MainToolBox()
        {
            InitializeComponent();
            sh_k = 1.0;
            trn_k = 0.3;
            mark_s = 1;
            sensUD.Value = sh_k;
            turnsensUD.Value = trn_k;
            iUDMark.Value = mark_s;
        }

        private double sh_k;

        public double ShiftSens
        {
            get { return sh_k; }
            set { sh_k = value; }
        }
        private double trn_k;

        public double TurnSens
        {
            get { return trn_k; }
            set { trn_k = value; }
        }

        private int mark_s;

        public int MarkSens
        {
            get { return mark_s; }
            set { mark_s = value; }
        }

        private void TokBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sensUD.Value!=null) ShiftSens = sensUD.Value.Value;
            if (turnsensUD.Value!=null) TurnSens = turnsensUD.Value.Value;
            if (iUDMark.Value != null) MarkSens = iUDMark.Value.Value;
            ToolWnd.Hide();
        }

        private void ToolWnd_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ToolWnd.Hide();
            e.Cancel = true;
        }

        private void ToolWnd_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ToolWnd.IsVisible)
            {
                sensUD.Value = sh_k;
                turnsensUD.Value = trn_k;
                iUDMark.Value = mark_s;
            }
        }
    }
}
