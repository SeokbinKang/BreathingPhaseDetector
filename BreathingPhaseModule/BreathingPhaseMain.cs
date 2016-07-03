using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
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

        public static BreathingPhaseAnalyzer pBPA  = new BreathingPhaseAnalyzer();

        public static double readCaliData()
        {
            double val = readCalibratedData();
            Console.WriteLine(val);
            return val;
        }
        public static int tick()
        {
            //read rawdata
            //analysis
            //return phaseResult
            return 1;
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
