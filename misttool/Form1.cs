using AutoItX3Lib;
using KAutoHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace misttool
{
    public partial class Form1 : Form
    {
        string imagePath = Environment.CurrentDirectory + @"\mist\imagestatus\";
        string searchImage = Environment.CurrentDirectory + @"\mist\imagedetect\";

        List<Thread> threadClick;
        List<Thread> threadScreen;
        List<string> reconnects = new List<string>();

        System.Timers.Timer myTimer = new System.Timers.Timer();
        int timeReload = 5;

        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(min, max);
            }
        }

        public Form1()
        {
            KAutoHelper.ADBHelper.SetADBFolderPath(@"D:\scrcpy\");
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.Columns.Add("Image", 300);
            listView1.Columns.Add("IP Address", 150);
            listView1.Columns.Add("Img Status", 100);
            listView1.Columns.Add("Date Update", 150);
            listView1.Columns.Add("Sock connect", 150);
            listView1.Columns.Add("Run", 90);
            listView1.Columns.Add("Time", 90);

            myTimer.Elapsed += new ElapsedEventHandler(ReloadData);

            //string device = "192.168.141.121:9000";
            //OpenView(device);
            //ConnectSock("192.168.141.121:9000");
            //OpenView(device);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Run all")
            {
                threadClick = new List<Thread>();
                threadScreen = new List<Thread>();

                button3.Text = "Stop all";
                var intPtrs = AutoControl.FindWindowHandlesFromProcesses("SDL_app", null, 100);

                foreach (var item in intPtrs)
                {
                    var title = AutoControl.GetText(item);
                    Thread t1 = new Thread(() => WorkAutoClick(title));
                    t1.Name = title;
                    threadClick.Add(t1);

                    Thread t2 = new Thread(() => WorkSreenShot(title));
                    t2.Name = title;
                    threadScreen.Add(t2);
                }

                myTimer.Interval = timeReload * 1000;
                myTimer.Start();

                threadScreen.ForEach(item => item.Start());
                threadClick.ForEach(item => item.Start());
            }
            else
            {
                button3.Text = "Run all";
                myTimer.Stop();
                threadScreen.ForEach(item =>
                {
                    if (item.ThreadState.ToString().Contains("Suspended") || item.ThreadState.ToString().Contains("SuspendRequested"))
                    {
                        item.Resume();
                        item.Abort();
                    }
                    else
                    {
                        item.Abort();
                    }
                });
                threadClick.ForEach(item =>
                {
                    if (item.ThreadState.ToString().Contains("Suspended") || item.ThreadState.ToString().Contains("SuspendRequested"))
                    {
                        item.Resume();
                        item.Abort();
                    }
                    else
                    {
                        item.Abort();
                    }
                });
                listView1.Items.Clear();
            }
            txtIP.Text = "";
            panel2.Enabled = false;
        }
        private void ReloadData(object sender, EventArgs e)
        {
            var listMachine = GetMachine();
            int index = 0;

            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(256, 50);

            if (listView1.InvokeRequired)
            {
                listView1.Invoke((MethodInvoker)delegate ()
                {
                    listView1.Items.Clear();

                    foreach (var m in listMachine)
                    {
                        ListViewItem item1 = new ListViewItem((m.Port).ToString(), index);
                        item1.SubItems.Add(m.IpAdrress);
                        item1.SubItems.Add(m.Status);
                        item1.SubItems.Add(m.DateUpdate.ToString("HH:mm:ss dd-MM"));
                        item1.SubItems.Add(m.SockConnect);
                        item1.SubItems.Add(m.Run);

                        listView1.Items.Add(item1);

                        if (m.Image != null)
                        {
                            imageList.Images.Add(Image.FromFile(m.Image));
                        }

                        index++;
                    }
                    listView1.SmallImageList = imageList;
                });

                label2.Invoke((MethodInvoker)delegate ()
                {
                    label2.Text = ADBHelper.GetDevices().Where(c => !c.Contains("offline")).ToList().Count.ToString();
                });
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var rs = ReloadVNC();
            MessageBox.Show("Number device open VNC: " + rs, "OK");
        }
        private int ReloadVNC()
        {
            AutoItX3 autoItX3Lib = new AutoItX3();
            int index = 0;
            int temp111 = 0;

            var ipAlready = CheckDeviceOpen();

            var numberDevice = numericUpDown1.Value;
            List<string> portAr = new List<string>();
            int tempport = 0;
            for (int index1 = 0; index1 < numberDevice; index1++)
            {
                if (index1 % 2 == 0)
                {
                    portAr.Add((5000 + tempport).ToString());
                    tempport++;
                }
            }

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            foreach (var item in portAr)
            {
                foreach (TcpConnectionInformation c in connections)
                {

                    if (item == c.LocalEndPoint.Port.ToString())
                    {
                        var match = ipAlready.FirstOrDefault(stringToCheck => stringToCheck.Equals(c.RemoteEndPoint.Address.ToString()));

                        if (match == null)
                        {
                            index++;

                            int portVNC = 0;
                            using (TcpClient tcpClient = new TcpClient())
                            {
                                try
                                {
                                    tcpClient.Connect(c.RemoteEndPoint.Address.ToString(), c.LocalEndPoint.Port + 4000 + temp111);
                                    portVNC = c.LocalEndPoint.Port + 4000 + temp111;
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        tcpClient.Connect(c.RemoteEndPoint.Address.ToString(), c.LocalEndPoint.Port + 4000 + 1 + temp111);
                                        portVNC = c.LocalEndPoint.Port + 4000 + 1 + temp111;
                                    }
                                    catch (Exception)
                                    {
                                    }

                                }
                            }

                            if (portVNC == 0)
                            {
                                MessageBox.Show("Turn on VNC for: " + c.RemoteEndPoint.Address.ToString());
                                index--;
                            }
                            else
                            {
                                var p = new Process();
                                p.StartInfo.FileName = "cmd.exe";
                                p.StartInfo.WorkingDirectory = @"D:\scrcpy\";
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.RedirectStandardInput = true;
                                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                p.Start();
                                p.StandardInput.WriteLine("scrcpy --window-title " + c.RemoteEndPoint.Address + ":" + portVNC.ToString() + " --window-x " + (portVNC - 9000) * 120 + " --window-y " + 50 + " --max-size=530  --tcpip=" + c.RemoteEndPoint.Address + ":" + portVNC.ToString());
                            }
                            ipAlready.Add(c.RemoteEndPoint.Address.ToString());
                        }

                    }
                }
                temp111++;
            }
            return index;
        }
        private List<string> CheckDeviceOpen()
        {
            List<string> ipAlready = new List<string>();

            var intPtrs = AutoControl.FindWindowHandlesFromProcesses("SDL_app", null, 100);

            List<string> removeLine = new List<string>();
            foreach (var item in intPtrs)
            {
                var title = AutoControl.GetText(item);
                ipAlready.Add(title.Split(':')[0]);
            }

            return ipAlready;
        }
        private void OpenView(string device)
        {
            var p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.WorkingDirectory = @"D:\scrcpy\";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.StandardInput.WriteLine("scrcpy --max-size=700 --window-title " + device + "  --tcpip=" + device);
        }
        private void ClearAppRecent(string device)
        {
            Thread.Sleep(1000);
            KAutoHelper.ADBHelper.Tap(device, 500, 500);
            Thread.Sleep(1000);
            KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + device + " shell " + '"' + "input keyevent KEYCODE_APP_SWITCH" + '"');
            Thread.Sleep(2000);
            KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + device + " shell " + '"' + "input keyevent 19" + '"');
            Thread.Sleep(1000);
            for (int i = 0; i < 4; i++)
            {
                KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + device + " shell " + '"' + "input keyevent DEL" + '"');
            }
            Thread.Sleep(500);
            KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + device + " shell " + '"' + "input keyevent KEYCODE_HOME" + '"');
            Thread.Sleep(500);

            KAutoHelper.ADBHelper.Tap(device, 500, 500);
        }
        private void ConnectSock(string device)
        {
            ClearAppRecent(device);
            //run sock client
            KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + device + " shell am start -n vn.proxy.client/vn.proxy.client.ui.MainActivity");
            //click center button connect
            var resPoint = GetPostionImage(device, searchImage + "connectsock.png", 2);
            var buttonConnect = resPoint;
            KAutoHelper.ADBHelper.Tap(device, resPoint.Value.X, resPoint.Value.Y, 5);

            Thread.Sleep(2000);
            //alert stopped
            resPoint = GetPostionImage(device, searchImage + "stopped.png", 2);
            if (resPoint != null)
            {
                KAutoHelper.ADBHelper.TapByPercent(device, 49.4, 70.5);
                Thread.Sleep(200);
                KAutoHelper.ADBHelper.Tap(device, buttonConnect.Value.X, buttonConnect.Value.Y);
                Thread.Sleep(2000);
            }
            //check connected
            resPoint = GetPostionImage(device, searchImage + "adsock.png", 3);
            if (resPoint == null)
            {
                //crash sock client
                ConnectSock(device);
            }
            else
            {
                ClearAppRecent(device);
            }
            reconnects.Remove(device);

            var thread = threadClick.Where(c => c.Name == device).FirstOrDefault();
            if (thread != null)
            {
                if (thread.ThreadState.ToString().Contains("Suspended") || thread.ThreadState.ToString().Contains("SuspendRequested"))
                {
                    thread.Resume();
                }
            }


            var thread1 = threadScreen.Where(c => c.Name == device).FirstOrDefault();
            if (thread1 != null)
            {
                if (thread1.ThreadState.ToString().Contains("Suspended") || thread1.ThreadState.ToString().Contains("SuspendRequested"))
                {
                    thread1.Resume();
                }
            }
        }

        public List<Machine> GetMachine()
        {
            var machine = new List<Machine>();

            var listDevice = ADBHelper.GetDevices().Where(c => !c.Contains("offline")).ToList();
            foreach (var item in listDevice)
            {
                var temp1 = item.Split(':');
                var ip = temp1[0];
                var port = temp1[1];

                var m = new Machine()
                {
                    IpAdrress = ip,
                    Port = Int32.Parse(port)
                };
                if (File.Exists(imagePath + ip.Replace(".", "-") + ".png"))
                {
                    m.Image = imagePath + ip.Replace(".", "-") + ".png";
                    m.DateUpdate = File.GetLastWriteTime(imagePath + ip.Replace(".", "-") + ".png");
                    TimeSpan ts = DateTime.Now - m.DateUpdate;
                    m.Status = ts.TotalMinutes > 5 ? "FAIL" : "OK";
                }
                else
                {
                    m.Image = Environment.CurrentDirectory + @"\mist\processimage\" + m.IpAdrress.Replace(".", "-") + ".png";
                }


                var rs = KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + item + " shell  ping -i 0 -c 1 google.com");
                if (rs.Contains("bytes of data."))
                {
                    m.SockConnect = "Connected";
                }
                else
                {
                    m.SockConnect = "Disconnected";
                    //if (reconnects.Where(c => c == item).FirstOrDefault() == null)
                    //{
                    //    reconnects.Add(item);
                    //    var thread = threadClick.Where(c => c.Name == item).FirstOrDefault();
                    //    if (thread != null)
                    //    {
                    //        thread.Suspend();
                    //    }

                    //    var thread1 = threadScreen.Where(c => c.Name == item).FirstOrDefault();
                    //    if (thread1 != null)
                    //    {
                    //        thread1.Suspend();
                    //    }

                    //    Thread t = new Thread(() => ConnectSock(item));
                    //    t.Start();
                    //}
                }

                var thread = threadClick.Where(c => c.Name == item).FirstOrDefault();
                if (thread.ThreadState == System.Threading.ThreadState.Running || thread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    TimeSpan duration = TimeSpan.ParseExact(m.Time, @"mm\:ss", CultureInfo.InvariantCulture);
                    int seconds = (int)duration.TotalSeconds;  // 23400
                    seconds += timeReload;
                    TimeSpan time = TimeSpan.FromSeconds(seconds);
                    DateTime dateTime = DateTime.Today.Add(time);
                    string displayTime = dateTime.ToString("mm:ss");

                    m.Run = "Run";
                    m.Time = displayTime;

                }
                else
                {
                    m.Run = "Stop";
                    m.Time = "00:00";
                }

                machine.Add(m);
            }


            return machine.OrderBy(q => q.Status).ToList();
        }

        private void WorkAutoClick(string title)
        {
            while (true)
            {
                try
                {
                    var temp = RandomNumber(0, 10);
                    if (temp > 5)
                    {
                        KAutoHelper.ADBHelper.TapByPercent(title, RandomNumber(13, 80), RandomNumber(13, 80));
                    }
                    else
                    {
                        KAutoHelper.ADBHelper.SwipeByPercent(title, RandomNumber(13, 80), RandomNumber(13, 80), RandomNumber(13, 80), RandomNumber(13, 80));
                    }

                    temp = RandomNumber(0, 10);
                    if (temp < 5)
                    {
                        KAutoHelper.ADBHelper.TapByPercent(title, RandomNumber(13, 80), RandomNumber(13, 80));
                    }
                    else
                    {
                        KAutoHelper.ADBHelper.SwipeByPercent(title, RandomNumber(13, 80), RandomNumber(13, 80), RandomNumber(13, 80), RandomNumber(13, 80));
                    }

                    var time = RandomNumber(1000, 7000);
                    Thread.Sleep(time);
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }

        private void WorkSreenShot(string title)
        {
            while (true)
            {
                var ip = title.Split(':')[0];
                try
                {
                    var main = KAutoHelper.ADBHelper.ScreenShoot(title);
                    main.Save(Environment.CurrentDirectory + @"\mist\processimage\" + ip.Replace(".", "-") + ".png");

                    DirectoryInfo d = new DirectoryInfo(Environment.CurrentDirectory + @"\mist\find");
                    FileInfo[] Files = d.GetFiles();
                    foreach (FileInfo file in Files)
                    {
                        var sub = ImageScanOpenCV.GetImage(file.FullName);

                        var resBitmap = ImageScanOpenCV.Find(main, sub, 0.7);

                        if (resBitmap != null)
                        {
                            var res = CaptureHelper.CropImage(resBitmap, new Rectangle(0, 40, 300, 50));
                            res.Save(imagePath + ip.Replace(".", "-") + ".png");
                            Thread.Sleep(60000);
                            break;
                        }
                    }

                    Thread.Sleep(1500);
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                var rectangle = listView1.GetItemRect(i);
                if (rectangle.Contains(e.Location))
                {
                    txtIP.Text = listView1.Items[i].SubItems[1].Text + ":" + listView1.Items[i].Text;
                    panel2.Enabled = true;
                    return;
                }
            }
        }
        private void button7_Click(object sender, EventArgs e)
        {
            var result = KAutoHelper.ADBHelper.ExecuteCMD("adb -d tcpip " + txtPort.Text);
            MessageBox.Show(result, "", MessageBoxButtons.OK);
            txtPort.Text = "";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + txtIP.Text + " shell am start -n com.mistplay.mistplay/com.mistplay.mistplay.view.activity.signUp.LaunchActivity");
            }
            else
            {
                AutoItX3 autoItX3Lib = new AutoItX3();

                var intPtrs = AutoControl.FindWindowHandlesFromProcesses("SDL_app", null, 100);

                foreach (var item in intPtrs)
                {
                    var title = AutoControl.GetText(item);
                    KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + title + " shell am start -n com.mistplay.mistplay/com.mistplay.mistplay.view.activity.signUp.LaunchActivity");
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                var win = AutoControl.FindWindowHandle(null, txtIP.Text);
                AutoItX3 autoItX3 = new AutoItX3();
                autoItX3.WinMove(txtIP.Text, null, 500, 300);
                AutoControl.BringToFront(win);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + txtIP.Text + " shell am start -n vn.proxy.client/vn.proxy.client.ui.MainActivity");
            }
            else
            {
                AutoItX3 autoItX3Lib = new AutoItX3();

                var intPtrs = AutoControl.FindWindowHandlesFromProcesses("SDL_app", null, 100);

                foreach (var item in intPtrs)
                {
                    var title = AutoControl.GetText(item);
                    KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + title + " shell am start -n vn.proxy.client/vn.proxy.client.ui.MainActivity");
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + txtIP.Text + " shell " + '"' + "input keyevent KEYCODE_APP_SWITCH" + '"');
            }
            else
            {
                AutoItX3 autoItX3Lib = new AutoItX3();

                var intPtrs = AutoControl.FindWindowHandlesFromProcesses("SDL_app", null, 100);

                foreach (var item in intPtrs)
                {
                    var title = AutoControl.GetText(item);
                    KAutoHelper.ADBHelper.ExecuteCMD("adb -s " + title + " shell " + '"' + "input keyevent KEYCODE_APP_SWITCH" + '"');
                }
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            txtIP.Text = String.Empty;
            panel2.Enabled = false;
        }

        private Point? GetPostionImage(string device, string find, int timeout)
        {
            int time = 0;
            while (timeout > time)
            {
                Thread.Sleep(1000);
                var main = KAutoHelper.ADBHelper.ScreenShoot(device);
                var sub = ImageScanOpenCV.GetImage(find);
                var resPoint = ImageScanOpenCV.FindOutPoint(main, sub, 0.8);
                var resBitmap = ImageScanOpenCV.Find(main, sub, 0.8);
                resBitmap.Save(@"D:\E\New folder\misttool\misttool\bin\Release\abc.png");
                if (resPoint != null)
                {
                    return resPoint;
                }
                time++;
            }
            return null;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                var thread = threadClick.Where(c => c.Name == txtIP.Text).FirstOrDefault();
                if (thread != null)
                {
                    if (thread.ThreadState.ToString().Contains("Suspended") || thread.ThreadState.ToString().Contains("SuspendRequested"))
                    {
                        thread.Resume();
                    }
                }


                var thread1 = threadScreen.Where(c => c.Name == txtIP.Text).FirstOrDefault();
                if (thread1 != null)
                {
                    if (thread1.ThreadState.ToString().Contains("Suspended") || thread1.ThreadState.ToString().Contains("SuspendRequested"))
                    {
                        thread1.Resume();
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtIP.Text))
            {
                var thread = threadClick.Where(c => c.Name == txtIP.Text).FirstOrDefault();
                if (thread != null)
                {
                    thread.Suspend();
                }

                var thread1 = threadScreen.Where(c => c.Name == txtIP.Text).FirstOrDefault();
                if (thread1 != null)
                {
                    thread1.Suspend();
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.ExitThread();

            Environment.Exit(Environment.ExitCode);
        }
    }
}
