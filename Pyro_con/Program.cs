using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Pyro_con
{
    class Program
    {
        public static Bitmap bmp;
        public static Bitmap bmp_out_temp;
        public static byte[,] arr;
        public static byte arr_max = 0;
        public static byte arr_min = 255;
        public static int x_max;
        public static int y_max;


        static void Main(string[] args)

        {
        }

        static void prg_1()
        {
            bmp = new Bitmap("01.bmp");            
            x_max = bmp.Width;
            y_max = bmp.Height;
            arr = new byte[bmp.Width, bmp.Height];
            for (int x = 0; x < x_max; x++)
                for (int y = 0; y < y_max; y++)
                {
                    arr[x, y] = bmp.GetPixel(x, y).R;
                    if (arr_max < arr[x, y])
                        arr_max = arr[x, y];
                    if (arr_min > arr[x, y])
                        arr_min = arr[x, y];
                }
            Console.WriteLine("Calc_done" + "\n");
            Console.WriteLine("размер картинки X=" + (bmp.Width / Convert.ToInt32(textBox2.Text) * Convert.ToDouble(textBox4.Text)).ToString() + "\n");

        }

        static void prg_2()
        {
            StreamWriter GWriter = new System.IO.StreamWriter("out_" + DateTime.Now.ToShortDateString().ToString() + "_" + DateTime.Now.ToShortTimeString().ToString().Replace(':', '.') + ".gcode", false);
            GWriter.AutoFlush = true;
            int count_st = 0;
            string st = "";
            //double pix_mm = 0.5;
            int feed_last = 0;
            int feed_coef = 5;//умножение
            int feed_diff = 50;//разница
            int feed_min = 200;
            int feed_max = 3000;
            int feed = feed_min;
            bool dir = true;
            GWriter.WriteLine("G21");
            for (int y = 0; y < bmp.Height; y++)
            {

                for (int x = 0; x < bmp.Width; x++)
                {
                    if (dir)
                    {
                        feed = arr[x, y] * feed_coef;
                        if (feed < feed_min) feed = feed_min;
                        if (feed > feed_max) feed = feed_max;
                        if (Math.Abs(feed - feed_last) > feed_diff)
                        {
                            count_st = 0;
                            if (st.Length > 0)
                            {
                                GWriter.WriteLine(st.Replace(',', '.'));
                                st = "";
                            }
                            GWriter.WriteLine("G91 G01 Y" + textBox4.Text.Replace(',', '.') + " F" + feed);
                        }
                        else
                        {
                            count_st++;
                            st = ("G91 G01 Y" + Math.Round(Convert.ToDouble(textBox4.Text) * count_st, 1) + " F" + feed_last);
                        }
                        feed_last = feed;
                    }
                    else
                    {
                        feed = arr[bmp.Width - x - 1, y] * feed_coef;
                        if (feed < feed_min) feed = feed_min;
                        if (feed > feed_max) feed = feed_max;
                        if (Math.Abs(feed - feed_last) > feed_diff)
                        {
                            count_st = 0;
                            if (st.Length > 0)
                            {
                                GWriter.WriteLine(st.Replace(',', '.'));
                                st = "";
                            }
                            GWriter.WriteLine("G91 G01 Y-" + textBox4.Text.Replace(',', '.') + " F" + feed);
                        }

                        else
                        {
                            count_st++;
                            st = ("G91 G01 Y-" + Math.Round(Convert.ToDouble(textBox4.Text) * count_st, 1) + " F" + feed_last);
                        }
                        feed_last = feed;
                    }


                }
                dir = !dir;
                if (count_st != 0)
                {

                    GWriter.WriteLine(st.Replace(',', '.'));
                    count_st = 0;
                    st = "";
                }
                GWriter.WriteLine("G01 X" + textBox4.Text.Replace(',', '.'));
            }
            GWriter.Close();
            Console.WriteLine("GCode_done" + "\n");

        }

        static void prg_3()
        {
            int otkl = Convert.ToInt32(textBox1.Text);//значение отклонения
            int pix_count = Convert.ToInt32(textBox2.Text);//размер точки в пикселях
            int count_it = Convert.ToInt32(textBox3.Text);//кол-во проходов

            bmp_out_temp = new Bitmap("00.bmp");
            int color_mid = arr[0, 0];//среднее значение цвета
            for (int k = 0; k < count_it; k++)

                for (int x = 0; x < bmp.Width - pix_count; x++)
                    for (int y = 0; y < bmp.Height - pix_count; y++)
                    {
                        for (int x_0 = x; x_0 < x + pix_count; x_0++)
                            for (int y_0 = y; y_0 < y + pix_count; y_0++)
                            {
                                color_mid = color_mid + arr[x_0, y_0];
                                color_mid = Convert.ToInt32(color_mid / (pix_count * pix_count));
                            }
                        if (Math.Abs(color_mid - arr[x, y]) < otkl)
                            arr[x, y] = Convert.ToByte(color_mid);

                        if (k == count_it - 1)
                        {
                            bmp_out_temp.SetPixel(x, y, Color.FromArgb(arr[x, y], arr[x, y], arr[x, y]));
                        }

                    }
            bmp_out_temp.Save("out.bmp");
            Console.WriteLine("Save_done" + "\n");

        }


    }
}
