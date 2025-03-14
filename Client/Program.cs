using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Client
{
    private const string ServerAddress = "127.0.0.1"; // Replace with actual server IP
    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // 1 second delay between sending lines

    // Hardcoded Airplane ID and Data File Path
    private static readonly string AirplaneId = "Plane" + new Random().Next(100, 999); // Generates Plane123-like ID
    private const string FilePath = @"C:\Users\Jai\Desktop\pro Part 2\Data Files\Telem_2023_3_12 14_56_40.txt"; // Path to the data file

    static void Main()
    {
        try
        {
            TcpClient client = new TcpClient(ServerAddress, ServerPort);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Send airplane ID to server
            writer.WriteLine(AirplaneId);
            Console.WriteLine($"Sent Airplane ID: {AirplaneId}");

            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"Data file not found: {FilePath}");
                client.Close();
                return;
            }

            string[] dataLines = File.ReadAllLines(FilePath);

            // Send each line of data
            foreach (var line in dataLines)
            {
                writer.WriteLine(line);
                Console.WriteLine($"Sent: {line}");
                Thread.Sleep(DelayMilliseconds);
            }

            // Signal EOF to server
            writer.WriteLine("EOF");
            Console.WriteLine("Completed sending data. Closing connection.");

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
