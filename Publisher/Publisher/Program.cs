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
using System.Net;
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
			string ipAddress;
			while (true)
			{
				counter += 1;
				clientSocket = serverSocket.AcceptTcpClient();
				ipAddress = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString(); // Recieves IP adress from client socket
				Console.WriteLine(" >> " + "Client at " + ipAddress + " started...");
				handleClient client = new handleClient();
				client.startClient(clientSocket, ipAddress);
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
		string clientIP;
		string clNo;

        //CSVReader object to be used to read all beam and target CSV files
        CSVReader reader = new CSVReader();

        //arrays to store all beam and target csv file paths respectively
        string[] beamFilePaths;
        string[] targetFilePaths;

        //integers to represent what file in the beam and target csv filePaths arrays, respectively, are being read from
        int beamFileIndex;
        int targetFileIndex;

        //string representation of the current beam and target csv files being read from, respectively
        string bFilePath;
        string tFilePath;

        //2-dimensional arrays containing the contents of the current beam and target csv files, respectively
        string[,] beamData;
        string[,] targetData;

        //integer to represent which line of the current beam and target csv files, respectively, is currently being read.
        int beamLine;
        int targetLine;

        //string representation of the current line of the current beam and target csv files, respectively, that were just read.
        string bData;
        string tData;

        public void startClient(TcpClient inClientSocket, string clientIP)
		{
			this.clientSocket = inClientSocket;
			this.clientIP = clientIP;
			Thread ctThread = new Thread(doChat);
			ctThread.Start();
		}

		// This is going to be done in the thread.
		private void doChat()
		{
            beamFilePaths = getAllFilesInDirectory(@"C:\_Beam Data");
            targetFilePaths = getAllFilesInDirectory(@"C:\_Target Data");

            //Read in data from first beam CSV file
            beamFileIndex = 0;  //Current index in beam file path array
            readingBeamFile(beamFileIndex);

            //Read in data from first target CSV file
            targetFileIndex = 0;    //Current index in target file path array
            readingTargetFile(targetFileIndex);

			bool targetEndOfFile = false;
			bool beamEndOfFile = false; 

            while (!targetEndOfFile && !beamEndOfFile)
			{
				try
				{
					NetworkStream networkStream = clientSocket.GetStream();

                    if (beamLine < (beamData.GetLength(1) - 1))
                    {
                        sendAndReceiveBeam(networkStream);
                    }
                    //reached end of current beam CSV file
                    else if (beamFileIndex < beamFilePaths.Length - 1)   //makes sure there are still beam CSV files left to be read
                    {
                        //read in data from next beam CSV file
                        beamFileIndex++;    //Increment current index in beam file path array
                        readingBeamFile(beamFileIndex);
                        sendAndReceiveBeam(networkStream);
                    }
					// End of all files
					else
					{
						send_To_Sub(networkStream, "End of file.");
						beamEndOfFile = true;
					}

                    if (targetLine < (targetData.GetLength(1) - 1))
					{
                        sendAndReceiveTarget(networkStream);
                    }
                    //reached end of current target CSV file
                    else if(targetFileIndex < targetFilePaths.Length-1)   //makes sure there are still target CSV files left to be read
                    {
                        //read in data from next target CSV file
                        targetFileIndex++;    //Increment current index in target file path array
                        readingTargetFile(targetFileIndex);
                        sendAndReceiveTarget(networkStream);
                    }
					// End of all files
					else
					{
						send_To_Sub(networkStream, "End of file.");
						targetEndOfFile = true;
					}
                }
				catch (Exception ex)
				{
					Console.WriteLine("Error with Client " + clientIP);
					Console.WriteLine("Error: " + ex.ToString());
					Console.WriteLine("Closing Client " + clientIP + "...");
					Console.WriteLine("Socket closed.");
					break;
				}
			}
			// End of while loop

			// Close connections
			Console.WriteLine(">> All data has been sent to Client " + clientIP + " successfully.");
			Console.WriteLine(">> Client " + clientIP + " connection has been closed...");
		}

		/// <summary>
		/// Method for handling information sent to the publisher from the subscriber.
		/// </summary>
		/// <param name="networkStream"></param> Current subscriber socket stream.
		private void recieve_From_Sub(NetworkStream networkStream)
		{
			byte[] bytesFrom = new byte[10025];
			string dataFromClient = null;
			while(dataFromClient.Equals("Message Received.") == false)
			{
				networkStream.Read(bytesFrom, 0, (int)bytesFrom.Length);
				dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
				dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("."));
			}
			Console.WriteLine("Data received from " + clientIP + ": " + dataFromClient);

			Thread.Sleep(500);
		}

		/// <summary>
		/// Method for sending information to the subscriber from the publisher.
		/// </summary>
		/// <param name="networkStream"></param> Current subscriber socket stream.
		/// <param name="csvLine"></param> CSV data to be written to the subscriber.
		private void send_To_Sub(NetworkStream networkStream, string csvLine)
		{
			Byte[] sendBytes = null;

			sendBytes = Encoding.ASCII.GetBytes(csvLine);
			networkStream.Write(sendBytes, 0, sendBytes.Length);
			networkStream.Flush();
			Console.WriteLine("---------------------------------------------------------------------");
			Console.WriteLine("Data sent to " + clientIP + ": " + csvLine);
			Console.WriteLine("---------------------------------------------------------------------");

			Thread.Sleep(500);
		}

        /// <summary>
        /// Gets the names of all CSV files in a specified directory and 
        /// returns them in an array of strings
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>filePaths</returns> array with all names of csv files in the specified directory
        private string[] getAllFilesInDirectory(string directory)
        {
            DirectoryInfo dir = new DirectoryInfo(directory);   //specify a directory to read from
            FileInfo[] Files = dir.GetFiles("*.csv"); //Getting CSV files in the directory specified
            string[] filePaths = new string[Files.Length];
            int fileCounter = 0;
            foreach (FileInfo file in Files)
            {
                //store all file paths of files in specified directory as CSVReader needs a complete file path
                filePaths[fileCounter] = directory + @"\" + file.Name;
                fileCounter++;
            }
            return filePaths;
        }

        /// <summary>
        /// Gets the file path of the beam csv file to be read from. The CSVReader then reads this file
        /// and assigns its contents to a 2-dimensional array.
        /// resets the current csv line and data string representation of that line
        /// </summary>
        /// <param name="tFileIndex"></param>
        private void readingBeamFile(int bFileIndex)
        {
            bFilePath = beamFilePaths[bFileIndex];   //file path of beam csv file to be read from
            beamData = reader.ReadCSV(bFilePath);       //contents of beam csv file that was read from
            beamLine = 1;       // Current line in current beamData csv file
            bData = ""; //data of the current line the the current beam csv file
        }

        /// <summary>
        /// Gets the file path of the target csv file to be read from. The CSVReader then reads this file
        /// and assigns its contents to a 2-dimensional array.
        /// resets the current csv line and data string representation of that line
        /// </summary>
        /// <param name="tFileIndex"></param>
        private void readingTargetFile(int tFileIndex)
        {
            tFilePath = targetFilePaths[tFileIndex];    //file path of target csv file to be read from
            targetData = reader.ReadCSV(tFilePath);       //contents of target csv file that was read from 
            targetLine = 1;		// Current line in current targetData csv file
            tData = ""; //data of the current line the the current target csv file
        }

        /// <summary>
        /// Gets all values in the current row of a beam csv file and puts them in a string delineated by a comma.
        /// The data string is then sent to a client (subscriber)
        /// Then the program waits for a response from the client (subscriber) that the data from the current row of the beam csv file was received.
        /// the current line in the current beam csv file is incremented and the string representing that data is reset.
        /// </summary>
        /// <param name="nStream"></param>
        private void sendAndReceiveBeam(NetworkStream nStream)
        {
            for (int i = 0; i < beamData.GetLength(0) - 1; i++)
            {
                //all values in the current row of the current beam CSV file separated by a comma.
                bData = bData + beamData[i, beamLine] + ",";
            }
			bData = bData.Remove(bData.Length - 1);

            // Send row of Beam data over the stream to the client (subscriber)   
            send_To_Sub(nStream, bData);
            // Wait for response that client got beam data. then you know you can send beam data again
            //recieve_From_Sub(nStream);
            //increment beamLine so that the next line is sent on the next iteration of the loop
            beamLine++;
            //reset string value for every line read
            bData = "";
        }

        /// <summary>
        /// Gets all values in the current row of a target csv file and puts them in a string delineated by a comma.
        /// The data string is then sent to a client (subscriber)
        /// Then the program waits for a response from the client (subscriber) that the data from the current row of the target csv file was received.
        /// the current line in the current target csv file is incremented and the string representing that data is reset.
        /// </summary>
        /// <param name="nStream"></param>
        private void sendAndReceiveTarget(NetworkStream nStream)
        {
            for (int i = 0; i < targetData.GetLength(0) - 1; i++)
            {
                //all values in the current row of the current target CSV file separated by a comma.
                tData = tData + targetData[i, targetLine] + ",";
            }
			tData = tData.Remove(tData.Length - 1);

            // Send row of Target data over the stream to the client (subscriber)
            send_To_Sub(nStream, tData);
            // Wait for response that client got target data. then you know you can send target data again
            //recieve_From_Sub(nStream);
            //increment targetLine so that the next line is sent on the next iteration of the loop
            targetLine++;
            //reset string value for every line read
            tData = "";
        }
	}
}
