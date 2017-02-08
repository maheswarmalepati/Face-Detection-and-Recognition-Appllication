using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalSecond_Bit
{
    class ImageProc
    {
        public static bool ConvertToGray(Bitmap b) //converts color image to gray sacle 
        {
            // GDI+ return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int stride = bmData.Stride; // bytes in a row 3*b.Width
            System.IntPtr Scan0 = bmData.Scan0;
            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                byte red, green, blue;
                int nOffset = stride - b.Width * 3;
                for (int y = 0; y < b.Height; ++y)
                { //For every pixel value do conversion formula
                    for (int x = 0; x < b.Width; ++x)
                    {
                        blue = p[0];
                        green = p[1];
                        red = p[2];
                        p[0] = p[1] = p[2] = (byte)(.299 * red + .587 * green + .114 * blue); //Conversion formula 
                        p += 3;
                    }
                    p += nOffset;
                }
            }
            b.UnlockBits(bmData);
            return true;
        }

        public static unsafe bool IsGrayScale(Image image)
        {
            using (var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(image, 0, 0);
                }

                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

                var pt = (int*)data.Scan0;
                var res = true;

                for (var i = 0; i < data.Height * data.Width; i++)
                {
                    var color = Color.FromArgb(pt[i]);

                    if (color.A != 0 && (color.R != color.G || color.G != color.B))
                    {
                        res = false;
                        break;
                    }
                }

                bmp.UnlockBits(data);

                return res;
            }
        }

        public static int[,] BitmapToIntArray(Bitmap b) //Input must be grayscale image
        {
            int xMax = b.Width;
            int yMax = b.Height;
            int[,] intArray = new int[xMax, yMax];
            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    intArray[x, y] = b.GetPixel(x, y).R; //Takes the red pixel because R=G=B values in a grayscale image
                }
            }
            return intArray;
        }

        public static Bitmap intArrayToBitmap(int[,] imgArray) //Input must be grayscale image
        {
            int xMax = imgArray.GetLength(0);
            int yMax = imgArray.GetLength(1);
            Bitmap bmp1 = new Bitmap(xMax, yMax);
            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    Color c1 = Color.FromArgb(imgArray[x, y], imgArray[x, y], imgArray[x, y]);
                    bmp1.SetPixel(x, y, c1);
                }
            }
            return bmp1;
        }

        public static bool ResizeImageProportional(Image img, ref Bitmap bm, Rectangle rect)
        {
            Rectangle newR = new Rectangle(rect.X, rect.Y, rect.Width, img.Height * rect.Width / img.Width);
            bm = new Bitmap(newR.Width, newR.Height, PixelFormat.Format24bppRgb);
            Graphics dc = Graphics.FromImage(bm);
            //dc.InterpolationMode = InterpolationMode.High;
            dc.DrawImage(img, newR);
            return true;
        }
    }
}
