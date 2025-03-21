using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private const string ServerAddress = "10.144.113.51"; // Update if needed
    private const int ServerPort = 12345;
    private const int DelayMilliseconds = 1000; // 1 second delay between sending lines

    private static readonly string AirplaneId = "Plane" + new Random().Next(100, 999);

    static void Main()
    {
        try
        {
            // Specify the folder path where data files are stored
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data Files");

            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Folder '{folderPath}' not found.");
                Console.ResetColor();
                return;
            }

            // Get all files from the folder
            string[] files = Directory.GetFiles(folderPath);
            if (files.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No files found in the folder.");
                Console.ResetColor();
                return;
            }

            // List the files to choose from
            Console.WriteLine("Available files:");
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
            }

            // Ask the user to select a file
            Console.Write("Enter the number of the file you want to send: ");
            int fileChoice;
            if (!int.TryParse(Console.ReadLine(), out fileChoice) || fileChoice < 1 || fileChoice > files.Length)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Invalid file choice.");
                Console.ResetColor();
                return;
            }

            // Get the selected file path
            string filePath = files[fileChoice - 1];

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

                // Send EOF to indicate the client is done sending data and is disconnecting
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

    // Method to generate a progress bar
    static string GenerateProgressBar(int current, int total, int barLength)
    {
        int filledLength = (int)((double)current / total * barLength);
        return new string('█', filledLength) + new string('-', barLength - filledLength);
    }
}