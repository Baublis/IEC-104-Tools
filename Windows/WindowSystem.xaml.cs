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
using lib60870;
using lib60870.CS101;
using lib60870.CS104;

namespace IEC_104_Tools
{
    /// <summary>
    /// Логика взаимодействия для WindowSystem.xaml
    /// </summary>
    public partial class WindowSystem : Window
    {
        public DateTime myTime;
        CP56Time2a timestamp;
        bool live = false;

        public WindowSystem()
        {
            InitializeComponent();
            date_picker_103.SelectedDate = DateTime.Now;
            date_picker_107.SelectedDate = DateTime.Now;
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
                textbox.Text = "255";
        }

        private void IOA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 65535)
                textbox.Text = "1";
        }

        private void RQT_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 63)
                textbox.Text = "63";
        }

        private void TSC_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 65535)
                textbox.Text = "65535";
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            live = true;
            hours_103.Text = DateTime.Now.TimeOfDay.Hours.ToString();
            minutes_103.Text = DateTime.Now.TimeOfDay.Minutes.ToString();
            seconds_103.Text = DateTime.Now.TimeOfDay.Seconds.ToString();
            hours_107.Text = DateTime.Now.TimeOfDay.Hours.ToString();
            minutes_107.Text = DateTime.Now.TimeOfDay.Minutes.ToString();
            seconds_107.Text = DateTime.Now.TimeOfDay.Seconds.ToString();
        }

        private void hours_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text == "" || Convert.ToDouble(textBox.Text) > 24)
                textBox.Text = "24";
        }

        private void minutes_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text == "" || Convert.ToDouble(textBox.Text) > 60)
                textBox.Text = "60";
        }

        private void seconds_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text == "" || Convert.ToDouble(textBox.Text) > 60)
                textBox.Text = "60";
        }

        private void LocalTime_103_Click(object sender, RoutedEventArgs e)
        {
            date_picker_103.IsEnabled = !date_picker_103.IsEnabled;
            seconds_103.IsEnabled = !seconds_103.IsEnabled;
            hours_103.IsEnabled = !hours_103.IsEnabled;
            minutes_103.IsEnabled = !minutes_103.IsEnabled;
        }

        private void LocalTime_107_Click(object sender, RoutedEventArgs e)
        {
            date_picker_107.IsEnabled = !date_picker_107.IsEnabled;
            seconds_107.IsEnabled = !seconds_107.IsEnabled;
            hours_107.IsEnabled = !hours_107.IsEnabled;
            minutes_107.IsEnabled = !minutes_107.IsEnabled;
        }

        private DateTime Create_timestamp(CheckBox checkbox, DatePicker datepicker, string hours, string minutes, string seconds)
        {
            if (checkbox.IsChecked == true)
                myTime = DateTime.Now;
            else
            {
                myTime = (DateTime)datepicker.SelectedDate;
                myTime = myTime.AddHours(double.Parse(hours));
                myTime = myTime.AddMinutes(double.Parse(minutes));
                myTime = myTime.AddSeconds(double.Parse(seconds));
            }
            return myTime;
        }

        private void QOI_100_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Не используется (0)":
                        {
                            QOI_100_textbox.Text = "0";
                            QOI_100_textbox.IsReadOnly = true;
                        }
                        break;
                    case "Резерв (1-19; 37-255)":
                        {
                            QOI_100_textbox.Text = "1";
                            QOI_100_textbox.IsReadOnly = false;
                        }
                        break;
                    default:
                        {
                            QOI_100_textbox.Text = (Convert.ToInt32(comboBox.SelectedIndex) + 20).ToString();
                            QOI_100_textbox.IsReadOnly = true;
                        }
                        break;
                }
            }
        }

        private void RQT_101_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Резерв (6-63)":
                        {
                            RQT_101_textbox.Text = comboBox.SelectedIndex.ToString();
                            RQT_101_textbox.IsReadOnly = false;
                        }
                        break;
                    default:
                        {
                            RQT_101_textbox.Text = comboBox.SelectedIndex.ToString();
                            RQT_101_textbox.IsReadOnly = true;
                        }
                        break;
                }
            }
        }

        private void QRP_105_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Резерв (3-255)":
                        {
                            QRP_105_textbox.Text = comboBox.SelectedIndex.ToString();
                            QRP_105_textbox.IsReadOnly = false;
                        }
                        break;
                    default:
                        {
                            QRP_105_textbox.Text = comboBox.SelectedIndex.ToString();
                            QRP_105_textbox.IsReadOnly = true;
                        }
                        break;
                }
            }
        }

        private void send_100_C_IC_NA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.send_100_C_IC_NA_1(CA_100.Text, Convert.ToByte(QOI_100_textbox.Text));
        }

        private void send_101_C_CI_NA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            int buf = Convert.ToInt32(RQT_101_textbox.Text) + 64 * FRZ_101.SelectedIndex;
            byte QCC = Convert.ToByte(buf);
            frm.send_101_C_CI_NA_1(CA_101.Text, QCC);
        }

        private void send_102_C_RD_NA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.send_102_C_RD_NA_1(CA_102.Text, IOA_102.Text);
        }

        private void send_103_C_CS_NA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            timestamp = new CP56Time2a(Create_timestamp(localtime_103, date_picker_103, hours_103.Text, minutes_103.Text, seconds_103.Text));
            frm.send_103_C_CS_NA_1(CA_103.Text, timestamp);
        }

        private void send_105_C_RP_NA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.send_105_C_RP_NA_1(CA_105.Text, Convert.ToByte(QRP_105_textbox.Text));
        }

        private void send_107_C_TS_TA_1(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            ushort tsc = Convert.ToUInt16(TSC_107.Text);
            timestamp = new CP56Time2a(Create_timestamp(localtime_107, date_picker_107, hours_107.Text, minutes_107.Text, seconds_107.Text));
            frm.send_107_C_TS_TA_1(CA_107.Text, tsc, timestamp);
        }     
    }
}
