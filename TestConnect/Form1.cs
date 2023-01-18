using System.Net.Http;
using System.Net.Sockets;

namespace TestConnect
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void pbConnect_Click(object sender, EventArgs e)
		{
			label1.Text = string.Empty;
			TcpClient tcp = new TcpClient();
			var res = tcp.BeginConnect(tbIP.Text, (int)udPort.Value, null, null);
			var success = res.AsyncWaitHandle.WaitOne((int)udTimeout.Value * 1000);
			if (success)
			{
				tcp.EndConnect(res);
			}
			label1.Text = success ? "OK" : "KO";
			tcp.Close();

		}
	}
}