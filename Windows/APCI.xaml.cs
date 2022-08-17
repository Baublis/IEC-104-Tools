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

namespace IEC_104_Tools
{
    /// <summary>
    /// Логика взаимодействия для APCI.xaml
    /// </summary>
    public partial class APCI : Window
    {
        public APCI()
        {
            InitializeComponent();
        }

        private void sendSTARTDT_ACT_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendSTARTDT_ACT_Click(sender, e);
        }

        private void sendSTOPDT_ACT_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendSTOPDT_ACT_Click(sender, e);
        }

        private void sendTESTFR_ACT_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendTESTFR_ACT_Click(sender, e);
        }

        private void sendSTARTDT_CON_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendSTARTDT_CON_Click(sender, e);
        }

        private void sendSTOPDT_CON_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendSTOPDT_CON_Click(sender, e);
        }

        private void sendTESTFR_CON_Click(object sender, RoutedEventArgs e)
        {
            MainWindow frm = (MainWindow)this.Owner;
            frm.sendTESTFR_CON_Click(sender, e);
        }
    }
}
