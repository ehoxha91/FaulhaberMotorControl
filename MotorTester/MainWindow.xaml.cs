using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MotorTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort port;
        System.Timers.Timer refreshTimer;
        bool recording_to_file = false;
        string[] file_lines;
        int count_lines = 0;

        public MainWindow()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            foreach (string prt in ports)
            {
                cmbPorts.Items.Add(prt);
            }


            #region COMMANDS COMBOBOX
            cmbCommands.Items.Add("EN");
            cmbCommands.Items.Add("DI");
            cmbCommands.Items.Add("V");
            cmbCommands.Items.Add("HALLSPEED");
            cmbCommands.Items.Add("ENCSPEED");
            cmbCommands.Items.Add("ENCRES");
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
            #endregion

            #region Address/Network Combobox
            cmbAddress.Items.Add(0);
            cmbAddress.Items.Add(1);
            cmbAddress.Items.Add(2);
            cmbAddress.Items.Add(3);
            cmbAddress.Items.Add(4);
            cmbAddress.Items.Add(5);
            cmbAddress.Items.Add(6);
            cmbAddress.Items.Add(7);
            cmbAddress.Items.Add(8);
            cmbAddress.Items.Add(9);
            cmbAddress.Items.Add(10);

            cmbConnection.Items.Add("Single");
            cmbConnection.Items.Add("Network");
            #endregion


            refreshTimer = new System.Timers.Timer(2000);
            refreshTimer.Elapsed += RefreshTimer_Elapsed;
        }

        private READ reading_decode = READ.GENERAL;
        private void RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            refreshTimer.Stop();

            port.WriteLine("POS");
            reading_decode = READ.POSITON;
            while (reading_decode == READ.POSITON) { }

            port.WriteLine("V");
            reading_decode = READ.VELOCITY;
            while (reading_decode == READ.VELOCITY) { }
            reading_decode = READ.GENERAL;
            if (recording_to_file)
            {
                string tmp_string = txtActualPosition.Text + "; " + txtActualVelocity.Text + "; "
                                    + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString()
                                    + ":" + DateTime.Now.Millisecond.ToString();
                file_lines[count_lines] = tmp_string;
                count_lines++;
            }

            refreshTimer.Start();
        }

        /// <summary>
        /// Costum commands from combobox and input field.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (txtArg.Text == string.Empty) port.WriteLine(cmbCommands.SelectedValue.ToString());
            else port.WriteLine(cmbCommands.SelectedValue.ToString() + txtArg.Text);
        }

        /// <summary>
        /// Open Serial Port; Start synchronous timer that reads the position and velocity.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPorts.SelectedValue.ToString() != string.Empty)
            {
                if (port.IsOpen)
                {
                    port.Close();
                    port.DataReceived -= Port_DataReceived;
                    refreshTimer.Stop();
                    btnConnect.Background = Brushes.LightGray;
                    btnConnect.Content = "Connect";
                }
                else
                {
                    port.DataReceived += Port_DataReceived;
                    port.Open();
                    if (port.IsOpen)
                    {

                        this.Dispatcher.Invoke(() =>
                        {
                            btnConnect.Background = Brushes.LightGreen;
                            btnConnect.Content = "Disconnect";
                        });
                    }
                    refreshTimer.Start();
                }
            }
        }

        /// <summary>
        /// Handle received messages from serial port!
        /// Synchronized from the client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Se"></param>
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs Se)
        {
            string generalData = port.ReadTo("\n");

            switch (reading_decode)
            {
                case READ.POSITON:
                    this.Dispatcher.Invoke(() =>
                    {
                        txtActualPosition.Text = generalData;
                    });
                    reading_decode = READ.VELOCITY;
                    break;
                case READ.VELOCITY:
                    this.Dispatcher.Invoke(() =>
                    {
                        txtActualVelocity.Text = generalData;
                    });
                    reading_decode = READ.GENERAL;
                    break;
                case READ.GENERAL:
                    this.Dispatcher.Invoke(() =>
                        {
                            txtRx.Text = generalData;
                        });
                    reading_decode = READ.POSITON;
                    break;
                default:
                    this.Dispatcher.Invoke(() =>
                    {
                        txtRx.Text = generalData;
                    });
                    reading_decode = READ.POSITON;
                    break;
            }
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
            checkSRO1.IsChecked = false;
            checkSRO2.IsChecked = false;
        }
        private void CheckSRO1_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("SRO1");
            checkSRO0.IsChecked = false;
            checkSRO2.IsChecked = false;
        }
        private void CheckSRO2_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("SRO2");
            checkSRO1.IsChecked = false;
            checkSRO0.IsChecked = false;
        }

        /// <summary>
        /// Velocity Mode, Encoder Mode or Position Mode.
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
            checkVelocity.IsChecked = false;
            chbENCMODE.IsChecked = false;
        }
        private void CheckVelocity_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("V0");
            checkPosition.IsChecked = false;
            chbENCMODE.IsChecked = false;
        }
        private void ChbENCMODE_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("ENCMODE");
            checkPosition.IsChecked = false;
            checkVelocity.IsChecked = false;
        }

        //TODO: still need to test position part, even thought we may not need it.
        /// <summary>
        /// Button Even handlers, changes velocity and/or relative position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetVel_Click(object sender, RoutedEventArgs e)
        {
            port.WriteLine("V" + txtVelocity.Text);
        }
        private void BtnSetPos_Click(object sender, RoutedEventArgs e)
        {
            port.WriteLine("LR" + txtPosition.Text);
            for (int i = 0; i < 1000; i++)
            {

            }
            port.WriteLine("M");
        }

        /// <summary>
        /// Write the settings to EEPROM - Be careful don't exced 10000 writings!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Don't do this more than 10000 times!");
            port.WriteLine("EEPSAV");
        }

        /// <summary>
        /// Makes sure that inputs are correct(signed numbers only).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            string _arg = ((System.Windows.Controls.TextBox)sender).Text;
            char first_c = ' ';
            if (_arg != string.Empty)
               first_c = _arg[0];
            if (System.Text.RegularExpressions.Regex.IsMatch(((System.Windows.Controls.TextBox)sender).Text, "[^0-9]") && first_c != '-')
            {
                System.Windows.MessageBox.Show("Please enter only numbers.");
                ((System.Windows.Controls.TextBox)sender).Text = ((System.Windows.Controls.TextBox)sender).Text.Remove(((System.Windows.Controls.TextBox)sender).Text.Length - 1);
            }
        }

        /// <summary>
        /// Update Position(PD), Velocity Filters(PI) and sampling rate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Open Github page;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("www.github.com/ehoxha91");
        }

        private void BtnSetVelProfile_Click(object sender, RoutedEventArgs e)
        {
            port.WriteLine("AC" + txtAv.Text);
            for (int i = 0; i < 1000; i++)
            {

            }
            port.WriteLine("DEC" + txtDv.Text);
            for (int i = 0; i < 1000; i++)
            {

            }
            if(txtVmode_max.Text != string.Empty)
            {
                port.WriteLine("SP" + txtVmode_max.Text);
            }
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (recording_to_file == false)
            {
                recording_to_file = true;
                btnRecord.Background = Brushes.Red;
            }
            else
            {
                recording_to_file = false;
                btnRecord.Background = Brushes.LightGray;

                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.ShowDialog();
                string path = dialog.SelectedPath + @"\pos_vel_time.txt";
                using (StreamWriter file = new StreamWriter(@path, true))
                {
                    for (int i = 0; i < count_lines; i++)
                    {
                        file.WriteLine(file_lines[i]);
                    }
                }

            }
        }

        private void CmbPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port = new SerialPort(cmbPorts.SelectedValue.ToString(), 9600, Parity.None, 8, StopBits.One);
            btnConnect.IsEnabled = true;
        }


        /// <summary>
        /// Speed sensor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChbEncSpeed_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("ENCSPEED");
            chbHallSpeed.IsChecked = false;
        }
        private void ChbHallSpeed_Checked(object sender, RoutedEventArgs e)
        {
            port.WriteLine("HALLSPEED");
            chbEncSpeed.IsChecked = false;
        }

        private void CmbAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port.WriteLine("NODEADR"+cmbAddress.SelectedValue.ToString());
        }

        private void CmbConnection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cmbConnection.SelectedValue.ToString()=="Single")
            {
                port.WriteLine("NET0");
            }
            else
            {
                port.WriteLine("NET1");
            }
        }
    }

    public enum READ
    {
        GENERAL = 0,
        POSITON = 1,
        VELOCITY = 2
    }
}
