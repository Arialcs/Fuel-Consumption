using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Client
{
    private const string ServerAddress = "127.0.0.1"; // Replace with actual server IP
    //jai - 10.144.105.88
    //girt - 10.144.104.146

    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // 1 second delay between sending lines

    // Hardcoded Airplane ID and Data File Path
    private static readonly string AirplaneId = "Plane" + new Random().Next(100, 999); // Generates Plane123-like ID
    private const string FilePath = @"C:\Users\Jai\Desktop\pro Part 2\Data Files\Telem_2023_3_12 14_56_40.txt"; // Path to the data file
    //C:\Users\soory\Downloads\Data Files\Telem_2023_3_12 16_26_4.txt
    //@"C:\Users\Jai\Desktop\pro Part 2\Data Files\Telem_2023_3_12 14_56_40.txt"
   // C:\Users\giris\Downloads\Data Files\Telem_2023_3_12 16_26_4.txt
    static void Main()
    {
        try
        {
            TcpClient client = new TcpClient(ServerAddress, ServerPort);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Send airplane ID to server
            writer.WriteLine(AirplaneId);
            Console.WriteLine($"Sent Airplane ID: {AirplaneId}\n");

            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"Data file not found: {FilePath}");
                client.Close();
                return;
            }

            string[] dataLines = File.ReadAllLines(FilePath);
            int totalLines = dataLines.Length;

            // Send each line of data
            for (int i = 0; i < totalLines; i++)
            {
                writer.WriteLine(dataLines[i]);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\rProgress: {i + 1}/{totalLines} [{GenerateProgressBar(i + 1, totalLines, 30)}] {((i + 1) * 100 / totalLines)}% ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Thread.Sleep(DelayMilliseconds);
            }

            // Signal EOF to server
            writer.WriteLine("EOF");
            Console.WriteLine("\nCompleted sending data. Closing connection.");

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }

    static string GenerateProgressBar(int current, int total, int barLength)
    {
        int filledLength = (int)((double)current / total * barLength);
        return new string('█', filledLength) + new string('-', barLength - filledLength);
    }
}
