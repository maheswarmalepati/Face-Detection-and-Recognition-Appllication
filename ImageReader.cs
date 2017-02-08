using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;

namespace FinalSecond_Bit
{
    class ImageReader
    {
        public static DataPoint[] ReadAllData(String directory)
        {
            int fileNum = 0;
            int dataIndex = 0;
            DirectoryInfo diR = new DirectoryInfo(directory);

            //count the file number 
            foreach (FileInfo fi in diR.GetFiles())
            {
                fileNum++;
            }
            DataPoint[] dataArray = new DataPoint[fileNum];
            //count the file number 
            foreach (FileInfo fi in diR.GetFiles())
            {
                String fname = fi.FullName;
                Bitmap bmp = new Bitmap(Image.FromFile(fname));
                byte[,] ImgPixelData = new byte[bmp.Width, bmp.Height];
                if (ImageProc.IsGrayScale(bmp) == false) //make sure it is grayscale 
                {
                    ImageProc.ConvertToGray(bmp);
                }
                
                double[] binary = new double[8]; //to store the binary digits of neighborng pixels
                int[] pixelvalues=new int[9];// to store the pixel valuies of 9 pixels 
                int[] histogram = new int[257];// to store the number of occurences of abasolute values of the binary for each pixel
                for (int i = 1; i < bmp.Width-1; i++)
                {
                    for (int j = 1; j < bmp.Height-1; j++)
                    {
                         pixelvalues[0]= bmp.GetPixel(i-1,j-1).R;  pixelvalues[1] = bmp.GetPixel(i-1,j).R;  pixelvalues[2] = bmp.GetPixel(i-1,j+1).R;
                        //pixelvalues[0] = pixelvalues[1] = pixelvalues[2] = 0;
                        pixelvalues[3] = bmp.GetPixel(i,j-1).R;   pixelvalues[4] = bmp.GetPixel(i,j).R;    pixelvalues[5] = bmp.GetPixel(i,j+1).R;
                        pixelvalues[6] = bmp.GetPixel(i+1,j-1).R; pixelvalues[7] = bmp.GetPixel(i+1,j).R;  pixelvalues[8] = bmp.GetPixel(i+1,j+1).R;
                        for(int p=0;p<8;p++)
                        {
                            if(pixelvalues[p]<=pixelvalues[4])
                            { binary[p] = 0; }//assigning 0 if values is less than the middle
                            else
                            { binary[p] = 1; }//assigning 1 if the value is greater than middle
                        }
                        //to see the uniformity of the binary
                        int nooftransitions = findtransitions(binary);
                        if(nooftransitions<=2)
                        {
                            int z1 = (int)converttobinary(binary);
                            histogram[z1]++;
                        }
                        else
                        {
                            histogram[256]++;
                        }
                      }
                    
                    }

                //convert to 1D
                int totalPixels = 257;
                int tempIndex = 0;
               int classLabel = 2; //will only work with numbers 0-9
                dataArray[dataIndex] = new DataPoint(classLabel, totalPixels, histogram);
                // dataArray[dataIndex].Bmp = bmp;
                dataIndex++;
                Console.WriteLine(bmp.Width +"   "+ bmp.Height);
                if ((dataIndex % 500) == 0)
                    Console.WriteLine("iter: " + dataIndex);
                
            }
            return dataArray;
        }
        public static int findtransitions(double[] d)
        {
            int z=0;int p=0;
            for (p = 0; p < 7; p++)
            {
                if (d[p] != d[p + 1])
                { z++; }
            }
            if (d[7] == d[0])
            {
                z++;
            }
            return z;
        }
        public static double converttobinary(double[] c)
        {
            double x = 0;
            for (int j = 0; j < 8; j++)
            {
                x += (Math.Pow(2, j)) * (c[j]);
            }
            return x;
        }
        public static DataPoint ReadDataPoint(String fname)  // fname is full file name
        {
            Bitmap bmp = new Bitmap(Image.FromFile(fname));
            byte[,] ImgPixelData = new byte[bmp.Width, bmp.Height];
            if (ImageProc.IsGrayScale(bmp) == false) //make sure it is grayscale 
            {
                ImageProc.ConvertToGray(bmp);
            }
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    ImgPixelData[i, j] = bmp.GetPixel(i, j).R;
                }
            }

            //convert to 1D
            int totalPixels = bmp.Width * bmp.Height;
            int tempIndex = 0;
            int[] pointData = new int[totalPixels];

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    pointData[tempIndex] = ImgPixelData[i, j]; //convert to 0 to 1 scale 
                    tempIndex++;
                }
            }
            char[] seps = { '\\' };
            string[] parts = fname.Split(seps);
            int classLabel = parts[parts.Length - 1][0] - 48;
            DataPoint dt = new DataPoint(classLabel, totalPixels, pointData);
            return dt;
        }
    }
}
