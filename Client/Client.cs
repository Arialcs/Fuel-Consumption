using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    private const string ServerAddress = "localhost"; // Update with actual server address
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

            // Select a random file from the list
            Random rand = new Random();
            int fileChoice = rand.Next(0, files.Length); // Randomly select a file
            string filePath = files[fileChoice];

            // Display the selected file
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nAutomatically selected file: {Path.GetFileName(filePath)}");
            Console.ResetColor();

            using (TcpClient client = new TcpClient(ServerAddress, ServerPort))
            using (NetworkStream stream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                // Send Airplane ID first
                writer.WriteLine(AirplaneId);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nSent Airplane ID: {AirplaneId}");
                Console.ResetColor();

                string[] dataLines = File.ReadAllLines(filePath);
                int totalLines = dataLines.Length;

                for (int i = 0; i < totalLines; i++)
                {
                    string line = dataLines[i];

                    // Create a structured data packet
                    string[] parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string time = parts[0].Trim();
                        string fuelRemaining = parts[1].Trim();

                        // Packet format: [Airplane ID][Time][Fuel Remaining][Checksum]
                        string packet = CreateDataPacket(AirplaneId, time, fuelRemaining);

                        // Send the packet
                        writer.WriteLine(packet);

                        // Display progress
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\rProgress: {i + 1}/{totalLines} [{GenerateProgressBar(i + 1, totalLines, 30)}] {((i + 1) * 100 / totalLines)}% ");
                        Console.ResetColor();

                        // Wait a moment before sending the next packet
                        Thread.Sleep(DelayMilliseconds);
                    }
                }

                // Send EOF to indicate the client is done sending data and is disconnecting
                writer.WriteLine("EOF");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nCompleted sending data. Closing connection.");
                Console.ResetColor();
            }
        }
        catch (IOException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError: Network-related issue - {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError: {ex.Message}");
            Console.ResetColor();
        }
    }

    // Method to generate the data packet
    static string CreateDataPacket(string airplaneId, string time, string fuelRemaining)
    {
        // Create a simple packet structure
        string packet = $"{airplaneId},{time},{fuelRemaining}";

        // Optionally, add a checksum or additional validation here if needed
        string checksum = CalculateChecksum(packet);
        packet += $",{checksum}";

        return packet;
    }

    // Method to calculate a simple checksum (e.g., sum of ASCII values of the characters)
    static string CalculateChecksum(string packet)
    {
        int sum = 0;
        foreach (char c in packet)
        {
            sum += c;
        }
        return sum.ToString();
    }

    // Method to generate a progress bar
    static string GenerateProgressBar(int current, int total, int barLength)
    {
        int filledLength = (int)((double)current / total * barLength);
        return new string('█', filledLength) + new string('-', barLength - filledLength);
    }
}
