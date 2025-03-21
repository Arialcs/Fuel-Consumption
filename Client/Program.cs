using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private const string ServerAddress = "10.0.0.83"; // Replace with your server IP
    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // Delay between sending data
    private static readonly string AirplaneId = "Plane" + new Random().Next(100, 999); // Dynamic ID per client

    static void Main()
    {
        Console.Write("Enter the number of clients to run: ");
        int totalClients;
        if (!int.TryParse(Console.ReadLine(), out totalClients) || totalClients <= 0)
        {
            Console.WriteLine("Invalid number of clients.");
            return;
        }

        // Loop to start multiple clients with different files
        for (int i = 0; i < totalClients; i++)
        {
            int clientNumber = i + 1;
            string filePath = PromptForFilePath(clientNumber); // Prompt user for file path for each client
            Thread clientThread = new Thread(() => RunClient(clientNumber, filePath));
            clientThread.Start();
        }
    }

    // This method will be called by each client thread
    static void RunClient(int clientNumber, string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found at {filePath} for Client {clientNumber}");
                Console.ResetColor();
                return;
            }

            using (TcpClient client = new TcpClient(ServerAddress, ServerPort))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                string clientId = AirplaneId + clientNumber; // Unique Airplane ID per client
                writer.WriteLine(clientId); // Send client ID to server
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[{clientId}] Sent Airplane ID");
                Console.ResetColor();

                string[] dataLines = File.ReadAllLines(filePath);
                int totalLines = dataLines.Length;

                for (int i = 0; i < totalLines; i++)
                {
                    writer.WriteLine(dataLines[i]); // Send data line to the server

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\r[{clientId}] Progress: {i + 1}/{totalLines} [{GenerateProgressBar(i + 1, totalLines, 30)}] {((i + 1) * 100 / totalLines)}% ");
                    Console.ResetColor();

                    Thread.Sleep(DelayMilliseconds);
                }

                writer.WriteLine("EOF");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[{clientId}] Completed sending data. Closing connection.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError in Client: {ex.Message}");
            Console.ResetColor();
        }
    }

    // Prompt the user for the file path for each client
    static string PromptForFilePath(int clientNumber)
    {
        Console.Write($"Enter the file path for Client {clientNumber}: ");
        string filePath = Console.ReadLine()?.Trim();
        return filePath;
    }

    // Method to generate a progress bar
    static string GenerateProgressBar(int current, int total, int barLength)
    {
        int filledLength = (int)((double)current / total * barLength);
        return new string('█', filledLength) + new string('-', barLength - filledLength);
    }
}
