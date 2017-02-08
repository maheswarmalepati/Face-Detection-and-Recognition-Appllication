using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalSecond_Bit
{
    public class DataPoint
    {
        public int[] data;
        public int[] Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        double bestDistance;
        public double BestDistance
        {
            get
            {
                return bestDistance;
            }
            set
            {
                bestDistance = value;
            }
        }



        int dimensionality;
        public int Dimensionality
        {
            get
            {
                return dimensionality;
            }
            set
            {
                dimensionality = value;
            }
        }

        string fileName;
        public string Filename
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        public DataPoint(int labelInput, int dimension, int[] dataInput)  // 28x28
        {

            dimensionality = dimension;
            data = new int[dimensionality];
            if (dataInput != null)  // added to accomodate Parallel.For
            {
                for (int i = 0; i < dimensionality; i++)
                {
                    data[i] = dataInput[i];
                }
            }
        }
    }
}
