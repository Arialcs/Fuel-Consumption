using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private const string ServerAddress = "127.0.0.1"; // Update if needed
    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // 1 second delay between sending lines

    private static readonly string AirplaneId = "Plane" + new Random().Next(100, 999);

    static void Main()
    {
        try
        {
            Console.Write("Enter the path to the data file: ");
            string filePath = Console.ReadLine()?.Trim(); // Trim spaces and invisible characters

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found at {filePath}");
                Console.ResetColor();
                return;
            }

            using (TcpClient client = new TcpClient(ServerAddress, ServerPort))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                writer.WriteLine(AirplaneId);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nSent Airplane ID: {AirplaneId}");
                Console.ResetColor();

                string[] dataLines = File.ReadAllLines(filePath);
                int totalLines = dataLines.Length;

                for (int i = 0; i < totalLines; i++)
                {
                    writer.WriteLine(dataLines[i]);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\rProgress: {i + 1}/{totalLines} [{GenerateProgressBar(i + 1, totalLines, 30)}] {((i + 1) * 100 / totalLines)}% ");
                    Console.ResetColor();

                    Thread.Sleep(DelayMilliseconds);
                }

                writer.WriteLine("EOF");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nCompleted sending data. Closing connection.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError: {ex.Message}");
            Console.ResetColor();
        }
    }

    static string GenerateProgressBar(int current, int total, int barLength)
    {
        int filledLength = (int)((double)current / total * barLength);
        return new string('█', filledLength) + new string('-', barLength - filledLength);
    }
}
