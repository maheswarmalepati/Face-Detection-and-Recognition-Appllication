using PCALib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FinalSecond_Bit
{
    public partial class Form1 : Form
    {
        Matrix EF = new Matrix(10304, 50); Matrix coefficients = new Matrix(200, 50);
        int numimages = 0; DataPoint[] trainData=new DataPoint[200];
        public Form1()
        {
            InitializeComponent();
        }

        public void Train_Click(object sender, EventArgs e)
        {

            String trainDir = "C:\\Users\\MAHI\\Downloads\\ATTDataSet\\Training\\";//"D:\\csharp2016\\DeepLearning\\MNIST\\data\\train\\";
            trainData = ImageReader.ReadAllData(trainDir);
            MessageBox.Show(trainData[0].Data.Length.ToString());
            MessageBox.Show(trainData.Length.ToString());
            numimages = trainData.Length;
            MessageBox.Show("training is done");
        }

        public static double cov(DataPoint a, DataPoint b)
        {
            double result = 0;
            for (int i = 0; i < a.Data.Length; i++)
            {
                result += a.Data[i] * b.Data[i];
            }
            return result;
        }
        private void Load_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "(image files)|*.jpg";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OriginalImage.Image = new Bitmap(ofd.FileName);
            }
            textBox1.Text = "OriginalImage";
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
        public static int findtransitions(double[] d)
        {
            int z = 0; int p = 0;
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

        private void StartMatching_Click(object sender, EventArgs e)
        {

            Bitmap bmp1 = new Bitmap(OriginalImage.Image);
            byte[,] ImgPixelData = new byte[OriginalImage.Width, OriginalImage.Height];
            if (ImageProc.IsGrayScale(OriginalImage.Image) == false) //make sure it is grayscale 
            {
                ImageProc.ConvertToGray(bmp1);
            }
            double[] binary1 = new double[8]; //to store the binary digits of neighborng pixels
            byte[] pixelvalues1 = new byte[9];// to store the pixel valuies of 9 pixels 
            int[] histogram1 = new int[257];// to store the number of occurences of abasolute values of the binary for each pixel
            for (int i = 1; i < bmp1.Width - 1; i++)
            {
                for (int j = 1; j < bmp1.Height - 1; j++)
                {
                    pixelvalues1[0] = bmp1.GetPixel(i - 1, j - 1).R; pixelvalues1[1] = bmp1.GetPixel(i - 1, j).R; pixelvalues1[2] = bmp1.GetPixel(i - 1, j + 1).R;
                    pixelvalues1[3] = bmp1.GetPixel(i, j - 1).R; pixelvalues1[4] = bmp1.GetPixel(i, j).R; pixelvalues1[5] = bmp1.GetPixel(i, j + 1).R;
                    pixelvalues1[6] = bmp1.GetPixel(i + 1, j - 1).R; pixelvalues1[7] = bmp1.GetPixel(i + 1, j).R; pixelvalues1[8] = bmp1.GetPixel(i + 1, j + 1).R;
                    for (int p = 0; p < 8; p++)
                    {
                        if (pixelvalues1[p] <= pixelvalues1[4])
                        { binary1[p] = 0; }//assigning 0 if values is less than the middle
                        else
                        { binary1[p] = 1; }//assigning 1 if the value is greater than middle
                    }
                    int nooftransitions = findtransitions(binary1);
                    if (nooftransitions <= 2)
                    {
                        int z1 = (int)converttobinary(binary1);
                        histogram1[z1]++;

                    }
                    else
                    {
                        histogram1[256]++;
                    }
                }
            }
            
           List<double> distance = new List<double>();
           double[] distance2 = new double[200];
            for (int i = 0; i <200 ; i++)
            {
                double dist = 0;
                for (int j = 0; j < 257; j++)
                {
                    dist += ((histogram1[j]-trainData[i].Data[j])* (histogram1[j] - trainData[i].Data[j]));
                   
                }
                dist = Math.Sqrt(dist);
               distance.Add(dist);
                distance2[i] = dist;
            }
            distance.Sort();
           // MessageBox.Show("The minimum distance is " + distance[0].ToString() + "---" + distance[1].ToString());
            int z = 0, k = 0, l = 0, m = 0;
            for (int i = 0; i < distance2.Length; i++)
            {
                if (distance[0] == distance2[i])
                { z = i; }
                if (distance[1] == distance2[i])
                { k = i; }
                if (distance[2] == distance2[i])
                { l = i; }
                if (distance[3] == distance2[i])
                { m = i; }
            }
            int fileNum2 = 0; DirectoryInfo diR = new DirectoryInfo("C:\\Users\\MAHI\\Downloads\\ATTDataSet\\Training\\");
            foreach (FileInfo fi in diR.GetFiles())
            {
                if (fileNum2 == z)
                {
                    String fname1 = fi.FullName;
                    Bitmap bmp3 = new Bitmap(Image.FromFile(fname1));
                    FirstMatch.Image = bmp3;
                }
                if (fileNum2 == k)
                {
                    String fname2 = fi.FullName;
                    Bitmap bmp3 = new Bitmap(Image.FromFile(fname2));
                    SecondMatch.Image = bmp3;
                }
                if (fileNum2 == l)
                {
                    String fname3 = fi.FullName;
                    Bitmap bmp3 = new Bitmap(Image.FromFile(fname3));
                    ThirdMatch.Image = bmp3;
                }
                if (fileNum2 == m)
                {
                    String fname4 = fi.FullName;
                    Bitmap bmp3 = new Bitmap(Image.FromFile(fname4));
                    FourthMatch.Image = bmp3;
                }
                fileNum2++;
            }
            textBox2.Text = "This is the First Match";
            textBox3.Text = "This is the Second Match";
            textBox4.Text = "This is the Third Match";
            textBox5.Text = "This is the Fourth Match";
            textBox6.Text = "The distance is  " + distance[0];
            textBox7.Text = "The distance is  " + distance[1];
            textBox8.Text = "The distance is  " + distance[2];
            textBox9.Text = "The distance is  " + distance[3];

            //   MessageBox.Show("The loades image matches with" + z + "image");

        }

        
    }
}
