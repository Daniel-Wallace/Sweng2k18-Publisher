using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;

namespace Publisher
{
	class Program
	{
		static void Main(string[] args)
		{
			TcpListener serverSocket = new TcpListener(8888);
			TcpClient clientSocket = default(TcpClient);
			int counter = 0;

			serverSocket.Start();
			Console.WriteLine(" >> " + "Server Started");

			counter = 0;
			while (true)
			{
				counter += 1;
				clientSocket = serverSocket.AcceptTcpClient();
				Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
				handleClient client = new handleClinet();
				client.startClient(clientSocket, Convert.ToString(counter));
			}

			clientSocket.Close();
			serverSocket.Stop();
			Console.WriteLine(" >> " + "exit");
			Console.ReadLine();
		}
	}

	//Class to handle each client request separatly
	public class handleClient
	{
		TcpClient clientSocket;
		string clNo;
		public void startClient(TcpClient inClientSocket, string clineNo)
		{
			this.clientSocket = inClientSocket;
			this.clNo = clineNo;
			Thread ctThread = new Thread(doChat);
			ctThread.Start();
		}
		// This is going to be done in the thread.
		private void doChat()
		{
			
			String[,] beamData;		
			int beamLine = 0;       // Current line in beamData

			String[,] targetData;
			int targetLine = 0;		// Current line in targetData

			while ((true))
			{
				try
				{
					NetworkStream networkStream = clientSocket.GetStream();
					
					
					// Send row of Target
					send_to_Sub(networkStream, "Suhhhh???");
					// Wait for response that client got target data. then you know you can send target data again
					recieve_From_Sub(networkStream);
					// Send row of Beam    
					send_to_Sub(networkStream, "foobar");
					// Wait for response that client got beam data. then you know you can send beam data again
					recieve_From_Sub(networkStream);
				}
				catch (Exception ex)
				{
					Console.WriteLine(" >> " + ex.ToString());
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="networkStream"></param> Current subscriber socket stream.
		private void recieve_From_Sub(NetworkStream networkStream)
		{
			byte[] bytesFrom = new byte[10025];
			string dataFromClient = null;

			networkStream.Read(bytesFrom, 0, (int)bytesFrom.Length);
			dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
			dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="networkStream"></param> Current subscriber socket stream.
		/// <param name="csvLine"></param> CSV data to be written to the subscriber.
		private void send_To_Sub(NetworkStream networkStream, String csvLine)
		{
			Byte[] sendBytes = null;

			sendBytes = Encoding.ASCII.GetBytes(csvLine);
			networkStream.Write(sendBytes, 0, sendBytes.Length);
			networkStream.Flush();
		}
	}
}
