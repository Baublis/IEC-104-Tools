using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using lib60870;
using lib60870.CS101;
using lib60870.CS104;


namespace IEC_104_Tools
{
    /// <summary>
    /// Логика взаимодействия для WindowParameters.xaml
    /// </summary>
    public partial class WindowParameters : Window
    {
        bool live = false;
        TypeID typeID;
        public DateTime myTime;
        public WindowParameters()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            live = true;
        }

        private void PreviewNumberInput(object sender, TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            if (Char.IsNumber(c))
                e.Handled = false;
            else
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        private void CA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 255)
                textbox.Text = "1";
        }

        private void KPA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 63)
                textbox.Text = "1";
        }

        private void IOA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 65535)
                textbox.Text = "1";
        }

        private void Commands_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "P_AC_NA_1 (113)":
                        {
                            QPA.Visibility = Visibility.Visible;
                            COT.Visibility = Visibility.Visible;
                            QPM.Visibility = Visibility.Hidden;
                            VALUE.Visibility = Visibility.Hidden;
                        }
                        break;
                    default:
                        {
                            QPA.Visibility = Visibility.Hidden;
                            COT.Visibility = Visibility.Hidden;
                            QPM.Visibility = Visibility.Visible;
                            VALUE.Visibility = Visibility.Visible;
                        }
                        break;
                }
            }
        }

        private void QPA_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Резерв (4-255)":
                        {
                            QPA_textbox.IsReadOnly = false;
                            QPA_textbox.Text = "4";
                        }
                        break;
                    default:
                        {
                            QPA_textbox.IsReadOnly = true;
                            QPA_textbox.Text = comboBox.SelectedIndex.ToString();
                        }
                        break;
                }
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            MainWindow frm = (MainWindow)this.Owner;
            ComboBoxItem selectedItem = (ComboBoxItem)type.SelectedItem;
            switch (selectedItem.Content.ToString())
            {
                case "P_ME_NA_1 (110)":
                    {
                        typeID = TypeID.P_ME_NA_1;
                        int kpa = Convert.ToInt32(KPA_textbox.Text);
                        int lpc = 64 * LPC.SelectedIndex;
                        int pop = 128 * POP.SelectedIndex;
                        byte qpm =   Convert.ToByte(kpa + lpc + pop);
                        float buf = float.Parse(value.Text.Replace('.', separator));
                        ParameterNormalizedValue cs = new ParameterNormalizedValue(Convert.ToInt32(IOA.Text), buf, qpm);
                        frm.send_Parameters(CauseOfTransmission.ACTIVATION, Convert.ToInt32(CA.Text), cs, value.Text, IOA.Text, null, typeID);
                    }
                    break;
                case "P_ME_NB_1 (111)":
                    {
                        typeID = TypeID.P_ME_NB_1;
                        int kpa = Convert.ToInt32(KPA_textbox.Text);
                        int lpc = 64 * LPC.SelectedIndex;
                        int pop = 128 * POP.SelectedIndex;
                        byte qpm = Convert.ToByte(kpa + lpc + pop);
                        int buf = Convert.ToInt32(value.Text);
                        ScaledValue scaledValue = new ScaledValue(buf);
                        ParameterScaledValue cs = new ParameterScaledValue(Convert.ToInt32(IOA.Text), scaledValue, qpm);
                        frm.send_Parameters(CauseOfTransmission.ACTIVATION, Convert.ToInt32(CA.Text), cs, value.Text, IOA.Text, null, typeID);
                    }
                    break;
                case "P_ME_NC_1 (112)":
                    {
                        typeID = TypeID.P_ME_NC_1;
                        int kpa = Convert.ToInt32(KPA_textbox.Text);
                        int lpc = 64 * LPC.SelectedIndex;
                        int pop = 128 * POP.SelectedIndex;
                        byte qpm = Convert.ToByte(kpa + lpc + pop);
                        float buf = float.Parse(value.Text.Replace('.', separator));
                        ParameterFloatValue cs = new ParameterFloatValue(Convert.ToInt32(IOA.Text), buf, qpm);
                        frm.send_Parameters(CauseOfTransmission.ACTIVATION, Convert.ToInt32(CA.Text), cs, value.Text, IOA.Text, null, typeID);
                    }
                    break;
                case "P_AC_NA_1 (113)":
                    {
                        typeID = TypeID.P_AC_NA_1;
                        byte qpa = Convert.ToByte(QPA_textbox.Text);
                        ParameterActivation cs = new ParameterActivation(Convert.ToInt32(IOA.Text), qpa);
                        if (COT_combo.SelectedIndex == 0)                                             
                            frm.send_Parameters(CauseOfTransmission.ACTIVATION, Convert.ToInt32(CA.Text), cs, null, IOA.Text, null, typeID);
                        else
                            frm.send_Parameters(CauseOfTransmission.DEACTIVATION, Convert.ToInt32(CA.Text), cs, null, IOA.Text, null, typeID);
                    }
                    break;
            }
        }

        private void KPA_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Резерв (5-63)":
                        {
                            KPA_textbox.IsReadOnly = false;
                            KPA_textbox.Text = "5";
                        }
                        break;
                    default:
                        {
                            KPA_textbox.IsReadOnly = true;
                            KPA_textbox.Text = comboBox.SelectedIndex.ToString();
                        }
                        break;
                }
            }
        }

        private void value_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)type.SelectedItem;
            TextBox textbox = (TextBox)sender;
            if ((selectedItem.Content.ToString() == "P_ME_NA_1 (110)" && Convert.ToInt32(textbox.Text) > 1) || textbox.Text == "")
                textbox.Text = "1";
        }

        private void value_float_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            ComboBoxItem selectedItem = (ComboBoxItem)type.SelectedItem;
            if (selectedItem.Content.ToString() == "P_ME_NB_1 (111)")
            {
                if (Char.IsNumber(c) || c == '-')
                    e.Handled = false;
                else
                    e.Handled = true;
                base.OnPreviewTextInput(e);
            }
            else
            {              
                if (Char.IsNumber(c) || c == ',' || c == '.' || c == '-')
                    e.Handled = false;
                else
                    e.Handled = true;
                base.OnPreviewTextInput(e);
            }                    
        }

        private DateTime Create_timestamp()
        {
            myTime = DateTime.Now;
            return myTime;
        }
    }
}
