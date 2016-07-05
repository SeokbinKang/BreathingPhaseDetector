using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Timers;
using BreathingPhaseModule;
namespace BreathingPhaseUI
{    
    public partial class Form1 : Form
    {
        

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

      

        private static System.Windows.Forms.Timer liveStreamTimer;

        private static int nDataPoints;

        private static Form1 currentInstance;

        private static List<BreathingPhaseData> DataList;

        private static double chart1_minVal = 90;
        private static double chart1_maxVal = 110;

        public Form1()
        {
            InitializeComponent();
            AllocConsole();
            nDataPoints = 0;
            currentInstance = this;
            DataList= new List<BreathingPhaseData>();
            
        }
        private static void cleanCharts()
        {
            chart1_minVal = 100;
            chart1_maxVal = 110;
            currentInstance.chart1.Series[0].Points.Clear();
            currentInstance.chart1.Series[1].Points.Clear();
            currentInstance.chart1.ChartAreas[0].AxisY.Minimum = chart1_minVal;
            currentInstance.chart1.ChartAreas[0].AxisY.Maximum = chart1_maxVal;
            
        }
        
        private static void LiveStreamTimerTick(Object myObject,
                                            EventArgs myEventArgs)
        {
            double rawVal;
            int rawValInt;
            int phase;
            
            rawVal =  BreathingPhaseMain.readCaliData();
            
            rawValInt = (int)rawVal;

            AddDataPointtoChart(rawVal);
            BreathingPhaseData data_ = new BreathingPhaseData(currentInstance.textBox_uID.Text, rawVal, 0, nDataPoints);
            DataList.Add(data_);
            nDataPoints++;

           phase=  BreathingPhaseMain.tick(rawVal);

            AddResultPointtoChart(phase);
            Console.Write(phase);

        }
        private static void AddDataPointtoChart(double val)
        {
            currentInstance.chart1.Series[0].Points.AddY(val);
            
            if(val< chart1_minVal)
            {
                chart1_minVal = val;
                currentInstance.chart1.ChartAreas[0].AxisY.Minimum = chart1_minVal;
            }
            if (val > chart1_maxVal)
            {
                chart1_maxVal = val;
                currentInstance.chart1.ChartAreas[0].AxisY.Maximum = chart1_maxVal;
            }
            currentInstance.chart1.Update();
        }
        private static void AddResultPointtoChart(int val)
        {
            
            double yval;
            double Yrange = chart1_maxVal - chart1_minVal;
            double Ycenter = chart1_minVal + Yrange / 2;
            int phase = val / 100;
            double level = val % 100;
            if(phase <=2)
            {
                yval = Ycenter + level / 100 * (Yrange / 2);
            } else
            {
                yval = Ycenter - level / 100 * (Yrange / 2);
            }
            currentInstance.chart1.Series[1].Points.AddY(yval);
         
            currentInstance.chart1.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BreathingPhaseMain.InitializeSensor();

            for (int i=0;i<60;i++)
            {
                BreathingPhaseMain.readCaliData();
                Thread.Sleep(50);

            }
            // UnInitializeSensor();
        
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (liveStreamTimer!=null && liveStreamTimer.Enabled)
            {
                BreathingPhaseMain.UnInitializeSensor();
                liveStreamTimer.Stop();
                liveStreamTimer.Dispose();
                this.button2.Text = "Live Stream";
            }            else {
                cleanCharts();
                DataList.Clear();
                BreathingPhaseMain.init();
                //set time for pulling data
                liveStreamTimer = new System.Windows.Forms.Timer();
                liveStreamTimer.Tick += new EventHandler(LiveStreamTimerTick);
                liveStreamTimer.Interval = 50;
                liveStreamTimer.Start();
               
                this.button2.Text = "Stop Stream";
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {

            DateTime now = DateTime.Now;
            String date_ = now.ToString("yyyy_MM_dd_HH_mm");
            String filename = this.textBox_uID.Text + "_" + DataList.Count.ToString() + "_" + date_+".txt";

            using (System.IO.StreamWriter file =
           new System.IO.StreamWriter(@filename))
            {
                foreach (BreathingPhaseData item_ in DataList)
                {
                    // If the line doesn't contain the word 'Second', write the line to the file.
                    
                        file.WriteLine(item_.exportTextFormat());
                    
                }
            }
            MessageBox.Show("File Saved to " + filename, "-", MessageBoxButtons.OK, MessageBoxIcon.Information);



        }

        private void button3_Click(object sender, EventArgs e)
        {
            DataList.Clear();
            
            cleanCharts();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;
            openFileDialog1.InitialDirectory = "./";

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            
            // Process input if the user clicked OK.
            if (userClickedOK == DialogResult.OK)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog1.OpenFile();
                                

                using (System.IO.StreamReader reader = new System.IO.StreamReader(fileStream))
                {
                    string dataLine;
                    // Read the first line from the file and write it the textbox.
                    while ((dataLine = reader.ReadLine()) != null)
                    {
                        string[] items = dataLine.Split('\t');
                        BreathingPhaseData data_ = new BreathingPhaseData(items[0], double.Parse(items[3]), int.Parse(items[4]), int.Parse(items[1]));
                        data_.timeStamp = items[2];
                        DataList.Add(data_);
                        AddDataPointtoChart(data_.rawData);
                    }
                }
                fileStream.Close();


                //push data to analyzer

                BreathingPhaseMain.debugPushData(DataList);

                //Console.Write(DataList.Count);
                //display analysis result

            foreach(BreathingPhaseData t in DataList)
                {
                    AddResultPointtoChart(t.phase);
                }
            }
        }
    }
}
