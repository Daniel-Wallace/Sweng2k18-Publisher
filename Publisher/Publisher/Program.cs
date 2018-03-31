/*
 * Program.cs (Server Socket) by
 * http://csharp.net-informations.com/communications/csharp-multi-threaded-server-socket.htm
 * 
 * Edited By:		Pat Mac Millan/Daniel Wallace
 * Last Edited:	    March 31st, 2018
 */

using System;
using System.IO;
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
				handleClient client = new handleClient();
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
			CSVReader reader = new CSVReader();

            //Get name of all CSV files in _Beam Data directory
            //store all beam csv files in an array
            DirectoryInfo beamDir = new DirectoryInfo(@"C:\_Beam Data");
            FileInfo[] Files = beamDir.GetFiles("*.csv"); //Getting CSV files
            string[] beamFiles = new string[Files.Length];
            int fileCounter = 0;
            foreach (FileInfo file in Files)
            {
                beamFiles[fileCounter] = file.Name;
                fileCounter++;
            }

            //Get name of all CSV files in _Target Data directory
            //store all target csv files in an array
            fileCounter = 0;
            DirectoryInfo targetDir = new DirectoryInfo(@"C:\_Target Data");
            Files = targetDir.GetFiles("*.csv"); //Getting CSV files
            string[] targetFiles = new string[Files.Length];
            foreach (FileInfo file in Files)
            {
                targetFiles[fileCounter] = file.Name;
                fileCounter++;
            }

            string bFilePath = "";
			string[,] beamData;
            int beamFileIndex = 0;  //Current index in beam file array
            int beamLine = 0;       // Current line in current beamData csv file

			string tFilePath = "";
			string[,] targetData;
            int targetFileIndex = 0;    //Current index in target file array
			int targetLine = 0;		// Current line in current targetData csv file


			while ((true))
			{
				try
				{
					NetworkStream networkStream = clientSocket.GetStream();
					
					if(targetLine < (targetData.GetLength(1) - 1))
						{
							//string tData = 
							// Send row of Target
							send_To_Sub(networkStream, );
							// Wait for response that client got target data. then you know you can send target data again
							recieve_From_Sub(networkStream);
						}

					if(beamLine < (beamData.GetLength(1) - 1))
						{
							// Send row of Beam    
							send_To_Sub(networkStream, "foobar");
							// Wait for response that client got beam data. then you know you can send beam data again
							recieve_From_Sub(networkStream);
						}
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
		private void send_To_Sub(NetworkStream networkStream, string csvLine)
		{
			Byte[] sendBytes = null;

			sendBytes = Encoding.ASCII.GetBytes(csvLine);
			networkStream.Write(sendBytes, 0, sendBytes.Length);
			networkStream.Flush();
		}
	}
}
