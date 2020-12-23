using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.IO.Ports;
using System.IO;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;

namespace port_demo
{



    public partial class Form1 : Form
    {
        public Form1()
        {
            /*-----------------窗体初始化模块--------------------*/
            InitializeComponent();
            this.comboBox1.SelectedIndex = 4;
            this.comboBox2.SelectedIndex = 3;
            this.comboBox3.SelectedIndex = 1;
            this.comboBox4.SelectedIndex = 0;
            this.comboBox5.SelectedIndex = 0;
        }

        public string portName;
        public string cloud_connect = "user=root; database=test; port=8332; pwd=gotmNAOL6^NcKJ9$; server=115.236.52.123";
        public int baudRate;
        public int dataBits;
        public int tag = 1;
        public int sequence = 0;
        public bool isOpen = false;
        public bool isHex = false;
        public bool SqlConnection = false;
        public bool Endtransmission = false;
        public byte checkcode = 0;

        SerialPort sp = new SerialPort();
        public StopBits stopbits;
        public Parity parity;
        MySqlConnection conn;

        public static Thread read_execute;
        System.Threading.Timer threadTimer = null;
        private System.Object lockThis = new System.Object();

        /*-----------------串口配置模块--------------------*/
        //串口检测
        private void button1_Click(object sender, EventArgs e)
        {
            bool comExistence = false;
            comboBox1.Items.Clear();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    SerialPort sp = new SerialPort("COM" + (i + 1).ToString());
                    sp.Open();
                    sp.Close();
                    comboBox1.Items.Add("COM" + (i + 1).ToString());
                    comExistence = true;
                }
                catch (Exception)
                {
                    continue;
                }
            }
            if (comExistence)
            {
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("未找到可用串口");
            }
        }

        //检查串口是否设置
        private bool CheckPortSetting()
        {
            if (comboBox1.Text.ToString() == "") return false;
            if (comboBox2.Text.ToString() == "") return false;
            if (comboBox3.Text.ToString() == "") return false;
            if (comboBox4.Text.ToString() == "") return false;
            if (comboBox5.Text.ToString() == "") return false;
            return true;
        }

        //串口配置
        private void SetPortProperty()
        {
            SerialPort sp = new SerialPort();
            sp.PortName = comboBox1.Text.Trim();
            portName = sp.PortName;
            sp.BaudRate = Convert.ToInt32(comboBox2.Text.Trim());
            baudRate = sp.BaudRate;
            float f = Convert.ToSingle(comboBox3.Text.Trim()); // trim表示移除前后的空白字符
            if (f == 0)
            {
                sp.StopBits = StopBits.None;
            }
            else if (f == 1)
            {
                sp.StopBits = StopBits.One;
            }
            else if (f == 1.5)
            {
                sp.StopBits = StopBits.OnePointFive;
            }
            else if (f == 2)
            {
                sp.StopBits = StopBits.Two;
            }
            else
            {
                sp.StopBits = StopBits.One;
            }
            stopbits = sp.StopBits;
            sp.DataBits = Convert.ToInt16(comboBox5.Text.Trim());
            dataBits = sp.DataBits;

            string s = comboBox4.Text.Trim();
            if (s.CompareTo("无") == 0)
            {
                sp.Parity = Parity.None;
            }
            else if (s.CompareTo("奇校验") == 0)
            {
                sp.Parity = Parity.Odd;
            }
            else if (s.CompareTo("偶校验") == 0)
            {
                sp.Parity = Parity.Even;
            }
            else
            {
                sp.Parity = Parity.None;
            }
            parity = sp.Parity;
            sp.ReadTimeout = -1;     //  设置超时读取时间
            sp.RtsEnable = true;

            // 定义Data Received 事件 ，  当串口收到数据后触发事件
            sp.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived); // 这句话没看懂
            if (radioButton1.Checked)
            {
                isHex = false;

            }
            else
            {
                isHex = true;

            }
        }

        //打开&关闭串口
        private void button2_Click(object sender, EventArgs e)
        {
            SetPortProperty();
            //SendIP();
            SerialPort sp = new SerialPort(portName, baudRate, parity, dataBits, stopbits);
            sp.ReadTimeout = 400;
            if (isOpen == false)
            {
                try
                {
                    sp.Open();
                    isOpen = true;
                    textBox1.Text += "串口已打开";
                    textBox1.Text += "\r\n";
                    button2.Text = "关闭串口";
                    comboBox2.Enabled = false; //禁用设置 
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    sp.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show("串口被占用");
                    isOpen = false;
                }
            }
            else
            {
                sp.Close();
                isOpen = false;
                button2.Text = "打开串口";
                comboBox2.Enabled = true; //启用设置 
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }

        }

        //清空数据
        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
        }

        /*-----------------数据库前置准备模块--------------------*/
        //获取IP,移动终端用
        public static string GetIP(string HostName)
        {
            IPHostEntry myAddress = Dns.GetHostEntry(HostName);
            IPAddress[] myIPAddress = myAddress.AddressList;
            int j = myAddress.AddressList.Length;
            string IPadd = myIPAddress[j - 1].ToString();
            return (IPadd);
        }

        public string ConnectionString()
        {
            string host = "LAPTOP-17S8S2N9";
            string server = GetIP(host);
            string connstr = "user=root; database=test; port=3306; pwd=angel070711; server=" + server;
            return connstr;
        }

        //string--hex转换，串口发送数据用
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        //监测每次重新打开应用程序后的执行起始位置
        public int tag_monitor()
        {
            conn = new MySqlConnection(cloud_connect);
            conn.Open();
            string SQLstr = "Select StartPosition from robot_monitor";
            MySqlCommand commission = new MySqlCommand(SQLstr, conn);
            MySqlDataAdapter adapter = new MySqlDataAdapter(commission);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            tag = Convert.ToInt16(dt.Rows[0]["StartPosition"]);
            return tag;
        }

        /*-----------------机器人串口通信命令生成--------------------*/

        //传入十进制main函数序号，生成串口校验序列（10进制转16进制）
        public string ten2hex(int FunctionNum)
        {
            string concat;
            string convert = string.Format("{0:X}", Convert.ToInt32(FunctionNum));
            if (FunctionNum < 16) concat = "02 47 0" + convert + " 03";
            else concat = "02 47 " + convert + " 03";
            return (concat);

        }

        //异或校验用：16进制转10进制
        public int[] hex2ten(int function)
        {
            string convert_hex = ten2hex(function);
            string[] split = convert_hex.Split(' ');
            int[] check_ten = new int[split.Length];
            for (int i = 0; i < split.Length; i++) check_ten[i] = System.Int32.Parse(split[i], System.Globalization.NumberStyles.HexNumber);
            return (check_ten);
        }

        //进行XOR校验，校验码checkcode奇加偶减
        public string commandseries(int function)
        {
            int[] check = hex2ten(function);
            byte[] data = new byte[check.Length];
            for (int i = 0; i < check.Length; i++) data[i] = Convert.ToByte(check[i]);
            for (int j = 0; j < data.Length; j++) checkcode ^= data[j];
            if (checkcode % 2 != 0)
            {
                checkcode += 2;
                string convert = string.Format("{0:X}", Convert.ToInt32(checkcode));
                return (ten2hex(function) + " " + convert);
            }
            else
            {
                checkcode -= 2;
                string convert = string.Format("{0:X}", Convert.ToInt32(checkcode));
                return (ten2hex(function) + " " + convert);
            }
        }

        /*-----------------数据库通信模块--------------------*/
        //数据库连接                     
        private void button5_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection(cloud_connect);
            if (SqlConnection == false)
            {
                try
                {
                    conn.Open();
                    textBox2.Text += "数据库已连接";
                    textBox2.Text += "\r\n";
                    button5.Text = "断开数据库";
                    SqlConnection = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("连接出错" + ex.ToString());
                    SqlConnection = false;
                }

            }
            else
            {
                conn.Close();
                textBox2.Text += "数据库已断开";
                textBox2.Text += "\r\n";
                button5.Text = "连接数据库";
                SqlConnection = false;
            }
        }

        //机器人登录
        private void login()
        {
            SerialPort sp = new SerialPort(portName, baudRate, parity, dataBits, stopbits);
            sp.Open();
            //发送初始化命令
            Byte[] start = strToToHexByte("04");
            sp.Write(start, 0, start.Length);
            textBox2.Text += "04发送成功 ";
            Byte[] login = strToToHexByte("02 4C 03 4F");
            sp.Write(login, 0, login.Length);
            textBox2.Text += "login发送成功";
            sp.Close();

        }

        //数据库读取和发送
        private void SendData(object state)
        {
            lock(lockThis)
            {
                CheckForIllegalCrossThreadCalls = false;  //让线程的命令可以访问主线程form控件
                conn = new MySqlConnection(cloud_connect);
                SerialPort sp = new SerialPort(portName, baudRate, parity, dataBits, stopbits);
                Endtransmission = false;                                              

                //打开数据库与串口                                                                                               
                //conn.Open();
                //sp.ReceivedBytesThreshold = 1;
                sp.Open();

                //读取数据
                //string sql = "SELECT Motion from robot_command where Sequence=" + sequence; //可能还要取速度加速度
                string sql = "SELECT * from robot_command where robot_id='EpsonC4' and checked='0' order by id DESC limit 1";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);                

                //收发
                if (isOpen == true)
                {
                    try
                    {
                        //判断是否有新指令传入
                        if (sequence == Convert.ToInt16(dt.Rows[0]["id"]))
                        {
                            textBox2.Text += "指令尚未刷新";
                            textBox2.Text += "\r\n";
                            checkcode = 0;
                        }
                        else
                        {
                            //根据取出的main函数生成通信指令
                            int function = Convert.ToInt16(dt.Rows[0]["motion"]);
                            string command = commandseries(function);
                            //textBox2.Text += command;
                            //textBox2.Text += "\r\n"; //下面这两条测试用

                            //发送
                            textBox2.Text += command;
                            textBox2.Text += "\r\n";
                            Byte[] writeBytes = strToToHexByte(command.ToString());
                            sp.Write(writeBytes, 0, writeBytes.Length);
                            textBox2.Text += "指令发送成功";
                            textBox2.Text += "\r\n";

                            //接收
                            Byte[] ReceivedData = new Byte[sp.BytesToRead];
                            sp.Read(ReceivedData, 0, ReceivedData.Length);
                            string RecvDataText = null;
                            string s = string.Empty;
                            for (int j = 0; j < ReceivedData.Length; j++)
                            {
                                RecvDataText += (ReceivedData[j].ToString("X2") + "");
                                s += (char)ReceivedData[j]; //16进制转ASCLL码
                            }
                            textBox1.Text += RecvDataText;
                            textBox1.Text += "\r\n";
                            RecvDataText = s;

                            //留出执行时间,此处login命令不在数据库中读取
                            //if (dt.Rows[0]["Motion"].ToString() == "04" | dt.Rows[0]["Motion"].ToString() == "02 4C 03 4F") System.Threading.Thread.Sleep(20);
                            //System.Threading.Thread.Sleep(7000);

                            //发送完毕，重置校验码，更新数据库
                            checkcode = 0;
                            sequence = Convert.ToInt16(dt.Rows[0]["id"]);
                            string check = "update robot_command set checked='1' where id='" + sequence + "'";
                            MySqlCommand update = new MySqlCommand(check, conn);
                            update.ExecuteNonQuery(); 
                        }
              
                    }
                    catch (Exception e) //监视一下sequence有没有问题,修改数据库后sql语句不存在报错问题
                    {
                        //read_execute.Abort();
                        textBox2.Text += e;
                        textBox2.Text += "\r\n";
                        //System.Threading.Thread.Sleep(200);
                    }

                }
                else
                {
                    MessageBox.Show("串口未打开");
                }
                sp.Close();
            }
        }

        //停止读取时更新monitor表中的起始位置, 更新数据库后弃用，monitor监视数显器
        private void tag_update()
        {
            conn = new MySqlConnection(cloud_connect);
            conn.Open();
            string ChangeTag = "Update robot_monitor set StartPosition=" + (sequence-1) + " where line=0";
            MySqlCommand update = new MySqlCommand(ChangeTag, conn);
            update.ExecuteNonQuery(); 
        }
        
        //发送数据按钮
        private void button4_Click(object sender, EventArgs e)
        {
            //SendData();
            //tag = tag_monitor();
            //sequence = tag;
            login();
            threadTimer = new System.Threading.Timer(new System.Threading.TimerCallback(SendData), null, 100, 6500);
            //CallWithTimeout(SendData, 60000);
        }

        //手动中止数据发送
        private void button6_Click(object sender, EventArgs e)
        {
            //Endtransmission = true;
            try { threadTimer.Dispose(); }
            catch { }
            //tag_update();
            conn = new MySqlConnection(cloud_connect);
            conn.Close();
            sp = new SerialPort(portName, baudRate, parity, dataBits, stopbits);
            sp.Close();
            textBox2.Text += "发送已中止";

        }

        /*-----------------数据接收示例函数--------------------*/
        //接收数据 未引用
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(100); //延时100ms
            this.Invoke((EventHandler)(delegate
            {
                if (isHex == false)
                {
                    System.Text.UTF8Encoding utf8 = new UTF8Encoding();
                    Byte[] readBytes = new Byte[sp.BytesToRead];
                    sp.Read(readBytes, 0, readBytes.Length);
                    string decodeString = utf8.GetString(readBytes);
                    textBox1.Text += decodeString;
                }
                else
                {
                    Byte[] ReceivedData = new Byte[sp.BytesToRead];
                    sp.Read(ReceivedData, 0, ReceivedData.Length);
                    string RecvDataText = null;
                    string s = string.Empty;
                    for (int i = 0; i < ReceivedData.Length; i++)
                    {
                        RecvDataText += (ReceivedData[i].ToString("X2") + "");
                        s += (char)ReceivedData[i]; //16进制转ASCLL码
                    }
                    textBox1.Text += RecvDataText;
                    RecvDataText = s;
                }
                //sp.DiscardInBuffer(); //清空缓存
            }));
        }
    }





}
