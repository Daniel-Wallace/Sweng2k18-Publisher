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
            string[] beamFilePaths = new string[Files.Length];
            int fileCounter = 0;
            foreach (FileInfo file in Files)
            {
                beamFilePaths[fileCounter] = @"C:\_Beam Data\" + file.Name;
                fileCounter++;
            }

            //Get name of all CSV files in _Target Data directory
            //store all target csv files in an array
            fileCounter = 0;
            DirectoryInfo targetDir = new DirectoryInfo(@"C:\_Target Data");
            Files = targetDir.GetFiles("*.csv"); //Getting CSV files
            string[] targetFilePaths = new string[Files.Length];
            foreach (FileInfo file in Files)
            {
                targetFilePaths[fileCounter] = @"C:\_Target Data\" + file.Name;
                fileCounter++;
            }

            //Read in data from first beam CSV file
            int beamFileIndex = 0;  //Current index in beam file path array
            string bFilePath = beamFilePaths[beamFileIndex];
			string[,] beamData = reader.ReadCSV(bFilePath);
            int beamLine = 0;       // Current line in current beamData csv file
            string bData = "";      //Data from current line of beam CSV file

            //Read in data from first target CSV file
            int targetFileIndex = 0;    //Current index in target file path array
            string tFilePath = targetFilePaths[targetFileIndex];
			string[,] targetData = reader.ReadCSV(tFilePath);  
			int targetLine = 0;		// Current line in current targetData csv file
            string tData = "";      //Data from current line of target CSV file

            while ((true))
			{
				try
				{
					NetworkStream networkStream = clientSocket.GetStream();
					
					if(targetLine < (targetData.GetLength(1) - 1))
					{
                        tData = ""; //reset string value for every line read
                        for (int i = 0; i < targetData.GetLength(0); i++)
                        {
                            //all values in the current row of the current target CSV file separated by a comma.
                            tData = tData + targetData[i, targetLine] + ",";
                        }
						// Send row of Target
						send_To_Sub(networkStream, tData);
						// Wait for response that client got target data. then you know you can send target data again
				        recieve_From_Sub(networkStream);
                        //increment targetLine so that the next line is sent on the next iteration of the loop
                        targetLine++;
					}
                    //reached end of current target CSV file
                    else if(targetFileIndex < targetFilePaths.Length)   //makes sure there are still target CSV files left to be read
                    {
                        //read in data from next target CSV file
                        targetFileIndex++;    //Current index in target file path array
                        tFilePath = targetFilePaths[targetFileIndex];
                        targetData = reader.ReadCSV(tFilePath);
                        targetLine = 0;		// Current line in current targetData csv file
                        tData = ""; //reset string value for every line read
                        for (int i = 0; i < targetData.GetLength(0); i++)
                        {
                            //all values in the current row of the current target CSV file separated by a comma.
                            tData = tData + targetData[i, targetLine] + ",";
                        }
                        // Send row of Target
                        send_To_Sub(networkStream, tData);
                        // Wait for response that client got target data. then you know you can send target data again
                        recieve_From_Sub(networkStream);
                        //increment targetLine so that the next line is sent on the next iteration of the loop
                        targetLine++;
                    }

					if(beamLine < (beamData.GetLength(1) - 1))
					{
                        bData = ""; //reset string value for every line read
                        for (int i = 0; i < beamData.GetLength(0); i++)
                        {
                            //all values in the current row of the current beam CSV file separated by a comma.
                            bData = bData + beamData[i, beamLine] + ",";
                        }
                        // Send row of Beam    
                        send_To_Sub(networkStream, bData);
						// Wait for response that client got beam data. then you know you can send beam data again
						recieve_From_Sub(networkStream);
                        //increment beamLine so that the next line is sent on the next iteration of the loop
                        beamLine++;
                    }
                    //reached end of current beam CSV file
                    else if (beamFileIndex < beamFilePaths.Length)   //makes sure there are still beam CSV files left to be read
                    {
                        //read in data from next beam CSV file
                        beamFileIndex++;    //Current index in beam file path array
                        bFilePath = beamFilePaths[beamFileIndex];
                        beamData = reader.ReadCSV(bFilePath);
                        beamLine = 0;		// Current line in current beamData csv file
                        bData = ""; //reset string value for every line read
                        for (int i = 0; i < beamData.GetLength(0); i++)
                        {
                            //all values in the current row of the current beam CSV file separated by a comma.
                            bData = bData + beamData[i, beamLine] + ",";
                        }
                        // Send row of Target
                        send_To_Sub(networkStream, bData);
                        // Wait for response that client got beam data. then you know you can send beam data again
                        recieve_From_Sub(networkStream);
                        //increment beamLine so that the next line is sent on the next iteration of the loop
                        beamLine++;
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
