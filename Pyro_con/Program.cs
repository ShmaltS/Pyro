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
        public static Bitmap bmp_in;
       // public static Bitmap bmp_out_temp;
        public static byte[,] arr;        
        public static int x_max;
        public static int y_max;

        public static int feed_coef;
        public static int feed_diff;
        public static int feed_min;
        public static int feed_max;
        public static int pix_in_mask;
        public static int pix_otkl;
        public static int count_rep;
        public static double line_mm;
        //public static string conf_file="default.ini";
        public static StreamWriter EWriter = new StreamWriter("Pyro_" + DateTime.Now.ToShortDateString().ToString() + "_" + DateTime.Now.ToShortTimeString().ToString().Replace(':', '.') + ".err", false);


        static void Main(string[] args)

        {
            EWriter.AutoFlush = true;
            init(args);
            prg_1();//загрузка исходного файла
            //prg_3();//оптимизация исходного файла
            prg_2();//вывод GCode

            EWriter.Close();
        }

        static bool check_conf(string st)
        {
            bool flag = false;
            if (System.IO.File.Exists(st))
                flag = true;
            return flag;
        }

        static void create_conf(string st)
        {
            StreamWriter CWriter = new StreamWriter(st, false);
            CWriter.WriteLine("feed_coef 5 коеффициент ускорения подачи");
            CWriter.WriteLine("feed_diff 50 разница ускорений подачи для сглаживания рывков");
            CWriter.WriteLine("feed_min 200 минимальная подача");
            CWriter.WriteLine("feed_max 3000 максимальная подача");
            CWriter.WriteLine("pix_in_mask 2 маска усреднения в pix");
            CWriter.WriteLine("line_mm 0,25 толщина линии в мм");
            CWriter.WriteLine("pix_otkl 50 цветовое усреднение 0-255");
            CWriter.WriteLine("count_rep 10 кол-во проходов на усреднение");
            CWriter.Flush();
            CWriter.Close();
        }

        static void init(string[] st_init)
        {
            if (check_conf(st_init[1]))
            {
                StreamReader FReader = new StreamReader(st_init[1]);
                string st;
                string[] st1;
                while (FReader.EndOfStream != true)
                {
                    try
                    {
                        st = FReader.ReadLine();
                        st1 = st.Split(' ');
                        switch (st1[0])
                        {
                            case "feed_coef":
                                feed_coef = Convert.ToInt32(st1[1]);
                                break;
                            case "feed_diff":
                                feed_diff = Convert.ToInt32(st1[1]);
                                break;
                            case "feed_min":
                                feed_min = Convert.ToInt32(st1[1]);
                                break;
                            case "feed_max":
                                feed_max = Convert.ToInt32(st1[1]);
                                break;
                            case "pix_in_mask":
                                pix_in_mask = Convert.ToInt32(st1[1]);
                                break;
                            case "line_mm":
                                line_mm = Convert.ToDouble(st1[1]);
                                break;
                            case "pix_otkl":
                                pix_otkl = Convert.ToInt32(st1[1]);
                                break;
                            case "count_rep":
                                count_rep = Convert.ToInt32(st1[1]);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        EWriter.WriteLine(DateTime.Now.ToString() + " BP 3 ");
                        EWriter.WriteLine("{0} Exception caught.", e);
                    }
                }

                FReader.Close();
                bmp_in = new Bitmap(@st_init[0]);
                Console.WriteLine("Configuration loaded");
            }
            else
            {
                Console.WriteLine("Conf file error");
                create_conf(st_init[1]);
            }

            

        }

        static void prg_1()
        {
            
            byte arr_max = 0;
            byte arr_min = 255;
            x_max = bmp_in.Width;
            y_max = bmp_in.Height;
            arr = new byte[x_max, y_max];
            for (int x = 0; x < x_max; x++)
                for (int y = 0; y < y_max; y++)
                {
                    arr[x, y] = bmp_in.GetPixel(x, y).R;
                    if (arr_max < arr[x, y])
                        arr_max = arr[x, y];
                    if (arr_min > arr[x, y])
                        arr_min = arr[x, y];
                }
            Console.WriteLine("Calc_done");
            Console.WriteLine("Размер по X=" + (x_max * line_mm)+" мм");
            Console.WriteLine("Размер по Y=" + (y_max * line_mm) + " мм");

        }

        static void prg_2()
        {
            StreamWriter GWriter = new StreamWriter("out_" + DateTime.Now.ToShortDateString().ToString() + "_" + DateTime.Now.ToShortTimeString().ToString().Replace(':', '.') + ".gcode", false);
            GWriter.AutoFlush = true;
            bool dir = true;
            int feed_last = feed_min;
            int count_st = 0;
            int feed = feed_min;
            GWriter.WriteLine("G21");

            for (int y = 0; y < y_max; y++)
            {
                for (int x = 0; x < x_max; x++)
                {
                    if (dir)
                    {
                        feed=get_feed(arr[x, y]);
                        if (Math.Abs(feed - feed_last) > feed_diff)
                        {
                            if (count_st > 0)
                            {
                                GWriter.WriteLine("G91 G01 Y" + Math.Round(line_mm * count_st, 2).ToString().Replace(',', '.') + " F" + feed_last);
                                count_st = 0;
                            }
                            count_st++;                            
                        }
                        else
                        {
                            count_st++;
                        }
                        feed_last = feed;
                    }
                    else
                    {
                        feed = get_feed(arr[x_max - x - 1, y]);
                        if (Math.Abs(feed - feed_last) > feed_diff)
                        {
                            if (count_st > 0)
                            {
                                GWriter.WriteLine("G91 G01 Y-" + Math.Round(line_mm * count_st, 2).ToString().Replace(',', '.') + " F" + feed_last);
                                count_st = 0;
                            }
                            count_st++;
                        }
                        else
                        {
                            count_st++;
                        }
                        feed_last = feed;
                    }
                }

                if (count_st != 0)
                {
                    if (dir)
                    {
                        GWriter.WriteLine("G91 G01 Y" + Math.Round(line_mm * count_st, 2).ToString().Replace(',', '.') + " F" + feed_last);
                        GWriter.WriteLine("G91 G01 Y20  F2000");
                        GWriter.WriteLine("G91 G01 X" + line_mm.ToString().Replace(',', '.'));
                        GWriter.WriteLine("G91 G01 Y-20  F2000");
                    }
                    else
                    {
                        GWriter.WriteLine("G91 G01 Y-" + Math.Round(line_mm * count_st, 2).ToString().Replace(',', '.') + " F" + feed_last);
                        GWriter.WriteLine("G91 G01 X" + line_mm.ToString().Replace(',', '.'));
                    }
                    count_st = 0;
                }
                dir = !dir;
            }
            GWriter.Close();
            Console.WriteLine("GCode_done");
        }

        static void prg_3()
        {            
            //bmp_out_temp = new Bitmap("00.bmp");
            Bitmap bmp_out_temp = new Bitmap(x_max,y_max,PixelFormat.Format32bppArgb);
            int color_mid = arr[0, 0];//среднее значение цвета
            for (int k = 0; k < count_rep; k++)

                for (int x = 0; x < x_max - pix_in_mask; x++)
                    for (int y = 0; y < y_max - pix_in_mask; y++)
                    {
                        for (int x_0 = x; x_0 < x + pix_in_mask; x_0++)
                            for (int y_0 = y; y_0 < y + pix_in_mask; y_0++)
                            {
                                color_mid = color_mid + arr[x_0, y_0];
                                color_mid = Convert.ToInt32(color_mid / (pix_in_mask * pix_in_mask));
                            }
                        if (Math.Abs(color_mid - arr[x, y]) < pix_otkl)
                            arr[x, y] = Convert.ToByte(color_mid);

                        if (k == count_rep - 1)
                        {
                            bmp_out_temp.SetPixel(x, y, Color.FromArgb(arr[x, y], arr[x, y], arr[x, y]));
                        }

                    }
            bmp_out_temp.Save("out.bmp");
            Console.WriteLine("Save out image done");

        }

        static int get_feed(int cur_feed)
        {
            int out_feed = 0;
            if (0 <= cur_feed & cur_feed <= 16)
                out_feed = 300;
            if (16 < cur_feed & cur_feed <= 32)
                out_feed = 400;
            if (32 < cur_feed & cur_feed <= 48)
                out_feed = 500;
            if (48 < cur_feed & cur_feed <= 64)
                out_feed = 600;
            if (64 < cur_feed & cur_feed <= 80)
                out_feed = 700;
            if (80 < cur_feed & cur_feed <= 96)
                out_feed = 800;
            if (96 < cur_feed & cur_feed <= 112)
                out_feed = 900;
            if (112 < cur_feed & cur_feed <= 128)
                out_feed = 1000;
            if (128 < cur_feed & cur_feed <= 144)
                out_feed = 1100;
            if (144 < cur_feed & cur_feed <= 160)
                out_feed = 1200;
            if (160 < cur_feed & cur_feed <= 176)
                out_feed = 1300;
            if (176 < cur_feed & cur_feed <= 192)
                out_feed = 1400;
            if (192 < cur_feed & cur_feed <= 208)
                out_feed = 1500;
            if (208 < cur_feed & cur_feed <= 224)
                out_feed = 1600;
            if (224 < cur_feed & cur_feed <= 240)

                out_feed = 1700;
            if (240 < cur_feed & cur_feed <= 256)
                
                out_feed = 1800;

            return out_feed;
        }


    }
}
