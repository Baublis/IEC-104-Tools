using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using lib60870.CS101;
using lib60870.CS104;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace IEC_104_Tools
{

    /// <summary>
    /// Логика взаимодействия для ConParameters.xaml
    /// </summary>
    public partial class ConParameters : Window
    {
        APCIParameters aPCIParameters;
        ApplicationLayerParameters applicationLayerParameters;
        double t_con;

        public ConParameters()
        {
            InitializeComponent();

        }

        private void port_PreviewKeyDown_Event(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int int_Key = ((int)e.Key);
            if (int_Key != 2 && (int_Key < 34 || int_Key > 43) && (int_Key < 74 || int_Key > 83))
                e.Handled = true;
        }

        private void CheckBox_OA_Checked(object sender, RoutedEventArgs e)
        {
            if (p_OA != null)
                p_OA.IsEnabled = true;
        }

        private void CheckBox_OA_Unchecked(object sender, RoutedEventArgs e)
        {
            if(p_OA != null)
            {
                p_OA.Text = "0";
                p_OA.IsEnabled = false;
            }         
        }

        private void RadioButton_CA_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton pressed = (RadioButton)sender;
            applicationLayerParameters.SizeOfCA = Convert.ToInt32(pressed.Content.ToString());
        }

        private void RadioButton_IOA_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton pressed = (RadioButton)sender;
            applicationLayerParameters.SizeOfIOA = Convert.ToInt32(pressed.Content.ToString());
        }

        private void load_data()
        {
            p_k.Text = aPCIParameters.K.ToString();
            p_w.Text = aPCIParameters.W.ToString();
            p_t0.Text = aPCIParameters.T0.ToString();
            p_t1.Text = aPCIParameters.T1.ToString();
            p_t2.Text = aPCIParameters.T2.ToString();
            p_t3.Text = aPCIParameters.T3.ToString();
            p_t_con.Text = t_con.ToString();
            p_OA.Text = applicationLayerParameters.OA.ToString();
            if (applicationLayerParameters.OA > 0)
                CheckBox_p_OA.IsChecked = true;
            switch (applicationLayerParameters.SizeOfCA)
            {
                case 1:
                    RadioButton_CA_1.IsChecked = true;
                    break;
                case 2:
                    RadioButton_CA_2.IsChecked = true;
                    break;
            }
            switch (applicationLayerParameters.SizeOfIOA)
            {
                case 1:
                    RadioButton_IOA_1.IsChecked = true;
                    break;
                case 2:
                    RadioButton_IOA_2.IsChecked = true;
                    break;
                case 3:
                    RadioButton_IOA_3.IsChecked = true;
                    break;
            }
        }

        private void upload_data()
        {
            aPCIParameters.K = Convert.ToInt32(p_k.Text);
            aPCIParameters.W = Convert.ToInt32(p_w.Text);
            aPCIParameters.T0 = Convert.ToInt32(p_t0.Text);
            aPCIParameters.T1 = Convert.ToInt32(p_t1.Text);
            aPCIParameters.T2 = Convert.ToInt32(p_t2.Text);
            aPCIParameters.T3 = Convert.ToInt32(p_t3.Text);
            applicationLayerParameters.OA = Convert.ToInt32(p_OA.Text);
            t_con = Convert.ToDouble(p_t_con.Text);
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            upload_data();
            frm.aPCIParameters = aPCIParameters;
            frm.applicationLayerParameters = applicationLayerParameters;
            frm.t_con = t_con;
            if (frm.con != null)
                if (frm.con.IsRunning)
                {
                    frm.disconnection();
                    Thread.Sleep(50);
                    frm.connection();
                }         
            this.Close();
        }

        private void Button_default_Click(object sender, RoutedEventArgs e)
        {
            t_con = 50;
            aPCIParameters = new APCIParameters();
            applicationLayerParameters = new ApplicationLayerParameters();
            load_data();
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            aPCIParameters = frm.aPCIParameters;
            applicationLayerParameters = frm.applicationLayerParameters;
            t_con = frm.t_con;
            load_data();
        }
    }
}
