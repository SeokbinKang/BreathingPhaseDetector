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
    public struct errorContainer
    {
        public double raw;
        public double correct;
        public double rpError; //relative phase error
        public double stError; //state error
        public double siError; //simple error
        public double latValue; //latency
        public int time; //time in ms
        public errorContainer(double r, double c, double rE, double stE, double siE, double lV, int t)
        {
            raw = r;
            correct = c;
            rpError = rE;
            stError = stE;
            siError = siE;
            latValue = lV;
            time = t;
        }
        public String exportTextFormat()
        {
            String combine = "";
            combine += time.ToString() + "\t";
            combine += stError.ToString() + "\t";
            combine += siError.ToString() + "\t";
            combine += rpError.ToString() + "\t";
            combine += latValue.ToString() + "\t";
            return combine;
        }

    }

    public partial class Form1 : Form
    {
        

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        private static int pauseTime = 1500;
        private static int inhaleTime = 2000, exhaleTime = 2000;
        private static int totalTime = 0;
        private static int totalCount = 0;   //number of points examined
        private static double totalError = 0;  //total error accumulated through examination of each interval pt
        private static double stateError = 0; //error determined by conflicting state determination (inhale/exhale)
        private static double simpleError = 0; // simple error calculated by binary (0 and 1) treatment of phase error
        private static double latency = 0; //latency between algorithm and sensor
        private static int latencyCount = 0;
        private static int lastTime = 0;
        private static double correctPhase = 0;

        private static double phaseState = 0;
        private static BreathingPhaseData rawBase;
        private static BreathingPhaseData rawMax;

        private static Boolean first = true;    // signifies "first instance" that a 

        private static System.Windows.Forms.Timer liveStreamTimer;
        private static System.Windows.Forms.Timer testTimer;

        private static int nDataPoints;

        private static Form1 currentInstance;

        private static List<BreathingPhaseData> DataList;
        private static List<errorContainer> errorList;
        private static List<double> lowPeakValues;
        private static List<double> highPeakValues;

        private static double chart1_minVal = 90;
        private static double chart1_maxVal = 110;
        private static double chart2_minVal = 0;
        private static double chart2_maxVal = 110;

        private Boolean test_started = false;
        //private static int accumulatedTime = 0;
        private static int nDataPointsLine = 0;
        private static List<int> phases = new List<int> ();
        
        public Form1()
        {
            InitializeComponent();
            AllocConsole();
            nDataPoints = 0;
            currentInstance = this;
            DataList= new List<BreathingPhaseData>();
            errorList = new List<errorContainer>();
            
        }
        private static void cleanCharts()
        {
            chart1_minVal = 100; //modified from 100
            chart1_maxVal = 110;
            chart2_minVal = 0;
            chart2_maxVal = 110;
            currentInstance.chart1.Series[0].Points.Clear();  //raw
            currentInstance.chart1.Series[1].Points.Clear();   //phase
            currentInstance.chart2.Series[0].Points.Clear(); //test
            currentInstance.chart2.Series[1].Points.Clear(); //test
            currentInstance.chart2.Series[2].Points.Clear(); //test
            currentInstance.chart2.Series[3].Points.Clear(); //test
            currentInstance.chart2.Series[4].Points.Clear(); //test
            //currentInstance.chart2.Series[5].Points.Clear();
            currentInstance.chart1.ChartAreas[0].AxisY.Minimum = chart1_minVal;
            currentInstance.chart1.ChartAreas[0].AxisY.Maximum = chart1_maxVal;
            currentInstance.chart2.ChartAreas[0].AxisY.Minimum = chart2_minVal;
            currentInstance.chart2.ChartAreas[0].AxisY.Maximum = chart2_maxVal;

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

        /// <summary>
        /// Adds a test point to chart 2 (test graph)
        /// </summary>
        /// <param name="val1"></param>     base area value
        /// <param name="val2"></param>     exhale area value
        /// <param name="val3"></param>     inhale area value
        /// <param name="val_stop"></param> pause area value
        /// <param name="val_norm"></param> "normal breathing" 

        private static void AddTestPointtoChart(double val1, double val2, double val3, double val_stop, double val_norm)
        {
            currentInstance.chart2.Series[0].Points.AddY(val1); //base
            currentInstance.chart2.Series[1].Points.AddY(val2); //exhale
            currentInstance.chart2.Series[2].Points.AddY(val_stop); //pause
            currentInstance.chart2.Series[3].Points.AddY(val3); //inhale
            //currentInstance.chart2.Series[5].Points.Add(val_norm);
            //currentInstance.chart2.Series[0].Points.Remove()
            int lastIndexBase = currentInstance.chart2.Series[0].Points.Count - 1;

            if (val1 < chart2_minVal)
            {
                chart2_minVal = val1;
                currentInstance.chart2.ChartAreas[0].AxisY.Minimum = chart2_minVal;
            }
            if (val3 > chart2_maxVal)
            {
                chart2_maxVal = val3;
                currentInstance.chart2.ChartAreas[0].AxisY.Maximum = chart2_maxVal;
            }
            
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

        private void button4_Click(object sender, EventArgs e)  //Start or Stop testing
        {

            test_started = !test_started;

            Console.WriteLine("Button Clicked");
            if (test_started)
            {
                int timeSum = 2500;
                this.button4.Text = "Stop Test";
               
                this.chart2.Update();
                int[] pattern = new int[] { 0, 1, 0, 2}; //if modified, one must also modify the pattern time calculations in calcPhaseAccuracy
                cleanCharts();
                //chart2.Series[4].Points.AddY(chart2_maxVal);
                chart2.Update();
                /*
                for (int i = 0; i < 30; i++)
                {
                    AddTestPointtoChart(54.5, 0, 0, 0, 1);
                }*/
                for (int i = 0; i < pattern.Length; i++)
                {
                   
                    for (int time_elapsed = 0; time_elapsed < timeSum; time_elapsed += 50)
                    {
                        switch (pattern[i])
                        {
                            case 0:
                                timeSum = pauseTime; //1.5 seconds
                                AddTestPointtoChart(54.5, 0, 0, 1, 0);  //pause
                                break;
                            case 1:
                                timeSum = inhaleTime; //2 sec
                                AddTestPointtoChart(54.5, 0, 14.5, 0, 0);  //inhale
                                break;
                            case 2:
                                timeSum = exhaleTime; //2 sec
                                AddTestPointtoChart(40, 14.5, 0, 0, 0); //exhale
                                break;
                        }
                       
                    }
                }
                for (int i = 0; i< 14; i++)   //700 milliseconds in, draw the line 
                {
                    currentInstance.chart2.Series[4].Points.AddY(0.0);
                }

                currentInstance.chart2.Series[4].Points.AddY(chart2_maxVal);
                chart2.Update();
                DataList.Clear();
                BreathingPhaseMain.init();
                //BreathingPhaseMain.InitializeSensor();
                testTimer = new System.Windows.Forms.Timer();
                testTimer.Tick += new EventHandler(Form1.loopTest);
                //testTimer.Tick += new EventHandler(Form1.calculatePhaseAccuracy); 
                testTimer.Interval = 50;
                testTimer.Start();
                
            }
            else
            {
                this.button4.Text = "Start Test";
                BreathingPhaseMain.UnInitializeSensor();
                testTimer.Stop();
                testTimer.Dispose();
                this.chart2.Update();
                Console.WriteLine("Test Stopped");

                //upload error values to a text file

                String filename = this.textBox_uID.Text + "_" + errorList.Count.ToString() + "_" + "ERROR.txt";

                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@filename))
                {
                    foreach (errorContainer item_ in errorList)
                    {
                        

                        file.WriteLine(item_.exportTextFormat());

                    }
                }
                MessageBox.Show("File Saved to " + filename, "-", MessageBoxButtons.OK, MessageBoxIcon.Information);


            }



        }
        
       
        private static void loopTest(Object myObject, EventArgs myEventArgs)
        {
            double rawVal;
            int rawValInt;
            int phase;
            int counter = currentInstance.chart2.Series.Count-1;
            for (int i = 0; i < counter; i++)
            {
                int series_length = currentInstance.chart2.Series[i].Points.Count;             
                if (i == 5)  //if iteration reaches normal breathing series
                {
                    currentInstance.chart2.Series[i].Points.RemoveAt(0);
                    currentInstance.chart2.Series[i].Points.AddY(0.0);
                }
                else if (i < 4)
                {
                    currentInstance.chart2.Series[i].Points.RemoveAt(0);
                    System.Windows.Forms.DataVisualization.Charting.DataPoint first_point = currentInstance.chart2.Series[i].Points.ElementAt(0);
                    currentInstance.chart2.Series[i].Points.Insert(series_length - 1, first_point);
                }
            }
            currentInstance.chart2.Update();



            rawVal = BreathingPhaseMain.readCaliData();
            rawValInt = (int)rawVal;
            BreathingPhaseData data_ = new BreathingPhaseData(currentInstance.textBox_uID.Text, rawVal, 0, nDataPointsLine);
            DataList.Add(data_);
            nDataPointsLine++;
            phase = BreathingPhaseMain.tick(rawVal);
            phases.Add(phase);



            errorContainer errorValues = calculatePhaseAccuracy();
            if (totalCount != 0)
            {
                errorValues.rpError /= totalCount;
                errorValues.siError /= totalCount;
                errorValues.stError /= totalCount;
            }
            errorList.Add(errorValues); 
            Console.WriteLine("Current Average % Error " + errorValues.rpError);
            Console.WriteLine("Percent of Incorrectly Determined States " + errorValues.stError);
            Console.WriteLine("Percent of Incorrectly Determined Phases " + errorValues.siError);
            Console.WriteLine("Latency Value(ms): " + errorValues.latValue);
        }

        /// <summary>
        /// calculates 3 types of error (inhale/exhale STATE ERROR, binary PHASE ERROR, and AVERAGE PHASE ERROR)
        /// calculate latency
        /// </summary>

        private static errorContainer calculatePhaseAccuracy()
        {
            
            lowPeakValues = BreathingPhaseMain.getLowPeakValues();
            highPeakValues = BreathingPhaseMain.getHighPeakValues();
            double tempData = DataList.Last().rawData;
            int phase = phases.Last();

            totalTime += 50;//time accumulates by 50 each iteration

            int tempTime = 0;
            int patternTime = pauseTime * 2 + inhaleTime + exhaleTime;  //pattern total time
            tempTime = (totalTime % patternTime) + 700;  //700 ms represents the vertical black line to account for user reaction
            tempTime = tempTime % patternTime;

            

            if (lowPeakValues.Count != 0 && lastTime != 0) //low peak values exist, last time was determined
            {
                latency += (totalTime - lastTime); //difference in time in milliseconds
                latencyCount++;
                lowPeakValues.RemoveAt(lowPeakValues.Count -1);
            }
            if (highPeakValues.Count != 0 && lastTime != 0)
            {
                latency += (totalTime - lastTime); //difference in time in milliseconds
                latencyCount++;
                highPeakValues.RemoveAt(highPeakValues.Count - 1);
            }


            if (tempTime == 0)   //end of exhale phase
            {
                phaseState = 300; //exhale phase value
                first = true;
                lastTime = totalTime;
                calcAvgPhaseError();
            }
            else if (tempTime == pauseTime + inhaleTime) //end of inhale phase
            {
                phaseState = 100; //inhale phase value
                first = true;
                lastTime = totalTime;
                calcAvgPhaseError();
            }
            
            else if (tempTime == pauseTime)  //start of inhale phase
            {
                rawBase = DataList.Last();

            }
            else if (tempTime == pauseTime*2 + inhaleTime )//start of exhale phase
            {
                rawMax = DataList.Last();
            }

            //totalTime += 50;
            //totalCount = 0;
            errorContainer errorValues = new errorContainer(tempData, correctPhase,totalError, stateError, simpleError, latency/latencyCount, totalTime);  //array of error values
            return errorValues;
        }


        //method for calculating all three errors
        private static void calcAvgPhaseError()
        {
            rawMax = DataList.Last();
            double absolute_diff = Math.Abs(rawMax.rawData - rawBase.rawData);
            double difference = Math.Abs(rawMax.rawData - rawBase.rawData);
            
            while (difference != 0)   //while the interval examined still has points to examine
            {

                if (DataList.Count > 1)
                {

                    double algPhase = phases.Last();
                    /*
                    if (first)
                    {
                        first = false;
                        if (!(algPhase >= (phaseState+ 100) && algPhase < (phaseState + 200)))
                        {                            
                            stateError++;
                        }

                    }//encountered end of an inhale/exhale interval, phases should be inhale-pause/exhale-pause 
                    else if (!(algPhase >= phaseState && algPhase < (phaseState + 100)))
                    {
                        stateError++;
                    }//normally, 1xx is inhale, 3xx is exhale (so 100-200, 300-400)*/


                    if (!(algPhase >= (phaseState) && algPhase < (phaseState + 200)))
                    {
                        stateError++;
                    } //just generalize phase to be from 100-299 = inhale, 300-499 = exhale, works better


                    algPhase = phases.Last() % 100;  //algorithm generated phase percent
                    //Generate own concept of phase percent
                    correctPhase = difference / absolute_diff * 100;
                    correctPhase = Math.Round(correctPhase);
                    if( correctPhase > 99 && correctPhase < 100)
                    {
                        correctPhase = 99;
                    }else if (correctPhase >= 100)
                    {
                        correctPhase = 0;
                    }
                    //Console.WriteLine("My Correct: " + correctPhase);
                    //Console.WriteLine("Algorithm: " + algPhase);
                    double phaseError = Math.Abs(algPhase - correctPhase)/100; //100 is baseline gap in interval

                    if (algPhase != correctPhase)
                    {
                        simpleError++; //simple binary error 
                    }

                    phases.RemoveAt(phases.Count - 1); //delete last element of phases list
                    DataList.RemoveAt(DataList.Count - 1); //delete last element of data list
                    totalCount++; //number of data points observed
                    totalError += phaseError;  //total error change


                    rawMax = DataList.Last(); //update current iteration of points
                    difference = Math.Abs(rawMax.rawData - rawBase.rawData);

                }
                else
                {
                    Console.WriteLine("DataList is empty");
                    break;
                }

            }//check the phases within the interval between rawBase and rawMax data points


            

        }
        
    }
}
