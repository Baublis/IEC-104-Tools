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
using System.Windows.Threading;
using lib60870;
using lib60870.CS101;
using lib60870.CS104;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Timers;

namespace IEC_104_Tools
{
    /// <summary>
    /// Логика взаимодействия для WindowProcess.xaml
    /// </summary>
    public partial class WindowProcess : Window
    {
        MainWindow mainWindow;
        public DateTime myTime;
        bool live = false;     
        CP56Time2a Select_Exute_timestamp;
        TypeID typeID;
        public WindowProcess()
        {
            InitializeComponent();
            date_picker.SelectedDate = DateTime.Now;
            mainWindow = (MainWindow)this.Owner;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Select_Click(new CP56Time2a(Create_timestamp()));
        }
        private void Select_Click(CP56Time2a timestamp)
        {
            Create_Commands(true, CauseOfTransmission.ACTIVATION, timestamp);
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            key_e.IsEnabled = false;
            Execute_Click(new CP56Time2a(Create_timestamp()));
            key_e.IsEnabled = true;
        }
        private void Execute_Click(CP56Time2a timestamp)
        {
            Create_Commands(false, CauseOfTransmission.ACTIVATION, timestamp);
        }


        private void Select_Exute_Click(object sender, RoutedEventArgs e)
        {
            mainWindow = (MainWindow)this.Owner;
            if (mainWindow.true_connected)
                Select_Exute_Click();
            else
                MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void Select_Exute_Click()
        {
            Select_Exute_timestamp = new CP56Time2a(Create_timestamp());
            Task task_check = new Task(() =>
            {               
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    key_cancel.IsEnabled = false;
                    key_e.IsEnabled=false;
                    key_s.IsEnabled=false;
                    key_s_e.IsEnabled=false;
                    Select_Click(Select_Exute_timestamp);
                }));
                bool key = true;
                while (key)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (mainWindow.old_ASDU?.TypeId == typeID && mainWindow.old_ASDU?.Cot == CauseOfTransmission.ACTIVATION_CON)
                        {
                            key = false;                        
                        }                          
                    });
                }
            });
            Task task_check_check = new Task(() =>
            {
                if (task_check.Wait(5000))
                    this.Dispatcher.BeginInvoke((Action)(() => Execute_Click(Select_Exute_timestamp)));
                else
                    MessageBox.Show("Не пришел ответ на SELECT", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    key_cancel.IsEnabled = true;
                    key_e.IsEnabled = true;
                    key_s.IsEnabled = true;
                    key_s_e.IsEnabled = true;
                }));                
            });
            task_check_check.Start();
            task_check.Start();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Create_Commands(false, CauseOfTransmission.DEACTIVATION, new CP56Time2a(Create_timestamp()));
        }

        private void Create_Commands(bool select, CauseOfTransmission cot, CP56Time2a timestamp)
        {
            Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            mainWindow = (MainWindow)this.Owner;
            ComboBoxItem selectedItem = (ComboBoxItem)type.SelectedItem;          
            switch (selectedItem.Content.ToString())
            {
                case "C_SC_NA_1 (45)":
                    {
                        typeID = TypeID.C_SC_NA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        SingleCommand cs = new SingleCommand(Convert.ToInt32(IOA.Text), Convert.ToBoolean(value_Bit.SelectedIndex), select, qu);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_DC_NA_1 (46)":
                    {
                        typeID = TypeID.C_DC_NA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        DoubleCommand cs = new DoubleCommand(Convert.ToInt32(IOA.Text), value_DP.SelectedIndex, select, qu);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_RC_NA_1 (47)":
                    {
                        typeID = TypeID.C_RC_NA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        StepCommandValue stepValue = (StepCommandValue)value_Step.SelectedIndex;
                        StepCommand cs = new StepCommand(Convert.ToInt32(IOA.Text), stepValue, select, qu);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_SE_NA_1 (48)":
                    {
                        typeID = TypeID.C_SE_NA_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        float buf = float.Parse(value_float.Text.Replace('.', separator));
                        SetpointCommandNormalized cs = new SetpointCommandNormalized(Convert.ToInt32(IOA.Text), buf, qos);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.NormalizedValue.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_SE_NB_1 (49)":
                    {
                        typeID = TypeID.C_SE_NB_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        int buf = Convert.ToInt32(value_float.Text);
                        ScaledValue scaledValue = new ScaledValue(Convert.ToInt32(buf));
                        SetpointCommandScaled cs = new SetpointCommandScaled(Convert.ToInt32(IOA.Text), scaledValue, qos);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.ScaledValue.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_SE_NC_1 (50)":
                    {
                        typeID = TypeID.C_SE_NC_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        SetpointCommandShort cs = new SetpointCommandShort(Convert.ToInt32(IOA.Text), float.Parse(value_float.Text.Replace('.', separator)), qos);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.Value.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_BO_NA_1 (51)":
                    {
                        typeID = TypeID.C_BO_NA_1;
                        UInt32 bitstring = bitstring_create();
                        Bitstring32Command cs = new Bitstring32Command(Convert.ToInt32(IOA.Text), bitstring);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.Value.ToString(), null, IOA.Text, typeID);
                    }
                    break;
                case "C_SC_TA_1 (58)":
                    {
                        typeID = TypeID.C_SC_TA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        SingleCommandWithCP56Time2a cs = new SingleCommandWithCP56Time2a(Convert.ToInt32(IOA.Text), Convert.ToBoolean(value_Bit.SelectedIndex), select, qu, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_DC_TA_1 (59)":
                    {
                        typeID = TypeID.C_DC_TA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        DoubleCommandWithCP56Time2a cs = new DoubleCommandWithCP56Time2a(Convert.ToInt32(IOA.Text), value_DP.SelectedIndex, select, qu, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_RC_TA_1 (60)":
                    {
                        typeID = TypeID.C_RC_TA_1;
                        int qu = Convert.ToInt32(qu_ql_TextBox.Text);
                        StepCommandValue stepValue = (StepCommandValue)value_Step.SelectedIndex;
                        StepCommandWithCP56Time2a cs = new StepCommandWithCP56Time2a(Convert.ToInt32(IOA.Text), stepValue, select, qu, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.State.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_SE_TA_1 (61)":
                    {
                        typeID = TypeID.C_SE_TA_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        float buf = float.Parse(value_float.Text.Replace('.', separator));
                        SetpointCommandNormalizedWithCP56Time2a cs = new SetpointCommandNormalizedWithCP56Time2a(Convert.ToInt32(IOA.Text), buf, qos, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.NormalizedValue.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_SE_TB_1 (62)":
                    {
                        typeID = TypeID.C_SE_TB_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        float buf = float.Parse(value_float.Text.Replace('.', separator));
                        ScaledValue scaledValue = new ScaledValue(Convert.ToInt32(buf));
                        SetpointCommandScaledWithCP56Time2a cs = new SetpointCommandScaledWithCP56Time2a(Convert.ToInt32(IOA.Text), scaledValue, qos, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.ScaledValue.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_SE_TC_1 (63)":
                    {
                        typeID = TypeID.C_SE_TC_1;
                        int ql = Convert.ToInt32(qu_ql_TextBox.Text);
                        SetpointCommandQualifier qos = new SetpointCommandQualifier(select, ql);
                        SetpointCommandShortWithCP56Time2a cs = new SetpointCommandShortWithCP56Time2a(Convert.ToInt32(IOA.Text), float.Parse(value_float.Text.Replace('.', separator)), qos, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.Value.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
                case "C_BO_TA_1 (64)":
                    {
                        typeID = TypeID.C_BO_TA_1;
                        UInt32 bitstring = bitstring_create();
                        Bitstring32CommandWithCP56Time2a cs = new Bitstring32CommandWithCP56Time2a(Convert.ToInt32(IOA.Text), bitstring, timestamp);
                        mainWindow.send_Process_Commands(cot, Convert.ToInt32(CA.Text), cs, cs.Value.ToString(), timestamp.ToString(), IOA.Text, typeID);
                    }
                    break;
            }
        }

        private void Commands_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "C_SC_NA_1 (45)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Visible;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_DC_NA_1 (46)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Visible;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_RC_NA_1 (47)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Visible;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_SE_NA_1 (48)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_SE_NB_1 (49)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_SE_NC_1 (50)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_BO_NA_1 (51)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Hidden;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            time.Visibility = Visibility.Hidden;
                            GroupBox_value_str.Visibility = Visibility.Visible;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Hidden;
                            key_s.Visibility = Visibility.Hidden;
                        }
                        break;
                    case "C_SC_TA_1 (58)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Visible;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_DC_TA_1 (59)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Visible;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                    case "C_RC_TA_1 (60)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Visible;
                            value_float.Visibility = Visibility.Hidden;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                        case "C_SE_TA_1 (61)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                        case "C_SE_TB_1 (62)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                        case "C_SE_TC_1 (63)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Visible;
                            qu_ql_GroupBox.Header = "Указатель команды (QL)";
                            qu_ql_ComboBox.Visibility = Visibility.Hidden;
                            qu_ql_Label.Content = "QL (0-127)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            qu_ql_TextBox.IsReadOnly = false;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Hidden;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Visible;
                            key_s.Visibility = Visibility.Visible;
                        }
                        break;
                        case "C_BO_TA_1 (64)":
                        {
                            qu_ql_GroupBox.Visibility = Visibility.Hidden;
                            qu_ql_GroupBox.Header = "Указатель команды (QU)";
                            qu_ql_ComboBox.Visibility = Visibility.Visible;
                            qu_ql_Label.Content = "Резерв (4-31)";
                            value_Bit.Visibility = Visibility.Hidden;
                            value_DP.Visibility = Visibility.Hidden;
                            value_Step.Visibility = Visibility.Hidden;
                            value_float.Visibility = Visibility.Visible;
                            time.Visibility = Visibility.Visible;
                            GroupBox_value_str.Visibility = Visibility.Visible;
                            GroupBox_value.Visibility = Visibility.Visible;
                            key_s_e.Visibility = Visibility.Hidden;
                            key_s.Visibility = Visibility.Hidden;
                        }
                        break;
                }
            }
        }

        private UInt32 bitstring_create()
        {
            bool[] bools =
               {
                Bit_0.IsChecked.Value,
                Bit_1.IsChecked.Value,
                Bit_2.IsChecked.Value,
                Bit_3.IsChecked.Value,
                Bit_4.IsChecked.Value,
                Bit_5.IsChecked.Value,
                Bit_6.IsChecked.Value,
                Bit_7.IsChecked.Value,
                Bit_8.IsChecked.Value,
                Bit_9.IsChecked.Value,
                Bit_10.IsChecked.Value,
                Bit_11.IsChecked.Value,
                Bit_12.IsChecked.Value,
                Bit_13.IsChecked.Value,
                Bit_14.IsChecked.Value,
                Bit_15.IsChecked.Value,
                Bit_16.IsChecked.Value,
                Bit_17.IsChecked.Value,
                Bit_18.IsChecked.Value,
                Bit_19.IsChecked.Value,
                Bit_20.IsChecked.Value,
                Bit_21.IsChecked.Value,
                Bit_22.IsChecked.Value,
                Bit_23.IsChecked.Value,
                Bit_24.IsChecked.Value,
                Bit_25.IsChecked.Value,
                Bit_26.IsChecked.Value,
                Bit_27.IsChecked.Value,
                Bit_28.IsChecked.Value,
                Bit_29.IsChecked.Value,
                Bit_30.IsChecked.Value,
                Bit_31.IsChecked.Value,
            };

            byte[] arr1 = Array.ConvertAll(bools, b => b ? (byte)1 : (byte)0);

            // pack (in this case, using the first bool as the lsb - if you want
            // the first bool as the msb, reverse things ;-p)
            int bytes = bools.Length / 8;
            if ((bools.Length % 8) != 0) bytes++;
            byte[] arr2 = new byte[bytes];
            int bitIndex = 0, byteIndex = 0;
            for (int i = 0; i < bools.Length; i++)
            {
                if (bools[i])
                {
                    arr2[byteIndex] |= (byte)(((byte)1) << bitIndex);
                }
                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            UInt32 buf = BitConverter.ToUInt32(arr2, 0);

            
            return buf;
        }

        private DateTime Create_timestamp()
        {
            if (local_time.IsChecked == true)
                myTime = DateTime.Now;
            else
            {
                myTime = (DateTime)date_picker.SelectedDate;
                myTime = myTime.AddHours(double.Parse(hours.Text));
                myTime = myTime.AddMinutes(double.Parse(minutes.Text));
                myTime = myTime.AddSeconds(double.Parse(seconds.Text));
            }
            return myTime;
        }

        private void QU_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (live)
            {
                ComboBox comboBox = (ComboBox)sender;
                ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
                switch (selectedItem.Content.ToString())
                {
                    case "Без определения (0)":
                        {
                            qu_ql_TextBox.IsReadOnly = true;
                            qu_ql_TextBox.Text = "0";
                        }
                        break;
                    case "Короткий импульс (1)":
                        {
                            qu_ql_TextBox.IsReadOnly = true;
                            qu_ql_TextBox.Text = "1";
                        }
                        break;
                    case "Длинный импульс (2)":
                        {
                            qu_ql_TextBox.IsReadOnly = true;
                            qu_ql_TextBox.Text = "2";
                        }
                        break;
                    case "Постоянный выход (3)":
                        {
                            qu_ql_TextBox.IsReadOnly = true;
                            qu_ql_TextBox.Text = "3";
                        }
                        break;
                    case "Резерв (4-31)":
                        {
                            qu_ql_TextBox.IsReadOnly = false;
                            qu_ql_TextBox.Text = "4";
                        }
                        break;
                }
            }
        }      

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            value_DP.Visibility = Visibility.Hidden;
            value_Step.Visibility = Visibility.Hidden;
            value_float.Visibility = Visibility.Hidden;
            live = true;
            hours.Text = DateTime.Now.TimeOfDay.Hours.ToString();
            minutes.Text = DateTime.Now.TimeOfDay.Minutes.ToString();
            seconds.Text = DateTime.Now.TimeOfDay.Seconds.ToString();
        }

        private void hours_LostFocus(object sender, RoutedEventArgs e)
        {
            if (hours.Text == "" || Convert.ToDouble(hours.Text) > 24)
                hours.Text = "24";
        }

        private void minutes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (minutes.Text == "" || Convert.ToDouble(minutes.Text) > 60)
                minutes.Text = "60";
        }

        private void seconds_LostFocus(object sender, RoutedEventArgs e)
        {
            if(seconds.Text == "" || Convert.ToDouble(seconds.Text) > 60)
                seconds.Text = "60";
        }

        private void value_float_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            ComboBoxItem comboBoxItem = (ComboBoxItem)type.SelectedItem;
            char c = Convert.ToChar(e.Text);
            if (comboBoxItem.Content.ToString() == "C_SE_NB_1 (49)")
            {
                if (Char.IsNumber(c) || c == '-')
                    e.Handled = false;
                else
                    e.Handled = true;
                base.OnPreviewTextInput(e);
            }
            else if (comboBoxItem.Content.ToString() == "C_BO_NA_1 (51)" || comboBoxItem.Content.ToString() == "C_BO_TA_1 (64)")
            {
                if (Char.IsNumber(c) || Char.IsLetter(c))              
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

        private void PreviewNumberInput(object sender, TextCompositionEventArgs e)
        {
            char c = Convert.ToChar(e.Text);
            if (Char.IsNumber(c))
                e.Handled = false;
            else
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        private void CheckBox_LocalTimr_Click(object sender, RoutedEventArgs e)
        {
            date_picker.IsEnabled = !date_picker.IsEnabled;
            seconds.IsEnabled = !seconds.IsEnabled;
            hours.IsEnabled = !hours.IsEnabled;
            minutes.IsEnabled = !minutes.IsEnabled;
        }

        private void CA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 255)
                textbox.Text = "1";
        }

        private void IOA_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "" || Convert.ToInt32(textbox.Text) > 65535)
                textbox.Text = "1";
        }

        private void Value_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (textbox.Text == "")
                textbox.Text = "0";
            if (type.SelectedItem.ToString() == "C_SE_NA_1 (48)" && Convert.ToInt32(textbox.Text) > 1)
                textbox.Text = "1";

        }

        private void Bit_Click(object sender, RoutedEventArgs e)
        {
            value_float.Text = Convert.ToString(bitstring_create());
        }

        private void value_float_KeyUp(object sender, KeyEventArgs e)
        {
            string str = value_float.Text;
            string BinaryCode = "0";
            CheckBox[] checkBoxes =
               {
                Bit_0,
                Bit_1,
                Bit_2,
                Bit_3,
                Bit_4,
                Bit_5,
                Bit_6,
                Bit_7,
                Bit_8,
                Bit_9,
                Bit_10,
                Bit_11,
                Bit_12,
                Bit_13,
                Bit_14,
                Bit_15,
                Bit_16,
                Bit_17,
                Bit_18,
                Bit_19,
                Bit_20,
                Bit_21,
                Bit_22,
                Bit_23,
                Bit_24,
                Bit_25,
                Bit_26,
                Bit_27,
                Bit_28,
                Bit_29,
                Bit_30,
                Bit_31
            };
            ComboBoxItem comboBoxItem = (ComboBoxItem)type.SelectedItem;
            if ((comboBoxItem.Content.ToString() == "C_BO_NA_1 (51)" || comboBoxItem.Content.ToString() == "C_BO_TA_1 (64)") && str != "")
            {
                if (str.Contains("h"))
                {
                    str = str.Replace("h", "");                                  
                    try
                    {
                        BinaryCode = Convert.ToString(Convert.ToInt32(Convert.ToInt32(str, 16)), 2).PadLeft(32, '0');
                    }
                    catch
                    {
                    }
                }
                else
                {
                    try
                    {
                        BinaryCode = Convert.ToString(Convert.ToInt32(str), 2).PadLeft(32, '0');
                    }
                    catch
                    {
                    }
                }                                 
                BinaryCode = Reverse(BinaryCode);
                for (int i = 0; i < BinaryCode.Length; i++)
                    if (BinaryCode[i] == '1')
                        checkBoxes[i].IsChecked = true;
                    else
                        checkBoxes[i].IsChecked = false;
            }

        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
