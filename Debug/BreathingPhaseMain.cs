using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using System.Threading.Tasks;
using System.Threading;

using System.Runtime.InteropServices;
using System.IO;
using System.Timers;

namespace BreathingPhaseModule
{
    public class BreathingPhaseMain
    {
        [DllImport("SensorDeviceWrapper.dll")]
        public static extern int InitializeSensor();
        [DllImport("SensorDeviceWrapper.dll")]
        public static extern int UnInitializeSensor();
        [DllImport("SensorDeviceWrapper.dll")]
        public static extern double readCalibratedData();

        public static BreathingPhaseAnalyzer pBPA  = new BreathingPhaseAnalyzer();  //analyzer object

         ~BreathingPhaseMain()   //destructor
        {
             UnInitializeSensor();
        }
        public static double readCaliData()
        {
            double val = readCalibratedData();
            Console.WriteLine(val);
            return val;
        }
        public static int init()
        {
            InitializeSensor();
            pBPA.init();
            return 1;
        }
        public static int tick()
        {
            int result;
            //read rawdata
            double val = readCalibratedData();
            //analysis
            result = pBPA.pushData(val);
            //return phaseResult

            return result;
        }
        public static List<double> getLowPeakValues()
        {
            return pBPA.getLowPeaks();
        }
        public static List<double> getHighPeakValues()
        {
            return pBPA.getHighPeaks();
        }

        public static int tick(double val)
        {
            int result;
            //read rawdata            
            //analysis
            result = pBPA.pushData(val);
            //return phaseResult

            return result;
        }
        public static int debugPushData(List<BreathingPhaseData> dataList)
        {
            pBPA.init();
            int result;
            for(int i=0;i<dataList.Count;i++)
            {
                result = pBPA.pushData(dataList[i].rawData);               
                
                BreathingPhaseData tt = dataList[i];
                tt.phase = result;
                dataList[i] = tt;
                
            }
       
            //data file pushed from UI
            //create internal data array
            //perform analysis while pushing
            //may return analysis results
            return 1;
        }


    }
}
