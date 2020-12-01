using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using System.Threading;
//Giao tiep cong COM
using System.IO;
using System.IO.Ports;
using System.Xml;
namespace CamBienMau
{
	public partial class Form1 : Form
	{
		string inputData = String.Empty;
		int iSTT = 1; //STT trong dataGridView
        
		delegate void SetTextCallback(string text); //Khai báo delegate SetTextCallback với tham số string
		public Form1()
		{
			InitializeComponent();
			serialPort1.DataReceived += new SerialDataReceivedEventHandler(DataReceive);
		}

		private void btKetNoi_Click(object sender, EventArgs e)
		{
			try
			{
				serialPort1.PortName = cbCOM.Text;
				serialPort1.BaudRate = Convert.ToInt32(cbSerialPort.Text);
				serialPort1.Open();
			}
			catch (Exception Ex)
			{
				MessageBox.Show("Không kết nối được với cổng COM \nChọn cổng COM khác");
			}
		}

		private void btNgatKetNoi_Click(object sender, EventArgs e)
		{

			if (lbTrangThai.Text == "Kết nối")
			{
				DialogResult result = MessageBox.Show("Ngắt kết nối", "", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if (result == DialogResult.OK)
				{
					serialPort1.Close();
				}
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			//// Cập nhật cổng COM
			cbCOM.DataSource = SerialPort.GetPortNames();
			int count = cbCOM.Items.Count;
			if (count == 1) cbCOM.SelectedIndex = 0;
			else if (count == 2) cbCOM.SelectedIndex = 1;
			else if (count == 3) cbCOM.SelectedIndex = 2;

			string[] baundrate = { "1200", "2400", "4800", "9600", "19200", "38400" };
			cbSerialPort.Items.AddRange(baundrate);
			cbSerialPort.SelectedIndex = 3;

            /// Cập nhật đồ thị
            GraphPane myPane = zedGraphControl1.GraphPane;
            myPane.Title.Text = "ĐỒ THỊ THÔNG SỐ MÀU CẢM BIẾN THU ĐƯỢC";
            myPane.XAxis.Title.Text = "Lần đo";//tên trục hoành
            myPane.YAxis.Title.Text = "Chỉ số";//tên trục tung
            RollingPointPairList list1 = new RollingPointPairList(2000);//số điểm có thể lưu trong đồ thị này
            RollingPointPairList list2 = new RollingPointPairList(2000);
            RollingPointPairList list3 = new RollingPointPairList(2000);

            LineItem curve1 = myPane.AddCurve("Red", list1, Color.Red, SymbolType.None);//đường để vẽ đồ thị
            LineItem curve2 = myPane.AddCurve("Green", list2, Color.Green, SymbolType.None);
            LineItem curve3 = myPane.AddCurve("Blue", list3, Color.Blue, SymbolType.None);

            myPane.XAxis.Scale.Min = 1;  // Setup trục X
            myPane.XAxis.Scale.Max = 100; 
            myPane.XAxis.Scale.MinorStep = 2;  
            myPane.XAxis.Scale.MajorStep = 10;

            myPane.YAxis.Scale.Min = 0;  // Setup trục Y
            myPane.YAxis.Scale.Max = 300; 
            myPane.YAxis.Scale.MinorStep = 5;  
            myPane.YAxis.Scale.MajorStep = 20; 
            //scale the axes
            zedGraphControl1.AxisChange();

        }
        int tick=1;
		private void timer1_Tick(object sender, EventArgs e)
		{
			if (serialPort1.IsOpen)
			{
				lbTrangThai.Text = "Kết nối";
				lbTrangThai.ForeColor = Color.Lime;
				btKetNoi.Enabled = false;
				btNgatKetNoi.Enabled = true;
			}
			else
			{
				lbTrangThai.Text = "Ngắt kết nối";
				lbTrangThai.ForeColor = Color.Red;
				btNgatKetNoi.Enabled = false;
				btKetNoi.Enabled = true;
			}
			int divTick = tick % 2;
			if (divTick == 1)
			{
				lbGiang.ForeColor = Color.Red;
				lbNam.ForeColor = Color.Red;
			}
			else
			{
				lbGiang.ForeColor = Color.Blue;
				lbNam.ForeColor = Color.Blue;
			}

            if (tick > 100) tick = 0;
			else tick++;

			// Check nếu DataGridView đủ dữ liệu sẽ tính TB
			TinhTB(); 
		}
		//Thread mới để nhận dữ liệu
		private void DataReceive(object obj, SerialDataReceivedEventArgs e)
		{
            inputData = serialPort1.ReadLine();
            if (!btNhanKQ.Enabled && iSTT <= 100)
            {
                string[] inputDataSplit;
                string inputDataTrim;
                inputDataTrim = inputData.Trim();//xóa đầu đuôi
                inputDataSplit = inputDataTrim.Split(' '); //chia thành 3 chuỗi nhỏ

                //Kiểm tra dữ liệu để đưa lên txb
                if (inputDataSplit.Length == 3)
                {
                    if (inputDataSplit[0].StartsWith("Red="))
                    {
                        string inputR = inputDataSplit[0].Replace("Red=", "");
                        int inputRint;
                        if (int.TryParse(inputR, out inputRint))
                        {
                            SetTxbR(inputR);
                        }
                    }
                    if (inputDataSplit[1].StartsWith("Green="))
                    {
                        string inputG = inputDataSplit[1].Replace("Green=", "");
                        int inputBint;
                        if (int.TryParse(inputG, out inputBint))
                        {
                            SetTxbG(inputG);
                        }
                    }
                    if (inputDataSplit[2].StartsWith("Blue="))
                    {
                        string inputB = inputDataSplit[2].Replace("Blue=", "");
                        int inputGint;
                        if (int.TryParse(inputB, out inputGint))
                        {
                            SetTxbB(inputB);  //Tại đây sẽ load cả dữ liệu ra DataGridView và lên đồ thị
                        }
                    }

                    //Thêm dữ liệu lên DataGridView và đồ thị
                    Invoke(new Action(() =>
                    {
                        AddDataGridView();
                        AddZedGraph();
                    }));

                    iSTT++;
                }
            }
		}
		private void SetTxbR(string text)
		{
			if (this.txbR.InvokeRequired)
			{
				SetTextCallback method = new SetTextCallback(SetTxbR); // khởi tạo một delegate mới gọi tới SetTxbR
				this.Invoke(method, new object[] { text });
			}
			else
			{
				this.txbR.Text = text;
			}
		}
		private void SetTxbG(string text)
		{
			if (this.txbG.InvokeRequired)
			{
				SetTextCallback method = new SetTextCallback(SetTxbG); // khởi tạo một delegate mới gọi tới SetTxbR
				this.Invoke(method, new object[] { text });
			}
			else
			{
				this.txbG.Text = text;
			}
		}
		private void SetTxbB(string text)
		{
			if (this.txbB.InvokeRequired)
			{
				SetTextCallback method = new SetTextCallback(SetTxbB); // khởi tạo một delegate mới gọi tới SetTxbR
				this.Invoke(method, new object[] { text });
			}
			else
			{
				this.txbB.Text = text;
            }
        }

        //Hiển thị dữ liệu lên DataGridView
        private void AddDataGridView()
        {
            string[] dulieu = new string[]
            {
                iSTT.ToString(),
                this.txbR.Text,
                this.txbG.Text,
                this.txbB.Text
            };
            this.dataGridView1.Rows.Add(dulieu);
        }

        private void AddZedGraph()
        {
            //Đưa dữ liệu lên đồ thị
            if (this.zedGraphControl1.GraphPane.CurveList.Count <= 0)
                return; // Kiểm tra việc khởi tạo các đường curve

            LineItem curveR = this.zedGraphControl1.GraphPane.CurveList[0] as LineItem;
            LineItem curveG = this.zedGraphControl1.GraphPane.CurveList[1] as LineItem;
            LineItem curveB = this.zedGraphControl1.GraphPane.CurveList[2] as LineItem;
            if (curveR == null)  //kiểm tra 
                return;
            if (curveG == null)
                return;
            if (curveB == null)
                return;
            IPointListEdit listR = curveR.Points as IPointListEdit;
            IPointListEdit listG = curveG.Points as IPointListEdit;
            IPointListEdit listB = curveB.Points as IPointListEdit;
            if (listR == null)
                return;
            if (listG == null)
                return;
            if (listB == null)
                return;
            int pointR = int.Parse(this.txbR.Text.ToString());
            int pointG = int.Parse(this.txbG.Text.ToString());
            int pointB = int.Parse(this.txbB.Text.ToString());
            listR.Add(iSTT, pointR);// Đây chính là hàm hiển thị dữ liệu lên đồ thị
            listG.Add(iSTT, pointG);
            listB.Add(iSTT, pointB);

            this.zedGraphControl1.Refresh();
        }

        int TbRed;
		int TbGreen;
		int TbBlue;
		private void TinhTB()
        {
			if(iSTT == 101)
            {
				int sumR = 0;
				int sumG = 0;
				int sumB = 0;
				for (int i=0; i<100; i++)
                {
					sumR += int.Parse(dataGridView1[1, i].Value.ToString());
					sumG += int.Parse(dataGridView1[2, i].Value.ToString());
					sumB += int.Parse(dataGridView1[3, i].Value.ToString());
				}
				TbRed = sumR / 100;
				TbGreen = sumG / 100;
				TbBlue = sumB / 100;
				string[] dulieu = new string[]
					{
					"TB",
					TbRed.ToString(),
					TbGreen.ToString(),
					TbBlue.ToString(),
					};
				this.dataGridView1.Rows.Add(dulieu);  //Thêm hàng trung bình trên DataGridView

				iSTT = 1; //reset 
                txbR.Text = TbRed.ToString();  //Hiển thị cả giá trị trung bình lên textbox
                txbG.Text = TbGreen.ToString();
                txbB.Text = TbBlue.ToString();
                btMau.BackColor = Color.FromArgb(TbRed, TbGreen, TbBlue); //Hiển thị màu ra ô
				btNhanKQ.Enabled = true;
			}
		}

        private void btNhanKQ_Click(object sender, EventArgs e)
        {

			if(lbTrangThai.Text == "Kết nối")
            {
				//Làm mới DataGridView
				int height = dataGridView1.RowCount;
				for (int i =0; i<= height - 2; i++)
                {
					dataGridView1.Rows.Remove(dataGridView1.Rows[0]);
                }

                //Làm mới Đồ thị
                zedGraphControl1.GraphPane.CurveList[0].Clear();
                zedGraphControl1.GraphPane.CurveList[1].Clear();
                zedGraphControl1.GraphPane.CurveList[2].Clear();
                btNhanKQ.Enabled = false;
			}				
        }

        private void btXuatExcel_Click(object sender, EventArgs e)
        {

        }
    }
}
