using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;
using System.Diagnostics;

namespace BreathingPhaseModule
{
    public interface IBreathingPhaseDataModifier
    { int phase { set; } }
    public struct BreathingPhaseData : IBreathingPhaseDataModifier
    {

        public double rawData;
        public string timeStamp;
        public int phase //1~100 : inhalse, -100~-1 : exhale
         {
            set;
            get;
        }
    public String userID;
        public int sequenceID;
        public BreathingPhaseData(String uid, double rawVal,int PhaseVal,int seqN)
        {
            rawData = rawVal;
            userID = uid;
            phase = PhaseVal;
            timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            sequenceID = seqN;

        }
     
        public String exportTextFormat()
        {
            String result = "";
            result += userID + "\t";
            result += sequenceID.ToString() + "\t";
            result += timeStamp+ "\t";
            result += rawData.ToString("F2") + "\t";
            result += phase.ToString();
            return result;

        }
    }
    public class BreathingPhaseAnalyzer
    {
        private int paramMaxItem = 300;
        private int paramMinGradientWindow = 5;
        private int paramMinSecondDWindow = 2 * 5;
        private double paramZeroThreashold = 0.01;

        private double[] temporalWeight = { 0.5, 0.3, 0.1, 0.1 };
        private double lowPeakVal;
        private double highPeakVal;
        private double[] dataArray; //circular array. controlled by index.
        private double[] gradientArray;
        private double[] secondDerivativeArray;
        private List<int> phaseResultList;

        private List<double> lowPeakValueList;
        private List<double> highPeakValueList;
        int curIndex;
        int nItem;
        public BreathingPhaseAnalyzer()
        {
            //constructor : do nothing
            lowPeakValueList = new List<double>();
            highPeakValueList = new List<double>();
            phaseResultList = new List<int>();
            dataArray = new double[paramMaxItem];
            gradientArray = new double[paramMaxItem];
            secondDerivativeArray = new double[paramMaxItem];
            
            init();

        }
        public void init()
        {
            
            curIndex = -1;
            nItem = 0;
            lowPeakValueList.Clear();
            highPeakValueList.Clear();
            lowPeakVal = -1;
            highPeakVal = -1;
        }

        public int pushData(double val)
        {
            curIndex++;
            if (curIndex >= paramMaxItem) curIndex = 0;
            dataArray[curIndex] = val;
            nItem++;
            int phaseResult=0;
            //calculate gradient
            calGradient();
            //calculate second derivative - retro
            calSecondDerivative();
            //phase detection
            phaseResult = determinePhase();

            Console.WriteLine(dataArray[curIndex].ToString() + "\t" + gradientArray[curIndex].ToString() +"\t"+phaseResult.ToString());

            return phaseResult;
        }
        public void setParameter()
        {

        }
        private double calGradient()
        {
            if(nItem< paramMinGradientWindow)
            {
                gradientArray[curIndex] = 0;
                return 0;
            }
            double AvgGradientValue = 0;
            for(int d = 1; d <= temporalWeight.Length; d++)
            {
                double unitGradient;
                int diffIndex = curIndex - d;
                if (diffIndex < 0)
                    diffIndex = paramMaxItem + diffIndex;

                unitGradient = (dataArray[curIndex] - dataArray[diffIndex]) / d;
                AvgGradientValue += unitGradient * temporalWeight[d - 1];

            }
            gradientArray[curIndex] = AvgGradientValue;
            return 1;
        }
        private double detectPeak()
        {
            return 1;
        }
        private double calSecondDerivative()
        {
            if(nItem<(paramMinSecondDWindow+1))
            {
                return 0;
            }
            //calculate secondderivative for curindex - paramMinSecondDWindow/2
            int SD = 0;
            for (int i = 1; i <= paramMinSecondDWindow;i++) {
                int diffIndex = curIndex - i;
                if (diffIndex < 0)
                    diffIndex = paramMaxItem + diffIndex;
                if (gradientArray[diffIndex] > 0) SD++;
                else SD--;
            }
            if(SD==0)
            {
                int peakIndex = curIndex - paramMinSecondDWindow / 2;
                if (peakIndex < 0)
                    peakIndex = paramMaxItem + peakIndex;
                //peak
                if (gradientArray[curIndex]>0)
                {

                    //low peak
                    lowPeakValueList.Insert(0, dataArray[peakIndex]);
                    if (lowPeakVal == -1) lowPeakVal = dataArray[peakIndex];
                    else lowPeakVal = lowPeakVal * 0.5 + dataArray[peakIndex] * 0.5;
                } else
                {
                    //high peak
                    highPeakValueList.Insert(0, dataArray[peakIndex]);
                    if (highPeakVal == -1) highPeakVal = dataArray[peakIndex];
                    else highPeakVal = highPeakVal * 0.5 + dataArray[peakIndex] * 0.5;
                    
                }

            }

            return 1;
        }
        private int determinePhase()
        {
            int result=0; //coding : 1xx : inhale, 2xx : inhale-pause , 3xx : exhale, 4xx : exhale-pause
            double gradientVal = gradientArray[curIndex];
            bool isPause = false;
            //determine inhale/exhale
            if (Math.Abs(gradientVal) < paramZeroThreashold)
            {
            //    Console.WriteLine(gradientVal.ToString() + "\t" + Math.Abs(gradientVal).ToString() + "\t" + paramZeroThreashold);
                // pause
                if(nItem==1)
                {
                    result = 200; //default : inhale
                } else
                {
                    result = phaseResultList[0];
                    if (result >= 100 && result < 200) result += 100;
                    if (result >= 300 && result < 400) result += 100;
                    //may need temporal aggregation 
                    isPause = true;
                }
            } else
            { // normal
                if (gradientVal > 0) result = 100;
                else result = 300;
            }
            

            //determine phase level
            if(!isPause)
            {
                if(lowPeakVal==-1 || highPeakVal==-1)
                {
                    // initial stage : cannot estimate phase level
                    //do nothing
                } else
                {
                    double phaseLevel=0;                    
                    Debug.Assert(result >= 100);
                    Debug.Assert(highPeakVal - lowPeakVal > 0);
                    if(result/100 == 1)
                    {
                        //inhale
                        
                        phaseLevel = (((dataArray[curIndex] - lowPeakVal) * 100) / (highPeakVal - lowPeakVal));      
                                              
                    } else if(result/100 ==3)
                    {
                        phaseLevel = (((dataArray[curIndex] - lowPeakVal) * 100) / (highPeakVal - lowPeakVal));
                        phaseLevel = 100 - phaseLevel;
                    } else
                    {
                        //should not reach here
                        Debug.Assert(true);
                    }
                    if (phaseLevel > 99) phaseLevel = 99;
                    if (phaseLevel < 0) phaseLevel = 0;
                    result += (int)phaseLevel;

                }
            }
            phaseResultList.Insert(0, result);
            return result;
        }
    }
}
