using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Net;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Configuration;
using System.Windows.Interop; 

namespace TimeAndWeather
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WS_EX_APPWINDOW = 0x40000;
        private const int WS_EX_TOOLWINDOW = 0x80;


        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        const int WS_EX_APPWINDOW = 0x40000;
        //        const int WS_EX_TOOLWINDOW = 0x80;
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在TaskBar
        //        cp.ExStyle |= WS_EX_TOOLWINDOW;      // 不显示在Alt-Tab
        //        return cp;
        //    }
        //}


        public MainWindow()
        {
            
            InitializeComponent();
            InitialTray();

            ShowInTaskbar = false;//不在任务栏显示

            //SetWindowLong(this, WS_EX_TOOLWINDOW);
            this.Focusable = false;
            //this.IsEnabled = false;//可以固定位置
            this.IsTabStop = false;
            //this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;
            this.Topmost = false;
            //this.WindowState = System.Windows.WindowState.Minimized;


            


            ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
           {
               while (true)
               {
                   DateTime dt = DateTime.Now;
                   if (dt.Second == 0)
                   {
                       UpDateTime(dt);
                       Thread.Sleep((60) * 1000);
                   }
                   else
                   {
                       UpDateTime(dt);
                       Thread.Sleep(1000);
                   }
               }
           }), null);

            
            ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
           {
               while (true)
               {
                   UpWeather();
                   Thread.Sleep((60) * 1000 * 5);
               }
           }), null);

            //label4.Content = GetWeather();

            double Top = Convert.ToDouble(ConfigurationManager.AppSettings["Top"]);
            double Left = Convert.ToDouble(ConfigurationManager.AppSettings["Left"]);
            WinPosition(Top, Left);
            //this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);
        }

        //void MainWindow_SourceInitialized(object sender, EventArgs e)
        //{
        //    //base.OnSourceInitialized(e);

        //    this.win_SourceInitialized(this, e);
        //}

        //void win_SourceInitialized(object sender, EventArgs e)
        //{

        //    HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        //    if (hwndSource != null)
        //    {
        //        hwndSource.AddHook(new HwndSourceHook(WndProc));
        //    }

        //}

        //protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    //switch (msg)
        //    //{
        //    //    case 132:

        //    //        //你的代码

        //    //        break;
        //    //}
        //    //this.WindowState = System.Windows.WindowState.Normal;
        //    //Console.WriteLine("窗口状态：" + msg);
        //    return IntPtr.Zero;
        //}

        /// <summary>
        ///  设置窗体在屏幕的位置
        /// </summary>
        /// <param name="t">上边距</param>
        /// <param name="l">左边距</param>
        public void WinPosition(double t,double l)
        {
            this.Top = t;
            this.Left = l;
        }

        /// <summary>
        ///  保存窗体在屏幕的位置
        /// </summary>
        /// <param name="t">上边距</param>
        /// <param name="l">左边距</param>
        public void SaveWinPosition()
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings["Top"].Value = this.Top.ToString();
            cfa.AppSettings.Settings["Left"].Value = this.Left.ToString();
            cfa.Save();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }


        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private void InitialTray()
        {

            //设置托盘的各个属性
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            //notifyIcon.BalloonTipText = "程序开始运行";
            notifyIcon.Text = "托盘图标";
            notifyIcon.Icon = MyResource.tq;//new System.Drawing.Icon();
            notifyIcon.Visible = true;
            //notifyIcon.ShowBalloonTip(2000);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);



            System.Windows.Forms.MenuItem up = new System.Windows.Forms.MenuItem("窗口未置顶");
            up.Click += new EventHandler(up_Click);
            string temp = "未开机自启";
            if (GetStart())
            {
                temp = "已开机自启";
                string path = System.Windows.Forms.Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.SetValue("TimeAndWeather", path);
                rk2.Close();
                rk.Close();
            }
            else
            {
                temp = "未开机自启";
            }
            System.Windows.Forms.MenuItem start = new System.Windows.Forms.MenuItem(temp);
            start.Click += new EventHandler(start_Click);
            //退出菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            exit.Click += new EventHandler(exit_Click);

            //关联托盘控件
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { up,start, exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            //窗体状态改变时候触发
            this.StateChanged += new EventHandler(SysTray_StateChanged);
        }

        void start_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem start = sender as System.Windows.Forms.MenuItem;
            if (start.Text.Equals("未开机自启"))
            {
                start.Text = "已开机自启";
                //MessageBox.Show("设置开机自启动，需要修改注册表", "提示");
                string path = System.Windows.Forms.Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.SetValue("TimeAndWeather", path);
                rk2.Close();
                rk.Close();
            }
            else
            {
                start.Text = "未开机自启";
                //MessageBox.Show("取消开机自启动，需要修改注册表", "提示");
                string path = System.Windows.Forms.Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.DeleteValue("TimeAndWeather", false);
                rk2.Close();
                rk.Close();
            }
        }


        private bool GetStart()
        {
            ///定义注册表子Path
            string strRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            ///创建两个RegistryKey类，一个将指向Root Path，另一个将指向子Path
            RegistryKey regRootKey;
            RegistryKey regSubKey;
            ///定义Root指向注册表HKEY_LOCAL_MACHINE节点
            regRootKey = Registry.LocalMachine;
            ///Registry枚举类提供了以下几种
            /*
            Registry.ClassesRoot-------------->指向注册表HKEY_CLASSES_ROOT节点
            Registry.CurrentConfig-------------->指向注册表HKEY_CURRENT_CONFIG节点
            Registry.CurrentUser-------------->指向注册表HKEY_CURRENT_USER节点
            Registry.DynData-------------->指向注册表HKEY_DYN_DATA节点(动态注册表数据)
            Registry.LocalMachine-------------->指向注册表HKEY_LOCAL_MACHINE节点
            Registry.PerformanceData-------------->指向注册表HKEY_PERFORMANCE_DATA节点
            Registry.Users-------------->指向注册表HKEY_USERS节点
            */
            regSubKey = regRootKey.OpenSubKey(strRegPath);
            object strDSNList = regSubKey.GetValue("TimeAndWeather",false);

            ///关闭
            regSubKey.Close();
            regRootKey.Close();
            string str = strDSNList.ToString();
            if (str == "False")
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        void up_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MenuItem up = sender as System.Windows.Forms.MenuItem;
            if (up.Text.Equals("窗口已置顶"))
            {
                up.Text = "窗口未置顶";
                this.Topmost = false;
            }
            else
            {
                up.Text = "窗口已置顶";
                this.Topmost = true;
            }
        }

        /// <summary>
        /// 窗体状态改变时候触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SysTray_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 退出选项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exit_Click(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("确定要关闭吗?",
                                               "退出",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 鼠标单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
        }

        /// <summary>
        /// 更新日期时间
        /// </summary>
        /// <param name="dt3"></param>
        private void UpDateTime(DateTime dt3)
        {
            DateTime dt = DateTime.Now;
            Action action1 = () =>
            {
                //label1.Content = dt.Hour.ToString().PadLeft(2, '0') + ":" + dt.Minute.ToString().PadLeft(2, '0') + ":" + dt.Second.ToString().PadLeft(2, '0');
                label1.Content = dt.Hour.ToString().PadLeft(2, '0') + ":" + dt.Minute.ToString().PadLeft(2, '0');
            };
            label1.Dispatcher.BeginInvoke(action1);
            Action action2 = () =>
            {

                label2.Content = dt.ToString("R").Substring(8, 3) + "." + dt.Day.ToString().PadLeft(2, '0');
            };
            label2.Dispatcher.BeginInvoke(action2);

            Action action3 = () =>
            {
                label3.Content = GetWeek(dt.DayOfWeek.ToString());
            };
            label3.Dispatcher.BeginInvoke(action3);
        }


        /// <summary>
        /// 更新天气
        /// </summary>
        /// <param name="dt3"></param>
        private void UpWeather()
        {
            try
            {
                GetWeatherAPP();
                string weather = GetWeather();
                int start = weather.IndexOf("cityname=\"") + 10;
                int end = weather.IndexOf("\"", start);
                string cityname = weather.Substring(start, end - start);
                start = weather.IndexOf("temNow=\"") + 8;
                end = weather.IndexOf("\"", start);
                string temNow = weather.Substring(start, end - start) + "℃";
                start = weather.IndexOf("stateDetailed=\"") + 15;
                end = weather.IndexOf("\"", start);
                string stateDetailed = weather.Substring(start, end - start);
                Action action4 = () =>
                {
                    label4.Content = cityname;

                };
                label4.Dispatcher.BeginInvoke(action4);
                Action action5 = () =>
                {
                    label5.Content = temNow;
                };
                label5.Dispatcher.BeginInvoke(action5);
                Action action6 = () =>
                {
                    label6.Content = stateDetailed;
                };
                label6.Dispatcher.BeginInvoke(action6);
                SetWeatherAPP();
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(e.Message);
            }
        }


        private void GetWeatherAPP()
        {
            string cityname = ConfigurationManager.AppSettings["cityname"];
            string temNow = ConfigurationManager.AppSettings["temNow"];
            string stateDetailed = ConfigurationManager.AppSettings["stateDetailed"];
            Action action4 = () =>
            {
                label4.Content = cityname;

            };
            label4.Dispatcher.BeginInvoke(action4);
            Action action5 = () =>
            {
                label5.Content = temNow;
            };
            label5.Dispatcher.BeginInvoke(action5);
            Action action6 = () =>
            {
                label6.Content = stateDetailed;
            };
            label6.Dispatcher.BeginInvoke(action6);
        }

        private void SetWeatherAPP()
        {
            string cityname = "广州";
            string temNow = "25";
            string stateDetailed = "晴";
            Action action4 = () =>
            {
                cityname = label4.Content.ToString();

            };
            label4.Dispatcher.BeginInvoke(action4);
            Action action5 = () =>
            {
                temNow = label5.Content.ToString();
            };
            label5.Dispatcher.BeginInvoke(action5);
            Action action6 = () =>
            {
                stateDetailed = label6.Content.ToString();
            };
            label6.Dispatcher.BeginInvoke(action6);
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings["cityname"].Value = cityname;
            cfa.AppSettings.Settings["temNow"].Value = temNow;
            cfa.AppSettings.Settings["stateDetailed"].Value = stateDetailed;
            cfa.Save();
        }


        private string GetWeek(string week)
        {
            switch (week)
            {
                case "Monday":
                    week = "星期一";
                    break;
                case "Tuesday":
                    week = "星期二";
                    break;
                case "Wednesday":
                    week = "星期三";
                    break;
                case "Thursday":
                    week = "星期四";
                    break;
                case "Friday":
                    week = "星期五";
                    break;
                case "Saturday":
                    week = "星期六";
                    break;
                case "Sunday":
                    week = "星期日";
                    break;
            }
            return week;
        }

        /// <summary>
        /// 通过网站获取网络时间
        /// </summary>
        /// <param name="webUrl">网址</param>
        /// <returns>日期</returns>
        private static string GetWebsiteDatetime(string webUrl)
        {
            string datetime = "";
            try
            {
                WebRequest request = WebRequest.Create(webUrl);
                request.Timeout = 3000;
                request.Credentials = CredentialCache.DefaultCredentials;
                WebResponse response = (WebResponse)request.GetResponse();
                WebHeaderCollection headerCollection = response.Headers;
                foreach (var h in headerCollection.AllKeys)
                {
                    if (h == "Date")
                    {
                        datetime = headerCollection[h];
                        /*
                         * Mon, 05 Feb 2018 02:34:52 GMT  
                         * 
                         */
                        //DateTimeFormatInfo dtFormat = new System.Globalization.DateTimeFormatInfo();

                        //dtFormat.ShortDatePattern = "R";
                        //datetime = Convert.ToDateTime(date, dtFormat);
                    }
                }

                return datetime;
            }
            catch (ExecutionEngineException e)
            {
                Console.WriteLine(e.Message);
                return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "";
            }

        }

        private static DateTime GetWebDateTime()
        {
            DateTime datetime = new DateTime(1997, 1, 1);
            Dictionary<string, string> webUrl = new Dictionary<string, string>();
            webUrl.Add("中国科学院国家授时中心", "http://www.ntsc.ac.cn");//中国科学院国家授时中心
            webUrl.Add("bjTime", "http://www.bjtime.cn");//bjTime
            webUrl.Add("百度", "http://www.baidu.com");//百度
            webUrl.Add("淘宝", "http://www.taobao.com");//淘宝
            webUrl.Add("360", "http://www.360.cn");//360
            webUrl.Add("beijing-time", "http://www.beijing-time.org");//beijing-time
            foreach (var item in webUrl)
            {
                string date = GetWebsiteDatetime(item.Value);
                if (date == "" || date == null) continue;
                Console.WriteLine(date + " [" + item.Key + "]");
                DateTimeFormatInfo dtFormat = new System.Globalization.DateTimeFormatInfo();
                dtFormat.ShortDatePattern = "R";
                datetime = Convert.ToDateTime(date, dtFormat);

            }
            if (datetime.Year == 1997) datetime = DateTime.Now;

            return datetime;
        }


        /// <summary>
        /// 获取天气市
        /// </summary>
        /// <returns></returns>
        private string GetWeather2(string url)
        {
            string tempip = "";
           

            try
            {
                string adr = GetExternalAdr();
                string quName = adr.Substring(2, 2);
                WebRequest wr = WebRequest.Create(url);
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.UTF8);
                string all = sr.ReadToEnd(); //读取网站的数据

                int start = all.IndexOf("cityname=\"" + quName + "\"");
                int end = all.IndexOf("/>", start);
                tempip = all.Substring(start, end - start);
                sr.Close();
                s.Close();
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(e.Message);
            }
            return tempip;
        }


        /// <summary>
        /// 获取天气省
        /// </summary>
        /// <returns></returns>
        private string GetWeather()
        {
            string tempip = "";
            

            try
            {
                string adr = GetExternalAdr();
                string quName = adr.Substring(0, 2);
                WebRequest wr = WebRequest.Create("http://flash.weather.com.cn/wmaps/xml/china.xml");
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.UTF8);
                string all = sr.ReadToEnd(); //读取网站的数据

                int start = all.IndexOf("quName=\"" + quName + "\" pyName=\"") + 18 + quName.Length;
                int end = all.IndexOf("\" cityname=\"", start);
                tempip = all.Substring(start, end - start);
                tempip = GetWeather2("http://flash.weather.com.cn/wmaps/xml/" + tempip + ".xml");

                sr.Close();
                s.Close();
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(e.Message);
            }
            return tempip;
        }


       /// <summary>
       /// 获取地址
       /// </summary>
       /// <returns></returns>
        private string GetExternalAdr()
        {
            string tempip = "";
            try
            {
                //WebRequest wr = WebRequest.Create("http://pv.sohu.com/cityjson?ie=utf-8");
                WebRequest wr = WebRequest.Create("https://www.ipip.net/ip.html");
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.UTF8);
                string all = sr.ReadToEnd(); //读取网站的数据
                //string all = GetWebRequest("https://ip.cn/");
                int start = all.IndexOf("<span id=\"myself\">\r\n") + 20;
                int end = all.IndexOf("</span>", start);
                tempip = all.Substring(start, end - start);
                tempip = tempip.Trim();
                tempip = tempip.Substring(2, 4);
                sr.Close();
                s.Close();
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(e.Message);
            }
            return tempip;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SaveWinPosition();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetWebRequest(string url)
        {
            string strHTML = "";
            try
            {
                Uri uri = new Uri(url);
                WebRequest myReq = WebRequest.Create(uri);
                WebResponse result = myReq.GetResponse();
                Stream receviceStream = result.GetResponseStream();
                StreamReader readerOfStream = new StreamReader(receviceStream, System.Text.Encoding.GetEncoding("utf-8"));
                strHTML = readerOfStream.ReadToEnd();
                readerOfStream.Close();
                receviceStream.Close();
                result.Close();
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(e.Message);
            }
            return strHTML;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (!this.IsVisible)
            //{
            //    this.Visibility = System.Windows.Visibility.Visible;
            //}
        }

        //protected override void WndProc(ref System.Windows.Forms.Message msg)
        //{
        //    Console.WriteLine(msg.ToString());
        //}

      
        


    }

   
}
