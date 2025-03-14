using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private const string ServerAddress = "10.144.104.146";
    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // Delay in milliseconds (1 second)
    private const string FilePath = @"C:\Users\Jai\Desktop\pro Part 2\Data Files\Telem_2023_3_12 14_56_40.txt"; // Path to the data file

    static void Main()
    {
        try
        {
            // Connect to the server
            TcpClient client = new TcpClient(ServerAddress, ServerPort);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Check if the file exists
            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"File not found: {FilePath}");
                return;
            }

            // Read the file line by line
            string[] dataToSend = File.ReadAllLines(FilePath);

            // Send the data to the server with a delay between each line
            foreach (var line in dataToSend)
            {
                writer.WriteLine(line);
                Console.WriteLine($"Sent data: {line}");
                Thread.Sleep(DelayMilliseconds); // Add a delay (1 second) between each line
            }

            // Send EOF message to signal the end of data
            writer.WriteLine("EOF");
            Console.WriteLine("Data sent successfully. Closing connection...");

            // Close the connection
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
