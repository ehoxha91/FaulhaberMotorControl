using System;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace H_BridgeCommander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort port;
        System.Timers.Timer refreshTimer;
        public MainWindow()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            foreach (string prt in ports)
            {
                cmbPorts.Items.Add(prt);
            }

            cmbCommands.Items.Add("EN");
            cmbCommands.Items.Add("DI");
            cmbCommands.Items.Add("V");
            cmbCommands.Items.Add("HALLSPEED");
            cmbCommands.Items.Add("ENCSPEED");
            cmbCommands.Items.Add("CONTMOD");
            cmbCommands.Items.Add("STEPMOD");
            cmbCommands.Items.Add("APCMOD");
            cmbCommands.Items.Add("ENCMOD");

            cmbCommands.Items.Add("VOLTMOD");
            cmbCommands.Items.Add("LA");
            cmbCommands.Items.Add("LR");
            cmbCommands.Items.Add("M");
            cmbCommands.Items.Add("HO");
            cmbCommands.Items.Add("NP");
            cmbCommands.Items.Add("NPOFF");
            cmbCommands.Items.Add("DELAY");
            cmbCommands.Items.Add("TIMEOUT");
            cmbCommands.Items.Add("NE1"); // 'r' is returned when error happens.
            cmbCommands.Items.Add("NE0"); // disable             
            cmbCommands.Items.Add("RESET");
            cmbCommands.Items.Add("FCONFIG"); //Factory mode;
            cmbCommands.Items.Add("HO"); //Define home position.
            cmbCommands.Items.Add("POS"); // Read position;
            cmbCommands.Items.Add("TPOS"); // Read target position;
            cmbCommands.Items.Add("TEM"); // Read temp.
            cmbCommands.Items.Add("OST"); // Operation Status; 
                                          // Check page 90, communication manual for the responses

            refreshTimer = new System.Timers.Timer(1000);
            refreshTimer.Elapsed += RefreshTimer_Elapsed;
        }
        bool reading = true;
        private void RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            refreshTimer.Stop();
            //Command to read something from the port HBridge!
            byte[] transmit = { };
            port.Write(transmit, 0, transmit.Length);
            while (reading) { }
            refreshTimer.Start();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            port.WriteLine(cmbCommands.SelectedValue.ToString());
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            port = new SerialPort(cmbPorts.SelectedValue.ToString(), 9600, Parity.None, 8, StopBits.One);
            port.DataReceived += Port_DataReceived;
            port.Open();
            if(port.IsOpen)
            {
                
                this.Dispatcher.Invoke(() => 
                {
                    btnConnect.Background = Brushes.LightGreen;
                    btnConnect.IsEnabled = false;
                });
            }
            refreshTimer.Start();
        }

        string inData = string.Empty;
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs Se)
        {
            reading = true;
            inData = string.Empty;
            byte[] readbytes = new byte[255];
            char[] msgl = new char[200];
            int count = 0;
           
            for (int i = 0; i < port.ReadBufferSize; i++)
            {
                msgl[i] = Convert.ToChar(port.ReadByte());
                if (msgl[i] == '\n')
                {
                    count = i - 1;
                    break;
                }
            }
            for (int i = 0; i < count; i++)
            {
                inData += inData + msgl[i];
            }
            this.Dispatcher.Invoke(() =>
                {
                    txtRx.Text = inData.ToString();
                });
            reading = false;
        }

        /// <summary>
        /// Type of commanding:
        /// Serial/CAN communication, Analog Input Reference or PWM on Analog Input.
        /// SRO0, SRO1, SRO2
        /// Included only these three, the others are not used often.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckSRO0_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("SRO0");
        }
        private void CheckSRO1_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("SRO1");
        }
        private void CheckSRO2_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("SRO2");
        }


        /// <summary>
        /// Velocity Mode or Position Mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckPosition_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("LR0");
            for (int i = 0; i < 1000; i++)
            {
                int a;
            }
            port.WriteLine("M");
        }
        private void CheckVelocity_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("V0");
            for (int i = 0; i < 1000; i++)
            {
                int a;
            }
            port.WriteLine("M");
        }


        //TODO: still need to test position part, even thought we may not need it.
        /// <summary>
        /// Button Even handlers, changes velocity and/or relative position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetVel_Click(object sender, RoutedEventArgs e)
        {
            string msg = "V" + txtVelocity.Text;
            port.WriteLine(msg);
        }
        private void BtnSetPos_Click(object sender, RoutedEventArgs e)
        {
            port.WriteLine("POS" + txtPosition.Text);
        }

        /// <summary>
        /// Makes sure that inputs are correct(numbers only).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(((TextBox)sender).Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                ((TextBox)sender).Text = ((TextBox)sender).Text.Remove(((TextBox)sender).Text.Length - 1);
            }
        }

        private void BtnUpdateController_Click(object sender, RoutedEventArgs e)
        {
            if(chbVkp.IsChecked ==true)
                port.WriteLine("POR" + txtVKp.Text);
            if (chbVki.IsChecked == true)
                port.WriteLine("I" + txtVKp.Text);
            if (chbPkp.IsChecked == true)
                port.WriteLine("PP" + txtPKp.Text);
            if (chbPkd.IsChecked == true)
                port.WriteLine("PD" + txtPKd.Text);
            if (chbSR.IsChecked == true)
                port.WriteLine("SR" + txtSR.Text);
        }
    }
}
