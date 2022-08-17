using System;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using lib60870;
using lib60870.CS101;
using lib60870.CS104;
using System.Windows.Input;
using System.Windows.Controls;
using System.Reflection;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;

namespace IEC_104_Tools
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{	
		public class MyBaseData
		{
			private string id = "";
			public string ID { get => id; set => id = value.PadRight(6); }

			private string time = "";
			public string Time { get => time; set => time = value.PadRight(6); }

			private string pci = "";
			public string PCI { get => pci; set => pci = value.PadRight(26); }

			private string typeid = "";
			public string TypeID { get => typeid; set => typeid = value.PadRight(15); }

			private string cot = "";
			public string COT { get => cot; set => cot = value.PadRight(28); }

			private string oa = "";
			public string OA { get => oa; set => oa = value.PadRight(3); }

			private string ca = "";
			public string CA { get => ca; set => ca = value.PadRight(3); }

			private string ioa_d = "";
			public string IOA_d { get => ioa_d; set => ioa_d = value.PadRight(6); }

			private string ioa_h = "";
			public string IOA_h { get => ioa_h; set => ioa_h = value.PadRight(6); }

			private string values = "";
			public string Values { get => values; set => values = value.PadRight(13); }

			private string timestamp = "";
			public string Timestamp { get => timestamp; set => timestamp = value.PadRight(22); }

			private string quality = "";
			public string Quality { get => quality; set => quality = value.PadRight(10); }
			public string Len { get; set; }
			public string Message { get; set; }
			public string Cnt { get; set; }
			public string Description { get; set; }
			public override string ToString()
			{
				return string.Format("№ = {0} Time = {1} Apci/Lpci = {2} typeid = {3} cot = {4} OA = {5} CA = {6} IOA = {7} IOA hex = {8} Value = {9} Timestamp = {10} Quality = {11} Descr = {12}", ID, Time, pci, typeid, cot, OA, CA, IOA_d, IOA_h, Values, Timestamp, Quality, Description);
			}
		}
		public MyBaseData myCommand;
		public ObservableCollection<MyBaseData> main_source = new ObservableCollection<MyBaseData>();
		public ObservableCollection<MyBaseData> object_source = new ObservableCollection<MyBaseData>();
		public ObservableCollection<MyBaseData> raw_source = new ObservableCollection<MyBaseData>();	
		public APCIParameters aPCIParameters = new APCIParameters();
		public ApplicationLayerParameters applicationLayerParameters = new ApplicationLayerParameters();
		public Connection con;
		public double t_con = 50;
		public ASDU old_ASDU;
		DispatcherTimer timer_con = new DispatcherTimer();
		bool Disconnect_manual = false;
		public bool true_connected = false;
		int num_ID = 0;
		int num_ID_ob = 0;
		int num_ID_raw = 0;
		int num_ID_logs = 0;	
		bool Filt_On = false;
		string[] description = null;

		/// <summary>
		/// Главное окно:
		/// </summary>
		public MainWindow()
		{		
			InitializeComponent();
			grid_main.ItemsSource = main_source;
			grid_object.ItemsSource = object_source;
			grid_raw.ItemsSource = raw_source;			
		
			timer_con.Tick += new EventHandler(timer_Tick);

			DispatcherTimer timer_logs = new DispatcherTimer();
			timer_logs.Tick += new EventHandler(timer_Logs_Tick);
			timer_logs.Interval = new TimeSpan(0, 0, 0, 1, 0);
			timer_logs.Start();

			DispatcherTimer timer_scroll = new DispatcherTimer();
			timer_scroll.Tick += new EventHandler(timer_Scroll_Tick);
			timer_scroll.Interval = new TimeSpan(0, 0, 0, 0, 50);
			timer_scroll.Start();

			bilding_MainWindow();
		}
  
		void bilding_MainWindow()
        {
			CheckBox checkBox2 = new CheckBox();
			checkBox2.Name = "checkBox_ALL";
			checkBox2.Content = "Все типы";
			checkBox2.IsChecked = true;
			checkBox2.Checked += CheckBox_FILT_Checed_ALL;
			checkBox2.Unchecked += CheckBox_FILT_UnCheced_ALL;
			filt_type.Items.Add(checkBox2);
			foreach (TypeID type in FILTRS)
			{
				int code = (int)Enum.Parse(typeof(TypeID), type.ToString());
				CheckBox checkBox = new CheckBox();
				checkBox.Name = type.ToString();
				checkBox.Content = type.ToString().Replace("_", "__") + " " + "(" + code.ToString() + ")";
				checkBox.Checked += CheckBox_FILT_Chec;
				checkBox.Unchecked += CheckBox_FILT_Chec;
				checkBox.IsChecked = true;
				filt_type.Items.Add(checkBox);
			}
			filt_type.SelectedIndex = 0;
		}

		bool verification()
		{
			DateTime localDate = DateTime.Now;
			bool result_verification = false;
			Microsoft.Win32.RegistryKey currentUserKey = Microsoft.Win32.Registry.CurrentUser;
			Microsoft.Win32.RegistryKey IEC104TOLS = currentUserKey.CreateSubKey("IEC104TOLS");
			if (IEC104TOLS.GetValue("password") != null)
			{
				if (IEC104TOLS.GetValue("password").ToString() == localDate.Year.ToString())
					result_verification = true;
			}
			while (!result_verification)
			{
				PasswordWindow passwordWindow = new PasswordWindow();
				if (passwordWindow.ShowDialog() == true)
				{
					if (passwordWindow.Password == ((localDate.Year + localDate.Month - 2000) * 3).ToString())
					{
						IEC104TOLS.SetValue("password", localDate.Year.ToString());
						result_verification = true;
						MessageBox.Show("Авторизация пройдена");
					}
					else
						MessageBox.Show("Неверный пароль или сбита дата на вашем ПК");
				}
				else
				{
					MessageBox.Show("Авторизация не пройдена или сбита дата на вашем ПК");
				}

			}
			return result_verification;
		}
		
		private void timer_Tick(object sender, EventArgs e)
		{
			if (con != null)
			{
				if (!con.IsRunning)
				{
					if (automaticReconnect.IsChecked & !Disconnect_manual)
					{
						Disconnect_manual = true;
						test_Connection(textBox_Host.Text, Convert.ToInt32(textBox_Port.Text));
					}
				}
			}
				
		}

		private void timer_Scroll_Tick(object sender, EventArgs e)
		{
			scroll_grid();
		}

		private void button_Connect_Click(object sender, EventArgs e)
		{
			if (!true_connected)
				connection();
		}

		private void button_DisConnect_Click(object sender, RoutedEventArgs e)
		{
			if (true_connected)
				disconnection();
		}

		private void Button_Default_Click(object sender, RoutedEventArgs e)
		{
			textBox_Host.Text = "127.0.0.1";
			textBox_Port.Text = "2404";
		}

		private void textBox_Host_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			char c = Convert.ToChar(e.Text);
			if (Char.IsNumber(c) || c == '.')
				e.Handled = false;
			else
				e.Handled = true;

			base.OnPreviewTextInput(e);
		}

		private void textBox_Port_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			char c = Convert.ToChar(e.Text);
			if (Char.IsNumber(c))
				e.Handled = false;
			else
				e.Handled = true;

			base.OnPreviewTextInput(e);
		}

		private void exit_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.RegistryKey currentUserKey = Microsoft.Win32.Registry.CurrentUser;
			Microsoft.Win32.RegistryKey helloKey = currentUserKey.CreateSubKey("HelloKey");
			helloKey.SetValue("PORT", textBox_Port.Text);
			helloKey.SetValue("IP", textBox_Host.Text);
			helloKey.Close();
			Environment.Exit(0);
		}

		private void clear_DataGrid_Click(object sender, RoutedEventArgs e)
		{
			clear_DataGrid_Click();
		}

		private void clear_DataGrid_Click()
		{
			DebugListbox.Items.Clear();
			pause_Logs.IsChecked = false;
			num_ID = 0;
			num_ID_ob = 0;
			num_ID_raw = 0;
			num_ID_logs = 0;
			raw_source.Clear();
			object_source.Clear();
			main_source.Clear();
			filtrs_grid();
		}

		private void send_C_IC_NA_1_Click(object sender, RoutedEventArgs e)
		{
            ComboBoxItem selectedItem = (ComboBoxItem)Qualifier_type.SelectedItem;
            string str = selectedItem.Content.ToString();
			byte qoi = 0;
			switch (str)
            {
				case "STATION":
					qoi = QualifierOfInterrogation.STATION;
					break;
				case "GROUP_1":
					qoi = QualifierOfInterrogation.GROUP_1;
					break;
				case "GROUP_3":
					qoi = QualifierOfInterrogation.GROUP_3;
					break;
				case "GROUP_4":
					qoi = QualifierOfInterrogation.GROUP_4;
					break;
				case "GROUP_5":
					qoi = QualifierOfInterrogation.GROUP_5;
					break;
				case "GROUP_6":
					qoi = QualifierOfInterrogation.GROUP_6;
					break;
				case "GROUP_7":
					qoi = QualifierOfInterrogation.GROUP_7;
					break;
				case "GROUP_8":
					qoi = QualifierOfInterrogation.GROUP_8;
					break;
				case "GROUP_9":
					qoi = QualifierOfInterrogation.GROUP_9;
					break;
				case "GROUP_10":
					qoi = QualifierOfInterrogation.GROUP_10;
					break;
				case "GROUP_11":
					qoi = QualifierOfInterrogation.GROUP_11;
					break;
				case "GROUP_12":
					qoi = QualifierOfInterrogation.GROUP_12;
					break;
				case "GROUP_13":
					qoi = QualifierOfInterrogation.GROUP_13;
					break;
				case "GROUP_14":
					qoi = QualifierOfInterrogation.GROUP_14;
					break;
				case "GROUP_15":
					qoi = QualifierOfInterrogation.GROUP_15;
					break;
				case "GROUP_16":
					qoi = QualifierOfInterrogation.GROUP_16;
					break;
			}
			send_100_C_IC_NA_1(txtNum_CA.Text, qoi);
		}

		private void send_C_RD_NA_1_Click(object sender, RoutedEventArgs e)
		{
			send_102_C_RD_NA_1(txtNum_CA.Text, txtNum_IOA.Text);
		}

		private void cmdUp_CA_Click(object sender, RoutedEventArgs e)
		{
			if (txtNum_CA.Text == "")
				txtNum_CA.Text = "0";
			txtNum_CA.Text = (Convert.ToInt32(txtNum_CA.Text) + 1).ToString();
			if (Convert.ToInt32(txtNum_CA.Text) < 0)
				txtNum_CA.Text = "0";
		}

		private void cmdDown_CA_Click(object sender, RoutedEventArgs e)
		{
			if (txtNum_CA.Text == "")
				txtNum_CA.Text = "0";
			if (Convert.ToInt32(txtNum_CA.Text) > 0)
				txtNum_CA.Text = (Convert.ToInt32(txtNum_CA.Text) - 1).ToString();
			if (Convert.ToInt32(txtNum_CA.Text) < 0)
				txtNum_CA.Text = "0";
		}

		private void cmdUp_IOA_Click(object sender, RoutedEventArgs e)
		{
			if (txtNum_IOA.Text == "")
				txtNum_IOA.Text = "0";
			txtNum_IOA.Text = (Convert.ToInt32(txtNum_IOA.Text) + 1).ToString();
			if (Convert.ToInt32(txtNum_IOA.Text) < 0)
				txtNum_IOA.Text = "0";
		}

		private void cmdDown_IOA_Click(object sender, RoutedEventArgs e)
		{
			if (txtNum_IOA.Text == "")
				txtNum_IOA.Text = "0";
			if (Convert.ToInt32(txtNum_IOA.Text) > 0)
				txtNum_IOA.Text = (Convert.ToInt32(txtNum_IOA.Text) - 1).ToString();
			if (Convert.ToInt32(txtNum_IOA.Text) < 0)
				txtNum_IOA.Text = "0";
		}

		private void cmdDown_PORT_Click(object sender, RoutedEventArgs e)
		{
			if (textBox_Port.Text == "")
				textBox_Port.Text = "0";
			if (Convert.ToInt32(textBox_Port.Text) > 0)
				textBox_Port.Text = (Convert.ToInt32(textBox_Port.Text) - 1).ToString();
			if (Convert.ToInt32(textBox_Port.Text) < 0)
				textBox_Port.Text = "0";
		}

		private void cmdUp_PORT_Click(object sender, RoutedEventArgs e)
		{
			if (textBox_Port.Text == "")
				textBox_Port.Text = "0";
			textBox_Port.Text = (Convert.ToInt32(textBox_Port.Text) + 1).ToString();
			if (Convert.ToInt32(textBox_Port.Text) < 0)
				textBox_Port.Text = "0";
		}

		private void quickAccess_Enabled_Click(object sender, RoutedEventArgs e)
		{
			switch (quickAccess.Visibility)
			{
				case Visibility.Visible:
					quickAccess.Visibility = Visibility.Hidden;
					break;
				case Visibility.Hidden:
					quickAccess.Visibility = Visibility.Visible;
					break;
            }
        }
	
		private void grid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			if (e.Row.GetIndex() > 50000)
			{
				clear_DataGrid_Click();
			}			
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
            {
				Microsoft.Win32.RegistryKey currentUserKey = Microsoft.Win32.Registry.CurrentUser;
				Microsoft.Win32.RegistryKey IEC104TOLS = currentUserKey.CreateSubKey("IEC104TOLS");
				IEC104TOLS.SetValue("PORT", textBox_Port.Text);
				IEC104TOLS.SetValue("IP", textBox_Host.Text);
				IEC104TOLS.Close();
				if (true_connected)
					disconnection();

			}
			catch
            {

            }
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			verification();
			this.Title = this.Title + "  " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
			Microsoft.Win32.RegistryKey currentUserKey = Microsoft.Win32.Registry.CurrentUser;
			Microsoft.Win32.RegistryKey IEC104TOLS = currentUserKey.CreateSubKey("IEC104TOLS");
			if (IEC104TOLS.GetValue("IP") == null)
				textBox_Host.Text = "127.0.0.1";
			else
				textBox_Host.Text = IEC104TOLS.GetValue("IP").ToString();
			if (IEC104TOLS.GetValue("PORT") == null)
				textBox_Port.Text = "2404";
			else
				textBox_Port.Text = IEC104TOLS.GetValue("PORT").ToString();
			IEC104TOLS.Close();
		}

		private void tabItem_Clicked(object sender, MouseButtonEventArgs e)
		{
			quickAccessFiltrs.Visibility = Visibility.Hidden;
		}

		private void tabItem1_Clicked(object sender, MouseButtonEventArgs e)
		{
			quickAccessFiltrs.Visibility = Visibility.Visible;
		}



		/// <summary>
		/// Окна :
		/// </summary>
		private void show_ACPI_Click(object sender, RoutedEventArgs e)
		{
			ACPI taskWindow = new ACPI();
			taskWindow.Owner = this;
			taskWindow.Show();
		}

		private void show_con_Parameters_Click(object sender, RoutedEventArgs e)
		{
			ConParameters taskWindow = new ConParameters();
			taskWindow.Owner = this;
			taskWindow.Show();
		}

		private void show_Commands_System_Click(Object sender, RoutedEventArgs e)
		{
			WindowSystem taskWindow = new WindowSystem();
			taskWindow.Owner = this;
			taskWindow.Show();
		}

		private void show_Commands_Process_Click(object sender, RoutedEventArgs e)
		{
			WindowProcess taskWindow = new WindowProcess();
			taskWindow.Owner = this;
			taskWindow.Show();
		}

		private void show_Commands_Parameters_Click(object sender, RoutedEventArgs e)
		{
			WindowParameters taskWindow = new WindowParameters();
			taskWindow.Owner = this;
			taskWindow.Show();
		}

		private void show_PDF_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menuItem = (MenuItem)sender;
			byte[] PDF = null;
			if (menuItem.Name == "IEC101")
			{
				PDF = Properties.Resources.МЭК_101;
			}
			if (menuItem.Name == "IEC104")
			{
				PDF = Properties.Resources.МЭК_104;
			}
			MemoryStream ms = new MemoryStream(PDF);
			FileStream f = new FileStream(@"C:\Windows\Temp\File.pdf", FileMode.OpenOrCreate);
			File.SetAttributes(@"C:\Windows\Temp\File.pdf", FileAttributes.Hidden);
			ms.WriteTo(f);
			f.Close();
			ms.Close();
			Process.Start(@"C:\Windows\Temp\File.pdf");
		}



		/// <summary>
		/// Управление соединением:
		/// </summary>
		public void connection()
		{
			string str_Host = textBox_Host.Text;
			int str_Port = Convert.ToInt32(textBox_Port.Text);
		    con = new Connection(str_Host, str_Port, aPCIParameters, applicationLayerParameters);
			con.Autostart = automaticSTARTDT.IsChecked;
			con.DebugOutput = true;
			con.SetConnectionHandler(Connection_Event, null);
			con.SetASDUSentedHandler(ASDU_Sented_Event, null);
			con.SetASDUReceivedHandler(ASDU_Received_Event, null);
			con.SetSentRawMessageHandler(RAW_Message_Sented_Event, null);
			con.SetReceivedRawMessageHandler(RAW_Message_Receiv_Event, null);
			con.SetDebugLogHandler(Debug_Event);
			try
			{				
				con.Connect();
				true_connected = true;
				Disconnect_manual = false;
				Label_N_S_.Content = "N(S): " + con.statistics.SendSequenceCounter.ToString();
				Label_N_R_.Content = "N(R): " + con.statistics.ReceiveSequenceCounter.ToString();
				timer_con.Interval = TimeSpan.FromMilliseconds(t_con); 
				timer_con.Start();
			}
			catch
			{

			}
		}

		public void disconnection()
		{
			Disconnect_manual = true;
			try
			{
				con.ManualCancel(1);
				con_State.Background = new SolidColorBrush(Colors.Red);
				true_connected = false;
				con = null;
			}
			catch
			{

			}
		}

		private void test_Connection(string str_Host, int str_Port)
		{
			Task task1 = new Task(() =>
			{
				bool key_ready = false;
				int chet_closed = 0;
				while (!key_ready && chet_closed < 30)
				{
					
					TcpClient client = new TcpClient();
					try
					{
						client.Connect(str_Host, str_Port);
						key_ready = true;
					}
					catch
					{
						true_connected = false;
						chet_closed++;
						
					}
					finally
					{						
						client.Dispose();
						client.Close();
					}				
					Thread.Sleep(100);
				}
				try
				{
					con.ReConnect();
					Thread.Sleep(20);
					if (con.IsRunning)
						true_connected = true;
					Disconnect_manual = false;
				}
				catch
				{
					true_connected = false;
				}
			});
			task1.Start();
		}

		private void automaticSTARTDT_Click(object sender, RoutedEventArgs e)
		{
			if (con != null)
			{
				if (con.IsRunning)
				{
					disconnection();
					Thread.Sleep(20);
					connection();
				}
			}
		}



		/// <summary>
		/// События протокола:
		/// </summary>
		/// 
		private void Connection_Event(object parameter, ConnectionEvent connectionEvent)
		{
			Monitor_Statistic();
			switch (connectionEvent)
			{
				case ConnectionEvent.OPENED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.PaleGreen);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = parameter.ToString(),
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					break;
				case ConnectionEvent.CONNECT_FAILED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.Salmon);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение не установлено",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					break;
				case ConnectionEvent.I_CLOSED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.LightGray);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение закрыто клиентом",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					true_connected = false;
					break;
				case ConnectionEvent.CLOSED_T1:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.LightGray);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение закрыто. Таймаут Т1",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					true_connected = false;
					break;
				case ConnectionEvent.CLOSED_T3:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.LightGray);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение закрыто по Т3",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					true_connected = false;
					break;
				case ConnectionEvent.SERV_CLOSED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.LightGray);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение закрыто сервером",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					true_connected = false;
					break;
				case ConnectionEvent.SERV_RESET:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						con_State.Background = new SolidColorBrush(Colors.Salmon);
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "Соединение разорвано",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					true_connected = false;
					break;
				case ConnectionEvent.STARTDT_ACT_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked & outMassage.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> STARTDT ACT",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.STARTDT_CON_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked & outMassage.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> STARTDT CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					break;
				case ConnectionEvent.STARTDT_CON_RECEIVED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "<<< STARTDT CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					break;
				case ConnectionEvent.STOPDT_ACT_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> STOPDT ACT",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.STOPDT_CON_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> STOPDT CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.STOPDT_CON_RECEIVED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked)
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "<<< STOPDT CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
					}));
					break;
				case ConnectionEvent.TESTFR_ACT_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> TESTFR ACT",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.TESTFR_ACT_RECEIVED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "<<< TESTFR ACT",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
						if (automaticTESTFR_CON.IsChecked)
							con.SendTestFR_CON();
					}));
					break;
				case ConnectionEvent.TESTFR_CON_SENDED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = ">>> TESTFR CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.TESTFR_CON_RECEIVED:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked)
						{
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = "<<< TESTFR CON",
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.SEND_S:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							string str_PCI = string.Format(">>> S ({0})", con.statistics.ReceiveSequenceCounter.ToString());
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = str_PCI + parameter.ToString(),
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
				case ConnectionEvent.RECEIV_S:
					this.Dispatcher.BeginInvoke((Action)(() =>
					{
						if (!pause_Logs.IsChecked && outMassage.IsChecked)
						{
							string str_PCI = string.Format("<<< S ({0})", parameter.ToString());
							main_source.Add(new MyBaseData
							{
								ID = num_ID++.ToString(),
								Time = DateTime.Now.ToLongTimeString(),
								PCI = str_PCI,
								TypeID = "",
								COT = "",
								OA = "",
								CA = "",
								IOA_d = "",
								IOA_h = "",
								Values = "",
								Timestamp = "",
								Quality = "",
								Description = "",
							});
						}
					}));
					break;
			}
		}
	
		private bool ASDU_Sented_Event(object sendSequenceNumber, ASDU asdu)
		{
			Monitor_Statistic();
			Thread.Sleep(5);
			this.Dispatcher.Invoke(() =>
			{			
				if (!pause_Logs.IsChecked && myCommand.TypeID.StartsWith(asdu.TypeId.ToString()))
				{
					string str_PCI = string.Format(">>> I (R={1},S={0})", ((Int32)sendSequenceNumber-1).ToString(), con.statistics.ReceiveSequenceCounter.ToString());
					myCommand.ID = num_ID++.ToString();
					myCommand.Time = DateTime.Now.ToLongTimeString();
					myCommand.COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")";
					myCommand.OA = asdu.Oa.ToString();
					myCommand.CA = asdu.Ca.ToString();
					myCommand.PCI = str_PCI;
					main_source.Add(myCommand);				
				}			
			});			
			return true;
		}

		private bool ASDU_Received_Event(object parameter, ASDU asdu)
		{
			Monitor_Statistic();
			old_ASDU = asdu;
			bool repaly = false;

			switch (asdu.TypeId)
			{
				case TypeID.M_SP_NA_1: // {1}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (SinglePointInformation)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}
									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});
                        }
					}
					break;
				case TypeID.M_DP_NA_1: // {3}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (DoublePointInformation)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_BO_NA_1: // {7}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (Bitstring32)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_ME_NA_1: // {9}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (MeasuredValueNormalized)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
                                if (!pause_Logs.IsChecked && i == 0)
                                {
                                    main_source.Add(new MyBaseData
                                    {
                                        ID = num_ID++.ToString(),
                                        Time = DateTime.Now.ToLongTimeString(),
                                        PCI = Bilding_PCI(asdu),
                                        TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
                                        COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
                                        OA = asdu.Oa.ToString(),
                                        CA = asdu.Ca.ToString(),
                                        IOA_d = val.ObjectAddress.ToString(),
                                        IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
                                        Values = val.NormalizedValue.ToString(),
                                        Timestamp = "",
                                        Quality = val.Quality.ToString(),
                                        Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
                                    });
                                }
                                else if (!pause_Logs.IsChecked)
                                {
                                    main_source.Add(new MyBaseData
                                    {
                                        ID = "",
                                        Time = DateTime.Now.ToLongTimeString(),
                                        PCI = " ",
                                        TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
                                        COT = "",
                                        OA = "",
                                        CA = "",
                                        IOA_d = val.ObjectAddress.ToString(),
                                        IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
                                        Values = val.NormalizedValue.ToString(),
                                        Timestamp = "",
                                        Quality = val.Quality.ToString(),
                                        Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
                                    });
                                } 
                            });

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.NormalizedValue.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.NormalizedValue.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_ME_NB_1: // {11}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (MeasuredValueScaled)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.ScaledValue.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.ScaledValue.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.ScaledValue.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									val.ObjectAddress.ToString();
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.ScaledValue.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_ME_NC_1: // {13}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (MeasuredValueShort)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_IT_NA_1: // {15}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (IntegratedTotals)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.BCR.Value.ToString(),
										Timestamp = "",
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.BCR.Value.ToString(),
										Timestamp = "",
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.BCR.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = "",
												Quality = "",
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.BCR.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = "",
										Quality = "",
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_SP_TB_1: // {30}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (SinglePointWithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = val.Timestamp.ToString(),
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_DP_TB_1: // {31}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (DoublePointWithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = val.Timestamp.ToString(),
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_BO_TB_1: // {33}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (Bitstring32WithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = val.Timestamp.ToString(),
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_ME_TF_1: // {36}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (MeasuredValueShortWithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = val.Timestamp.ToString(),
												Quality = val.Quality.ToString(),
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = val.Quality.ToString(),
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;
				case TypeID.M_IT_TB_1: // {37}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (IntegratedTotalsWithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.BCR.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.BCR.Value.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});

							this.Dispatcher.Invoke(() =>
							{
								if (object_source.Count > 0)
								{
									for (int g = 0; g < object_source.Count; g++)
									{
										MyBaseData item = object_source[g];
										if (item.IOA_d.PadRight(6) == val.ObjectAddress.ToString().PadRight(6) && item.CA.PadRight(3) == asdu.Ca.ToString().PadRight(3))
										{
											repaly = true;
											object_source[g] = new MyBaseData
											{
												ID = object_source[g].ID,
												CA = asdu.Ca.ToString(),
												IOA_d = val.ObjectAddress.ToString(),
												IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
												COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
												TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
												Values = val.BCR.Value.ToString(),
												Time = DateTime.Now.ToLongTimeString(),
												Timestamp = val.Timestamp.ToString(),
												Quality = "",
												Cnt = (Convert.ToInt16(object_source[g].Cnt) + 1).ToString(),
												Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
											};
										}

									}
								}
								if (!repaly)
								{
									object_source.Add(new MyBaseData
									{
										ID = num_ID_ob++.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										Values = val.BCR.Value.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = "",
										Cnt = "1",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
							});
						}
					}
					break;			
				case TypeID.M_EI_NA_1: // {70}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (EndOfInitialization)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = "",
										Timestamp = "",
										Quality = "",
										Description = ""
									});
								}
							});
						}
					}
					break;

				case TypeID.C_SC_TA_1: // {58}
					{
						for (int i = 0; i < asdu.NumberOfElements; i++)
						{
							var val = (SingleCommandWithCP56Time2a)asdu.GetElement(i);

							this.Dispatcher.Invoke(() =>
							{
								if (!pause_Logs.IsChecked && i == 0)
								{
									main_source.Add(new MyBaseData
									{
										ID = num_ID++.ToString(),
										Time = DateTime.Now.ToLongTimeString(),
										PCI = Bilding_PCI(asdu),
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
										OA = asdu.Oa.ToString(),
										CA = asdu.Ca.ToString(),
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.State.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString())
									});
								}
								else if (!pause_Logs.IsChecked)
								{
									main_source.Add(new MyBaseData
									{
										ID = "",
										Time = DateTime.Now.ToLongTimeString(),
										PCI = " ",
										TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
										COT = "",
										OA = "",
										CA = "",
										IOA_d = val.ObjectAddress.ToString(),
										IOA_h = Convert.ToString(val.ObjectAddress, 16).ToUpper(),
										Values = val.State.ToString(),
										Timestamp = val.Timestamp.ToString(),
										Quality = "",
										Description = Bilding_Description(val.ObjectAddress.ToString(), asdu.Ca.ToString()),
									});
								}
							});
						}
					}
					break;
				default:
					{
						this.Dispatcher.Invoke(() =>
						{
							if (!pause_Logs.IsChecked)
							{
								main_source.Add(new MyBaseData
								{
									ID = num_ID++.ToString(),
									Time = DateTime.Now.ToLongTimeString(),
									PCI = Bilding_PCI(asdu),
									TypeID = asdu.TypeId.ToString() + " (" + Convert.ToInt32(asdu.TypeId) + ")",
									COT = asdu.Cot.ToString() + " (" + Convert.ToInt32(asdu.Cot) + ")",
									OA = asdu.Oa.ToString(),
									CA = asdu.Ca.ToString(),
									IOA_d = "",
									IOA_h = "",
									Values = "",
									Timestamp = "",
									Quality = "",
									Description = ""
								});
							}
						});
					}
					break;
			}

			return true;
		}
		
		private bool RAW_Message_Sented_Event(object parameter, byte[] message, int messageSize)
		{
			this.Dispatcher.BeginInvoke((Action)(() =>
			{			
				if (!pause_Logs.IsChecked)
				{
					RAW_Massege(message, messageSize, "TX: ");
				}
			}));
			return true;
		}

		private bool RAW_Message_Receiv_Event(object parameter, byte[] message, int messageSize)
		{
			this.Dispatcher.BeginInvoke((Action)(() =>
			{
				if (!pause_Logs.IsChecked)
				{
					RAW_Massege(message, messageSize, "RX: ");
				}
			}));

			return true;
		}

		private void RAW_Massege(byte[] message, int messageSize, string convertedMessage)
		{
			for (int y = 0; y < messageSize; y++)
			{
				convertedMessage = convertedMessage.Insert(convertedMessage.Length, Convert.ToString(message[y], 16));
				convertedMessage = convertedMessage + " ";
			}
			convertedMessage = convertedMessage.ToUpper();

			raw_source.Add(new MyBaseData
			{
				ID = num_ID_raw++.ToString(),
				Time = DateTime.Now.ToLongTimeString(),
				PCI = "",
				TypeID = "",
				COT = "",
				OA = "",
				CA = "",
				IOA_d = "",
				IOA_h = "",
				Values = "",
				Timestamp = "",
				Quality = "",
				Description = "",
				Len = messageSize.ToString(),
				Message = convertedMessage
			});
		}

		private void Debug_Event(string message)
        {
			this.Dispatcher.BeginInvoke((Action)(() => DebugListbox.Items.Add(message)));
		}

		private string Bilding_PCI(ASDU asdu)
		{
			uint a;
			uint b;
			a = Convert.ToUInt32(asdu.RAW_1[2]) >> 1;
			b = Convert.ToUInt32(asdu.RAW_1[3]) << 8;
			uint s = a + b;
			a = Convert.ToUInt32(asdu.RAW_1[4]) >> 1;
			b = Convert.ToUInt32(asdu.RAW_1[5]) << 8;
			uint r = a + b;
			//string str_PCI = string.Format("<<< I (R={0},S={1})", s.ToString(), r.ToString());
			string str_PCI = string.Format("<<< I (R={0},S={1})", r.ToString(), s.ToString());
			return str_PCI;
		}

		private string Bilding_Description(string IOA, string CA)
        {
			string descr = "";
			if (description != null)
				foreach (string str in description)
					if (str.Split(' ').Length == 3)
						if (IOA == str.Split(' ')[1] && CA == str.Split(' ')[0])
							descr = str.Split(' ')[2];
			return descr;
		}

		private void Bilding_Command(TypeID typeID, string ioa, string value, string timestamp)
		{
			myCommand = new MyBaseData
			{
				ID = "",
				Time = "",
				PCI = "",
				TypeID = typeID.ToString() + " (" + Convert.ToInt32(typeID) + ")",
				COT = "",
				OA = "",
				CA = "",
				IOA_d = ioa ?? "",
				IOA_h = "",
				Values = value ?? "",
				Timestamp = timestamp ?? "",
				Quality = "",
				Description = ""
			};
			if (myCommand.IOA_d != "")
			{
				int IOA_h = Convert.ToInt32(ioa);
				myCommand.IOA_h = Convert.ToString(IOA_h, 16).ToUpper();
			}
		}

		private void Monitor_Statistic()
        {
			this.Dispatcher.BeginInvoke((Action)(() =>
			{
				if (con != null)
				{
					Label_UNN_R_.Content = "UNC(R): " + con.statistics.UnconfirmedReceiveSequenceCounter;
					Label_UNN_S_.Content = "UNC(S): " + con.statistics.UnconfirmedSendSequenceCounter;
					Label_N_R_.Content = "N(R): " + con.statistics.ReceiveSequenceCounter;
					Label_N_S_.Content = "N(S): " + con.statistics.SendSequenceCounter;
					if (con.statistics.UnconfirmedSendSequenceCounter == aPCIParameters.K && con.connecting)
						Label_UNN_S_.Background = new SolidColorBrush(Colors.Red);
					else
						Label_UNN_S_.Background = null;
				}
				else
					Label_UNN_S_.Background = null;
			}));
		}



		/// <summary>
		/// Журналы:
		/// </summary>
		/// 

		private void Load_FileDescr(object sender, RoutedEventArgs e)
		{
			string path_description = null;
			DefaultDialogService defaultDialogService = new DefaultDialogService();
			if (defaultDialogService.OpenFileDialog())
			{
				path_description = defaultDialogService.FilePath;
			}
			if (path_description != null)
			{
				description = File.ReadAllLines(path_description);
				foreach (var item in main_source)
				{
					foreach (string str in description)
						if (str.Split(' ').Length == 3)
							if (item.IOA_d == str.Split(' ')[1] && item.CA == str.Split(' ')[0])
								item.Description = str.Split(' ')[2];
				}
			}
			grid_object.ItemsSource = null;
			grid_main.ItemsSource = null;
			filtrs_grid();
		}

		private void scroll_grid(DataGrid dataGrid)
		{
			this.Dispatcher.Invoke((Action)(() =>
			{
				if (!pause_Logs.IsChecked && (bool)automaticScrolling.IsChecked)
				{
					try
					{
						var border = VisualTreeHelper.GetChild(dataGrid, 0) as Decorator;
						if (border != null)
						{

							var scroll = border.Child as ScrollViewer;
							if (scroll != null) scroll.ScrollToEnd();
						}
					}
					catch
					{

					}

				}
			}));
		}

		private void scroll_grid()
		{
			scroll_grid(grid_main);
			scroll_grid(grid_raw);
		}

		async void timer_Logs_Tick(object sender, EventArgs e)
		{
			if (con != null)
            {
				string buf;
				this.Dispatcher.Invoke(() => 
				{
					buf = con.statistics.TimerT1Counter.ToString();
					if (con.statistics.TimerT1Counter > 1000)
					{
						buf = buf.Remove(buf.Length - 3);
						TimerT1_Counter.Content = "T1: " + buf;
					}					
					else
						TimerT1_Counter.Content = "T1: 0";

					buf = con.statistics.TimerT2Counter.ToString();
					if (con.statistics.TimerT2Counter > 1000)
					{
						buf = buf.Remove(buf.Length - 3);
						TimerT2_Counter.Content = "T2: " + buf;
					}
					else
						TimerT2_Counter.Content = "T2: 0";
					buf = con.statistics.TimerT3Counter.ToString();
					if (con.statistics.TimerT3Counter > 1000)
					{
						buf = buf.Remove(buf.Length - 3);
						TimerT3_Counter.Content = "T3: " + buf;
					}
					else
						TimerT3_Counter.Content = "T3: 0";
				});
			}
			
			if (auto_logs.IsChecked)
            {
				var task0 = Task.Run(task_logs_to_file);
				await Task.WhenAll(task0);
			}						
		}

		private void task_logs_to_file()
		{
			for (int i = num_ID_logs; i < main_source.Count; i++)
				File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + "Log_IEC_104_Tools.txt", main_source[i].ToString() + "\n");
			num_ID_logs = main_source.Count;
		}

		private void Save_logs_to_file(object sender, RoutedEventArgs e)
		{
			string path_description = null;
			DefaultDialogService defaultDialogService = new DefaultDialogService();
			if (defaultDialogService.SaveFileDialog())
			{
				path_description = defaultDialogService.FilePath;
			}
			if (path_description != null)
            {
				FileInfo fileInf = new FileInfo(path_description);
				if (fileInf.Exists)
				{
					fileInf.Delete();
				}
				for (int i = num_ID_logs; i < main_source.Count; i++)
					File.AppendAllText(path_description, main_source[i].ToString() + "\n");
			}				
		}

		private void Debug_DataGrid_Click(object sender, RoutedEventArgs e)
		{
			if (DataGridDebug.Visibility == Visibility.Visible)
				DataGridDebug.Visibility = Visibility.Hidden;
			else
				DataGridDebug.Visibility = Visibility.Visible;
		}



		/// <summary>
		/// Фильтры :
		/// </summary>
		/// 

		TypeID[] FILTRS =
		{
			TypeID.C_BO_NA_1,
			TypeID.C_BO_TA_1,
			TypeID.C_CD_NA_1,
			TypeID.C_CI_NA_1,
			TypeID.C_CS_NA_1,
			TypeID.C_DC_NA_1,
			TypeID.C_DC_TA_1,
			TypeID.C_IC_NA_1,
			TypeID.C_RC_NA_1,
			TypeID.C_RC_TA_1,
			TypeID.C_RD_NA_1,
			TypeID.C_RP_NA_1,
			TypeID.C_SC_NA_1,
			TypeID.C_SC_TA_1,
			TypeID.C_SE_NA_1,
			TypeID.C_SE_NB_1,
			TypeID.C_SE_NC_1,
			TypeID.C_SE_TA_1,
			TypeID.C_SE_TB_1,
			TypeID.C_SE_TC_1,
			TypeID.C_TS_NA_1,
			TypeID.C_TS_TA_1,
			TypeID.M_BO_NA_1,
			TypeID.M_BO_TA_1,
			TypeID.M_BO_TB_1,
			TypeID.M_DP_NA_1,
			TypeID.M_DP_TA_1,
			TypeID.M_DP_TB_1,
			TypeID.M_EI_NA_1,
			TypeID.M_EP_TA_1,
			TypeID.M_EP_TB_1,
			TypeID.M_EP_TC_1,
			TypeID.M_EP_TD_1,
			TypeID.M_EP_TE_1,
			TypeID.M_EP_TF_1,
			TypeID.M_IT_NA_1,
			TypeID.M_IT_TA_1,
			TypeID.M_IT_TB_1,
			TypeID.M_ME_NA_1,
			TypeID.M_ME_NB_1,
			TypeID.M_ME_NC_1,
			TypeID.M_ME_ND_1,
			TypeID.M_ME_TA_1,
			TypeID.M_ME_TB_1,
			TypeID.M_ME_TC_1,
			TypeID.M_ME_TD_1,
			TypeID.M_ME_TE_1,
			TypeID.M_ME_TF_1,
			TypeID.M_PS_NA_1,
			TypeID.M_SP_NA_1,
			TypeID.M_SP_TA_1,
			TypeID.M_SP_TB_1,
			TypeID.M_ST_NA_1,
			TypeID.M_ST_TA_1,
			TypeID.M_ST_TB_1
		};

		private void filtrs_grid() 
        {
			if (Filt_On)
			{
				string strTUPE = filt_type.Text;
				string strIOA = "";
				if (filt_IOA.Text.Contains("h"))
                {
					try
					{
						strIOA = Convert.ToInt32(filt_IOA.Text.Replace("h", ""), 16).ToString();
					}
					catch
					{
					}
				}
				else
					strIOA = filt_IOA.Text;

				var _itemSourceList1 = new CollectionViewSource() { Source = main_source };
				ICollectionView Itemlist1 = _itemSourceList1.View;
				var yourCostumFilter1 = new Predicate<object>(item1 => 
				{
					item1 = item1 as MyBaseData;
					bool repeat = false;
					foreach (object obj in filt_type.Items)
                    {
						CheckBox checkBox = (CheckBox)obj;
						string str = checkBox.Name;
						bool select = (bool)checkBox.IsChecked;
						if (((MyBaseData)item1).TypeID.Contains(str) && select && ((MyBaseData)item1).IOA_d.StartsWith(strIOA) && ((MyBaseData)item1).Description.Contains(filt_Descr.Text))
							repeat = true;
					}
					return item1 == null || repeat;
				});
				Itemlist1.Filter = yourCostumFilter1;
				grid_main.ItemsSource = Itemlist1;
			}
			else
            {
				grid_main.ItemsSource = main_source;
			}								
        }

        private void Filt_On_Checked(object sender, RoutedEventArgs e)
		{
			Filt_On = true;
			filtrs_grid();
		}

		private void Filt_Off_UnChecked(object sender, RoutedEventArgs e)
		{
			Filt_On = false;
			filtrs_grid();
		}

		private void filtr_KeyPress(object sender, KeyEventArgs e)
		{
			TextBox buf = (TextBox)sender;
			filtrs_grid();
		}

		private void CheckBox_FILT_Chec(object sender, RoutedEventArgs e)
		{
			filtrs_grid();
		}

		private void CheckBox_FILT_Checed_ALL(object sender, RoutedEventArgs e)
		{
			foreach (object obj in filt_type.Items)
			{
				CheckBox checkBox = (CheckBox)obj;
				checkBox.IsChecked = true;
			}
			filtrs_grid();
		}

		private void CheckBox_FILT_UnCheced_ALL(object sender, RoutedEventArgs e)
		{
			foreach (object obj in filt_type.Items)
			{
				CheckBox checkBox = (CheckBox)obj;
				checkBox.IsChecked = false;
			}
			filtrs_grid();
		}


		/// <summary>
		/// Команды :
		/// </summary>
		public void apply_parameters_con_Click()
		{
			ConParameters frm = (ConParameters)this.Owner;
		}

		public void sendSTARTDT_ACT_Click(object sender, EventArgs e)
		{
			if (true_connected)
			{
				con.SendStartDT_ACT();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void sendSTOPDT_ACT_Click(object sender, RoutedEventArgs e)
		{
			if (true_connected)
			{
				con.SendStopDT_ACT();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void sendTESTFR_ACT_Click(object sender, RoutedEventArgs e)
		{
			if (true_connected)
			{
				con.SendTestFR_ACT();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void sendSTARTDT_CON_Click(object sender, EventArgs e)
		{
			if (true_connected)
			{
				con.SendStartDT_CON();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void sendSTOPDT_CON_Click(object sender, RoutedEventArgs e)
		{
			if (true_connected)
			{
				con.SendStopDT_CON();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public void sendTESTFR_CON_Click(object sender, RoutedEventArgs e)
		{
			if (true_connected)
			{
				con.SendTestFR_CON();
			}
			else
				MessageBox.Show("Нет связи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
		}			

		public void send_100_C_IC_NA_1(string ca, byte qoi)
		{
			if (true_connected)
			{
				Bilding_Command(TypeID.C_IC_NA_1, null, null, null);
				con.SendInterrogationCommand(CauseOfTransmission.ACTIVATION, Convert.ToInt32(ca), qoi);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_101_C_CI_NA_1(string ca, byte qcc)
		{
			if (true_connected)
			{
				Bilding_Command(TypeID.C_CI_NA_1, null, null, null);
				con.SendCounterInterrogationCommand(CauseOfTransmission.ACTIVATION, Convert.ToInt32(ca), qcc);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_102_C_RD_NA_1(string ca, string ioa)
		{
			if (true_connected)
			{				
				Bilding_Command(TypeID.C_RD_NA_1, ioa, null, null);
				con.SendReadCommand(Convert.ToInt32(ca), Convert.ToInt32(ioa));							
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_103_C_CS_NA_1(string ca, CP56Time2a cP56Time)
		{
			if (true_connected)
			{
				Bilding_Command(TypeID.C_CS_NA_1, null, null, cP56Time.ToString());
				con.SendClockSyncCommand(Convert.ToInt32(ca), cP56Time);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_105_C_RP_NA_1(string ca, byte qrp)
		{
			if (true_connected)
			{
				Bilding_Command(TypeID.C_RP_NA_1, null, null, null);
				con.SendResetProcessCommand(CauseOfTransmission.ACTIVATION, Convert.ToInt32(ca), qrp);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_107_C_TS_TA_1(string ca, ushort tsc, CP56Time2a cP56Time)
		{
			if (true_connected)
			{
				Bilding_Command(TypeID.C_CS_NA_1, null, null, cP56Time.ToString());
				con.SendTestCommandWithCP56Time2a(Convert.ToInt32(ca), tsc, cP56Time);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_Process_Commands(CauseOfTransmission cot, int ca, InformationObject sc, string val, string time2A, string ioa, TypeID typeID)
		{
			if (true_connected)
			{
				Bilding_Command(typeID, ioa, val, time2A);
				con.SendControlCommand(cot, ca, sc);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void send_Parameters(CauseOfTransmission cot, int ca, InformationObject sc, string val, string ioa, string time2A, TypeID typeID)
		{
			if (true_connected)
			{
				Bilding_Command(typeID, ioa, val, time2A);
				con.SendParameters(cot, ca, sc);
			}
			else
			{
				MessageBox.Show("Соединение отсутствует", "Отмена операции", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
    }
    public class ValueToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string input = value as string;

				if (input.Contains("False"))
					return Brushes.Red;
				else if (input.Contains("True"))
					return Brushes.Lime;
				else
					return DependencyProperty.UnsetValue;
			
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public class PCIToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string input = value as string;

			if (input.Contains(">>>"))
				return Brushes.Yellow;
			else if (input.Contains("Соединение установлено"))
				return Brushes.PaleGreen;
			else if (input.Contains("Соединение"))
				return Brushes.Salmon;
			else
				return DependencyProperty.UnsetValue;

		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public class QualityToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string input = value as string;
			if (input != null)
				if (input.Contains("Overflow") || input.Contains("Blocked") || input.Contains("Substituted") || input.Contains("NonTopical") || input.Contains("Invalid"))
					return Brushes.Cyan;
				else
					return DependencyProperty.UnsetValue;
			return DependencyProperty.UnsetValue;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
