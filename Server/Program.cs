using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

class Server
{
    private const int Port = 12345;
    private static readonly object FileLock = new object();

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine($"Server running on port {Port}");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream, Encoding.ASCII);

        string clientId = null;

        // Declare variables before try-catch to be accessible throughout
        double totalFuelUsed = 0;
        double totalTime = 0;

        try
        {
            // Read the airplane ID (client ID)
            clientId = reader.ReadLine();
            if (clientId != null)
                Console.WriteLine($"Client connected: {clientId}");

            double? previousFuel = null;
            DateTime? previousTime = null;
            bool headerSkipped = false;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Handle EOF or disconnection gracefully
                if (line == "EOF")
                {
                    // Calculate and save the average fuel consumption
                    CalculateAndSaveAverage(clientId, totalFuelUsed, totalTime);
                    break;
                }

                // Skip header
                if (!headerSkipped && line.StartsWith("FUEL TOTAL QUANTITY"))
                {
                    Console.WriteLine($"Header skipped for {clientId}");
                    headerSkipped = true;
                    continue;
                }

                // Process telemetry line
                string[] parts = line.Split(',');

                if (parts.Length >= 2)
                {
                    string rawTimestamp = parts[0].Trim();
                    string fuelRemainingStr = parts[1].Trim();

                    // Normalize timestamp (replace underscores with slashes)
                    string timestamp = rawTimestamp.Replace('_', '/');

                    // Try parsing timestamp and fuel
                    if (DateTime.TryParse(timestamp, out DateTime time) && double.TryParse(fuelRemainingStr, out double fuelRemaining))
                    {
                        // Calculation of fuel usage
                        if (previousFuel.HasValue && previousTime.HasValue)
                        {
                            double fuelUsed = previousFuel.Value - fuelRemaining;
                            double timeElapsed = (time - previousTime.Value).TotalMinutes;

                            if (fuelUsed >= 0 && timeElapsed > 0)
                            {
                                totalFuelUsed += fuelUsed;
                                totalTime += timeElapsed;
                            }
                        }

                        previousFuel = fuelRemaining;
                        previousTime = time;

                        Console.WriteLine($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid data format from {clientId}: {line}");
                    }
                }
                else
                {
                    Console.WriteLine($"Malformed line from {clientId}: {line}");
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Connection error with {clientId}: {ex.Message}");
        }
        finally
        {
            // Always calculate and save the average when connection closes (EOF or forced closure)
            CalculateAndSaveAverage(clientId, totalFuelUsed, totalTime);

            // Close the connection gracefully
            client.Close();
            Console.WriteLine($"Connection closed with {clientId}");
        }
    }

    static void CalculateAndSaveAverage(string clientId, double totalFuelUsed, double totalTime)
    {
        double averageConsumption = totalTime > 0 ? totalFuelUsed / totalTime : 0;
        Console.WriteLine($"Final Average Fuel Consumption for {clientId}: {averageConsumption:F4} gallons/min");

        // Save the result to a file
        lock (FileLock)
        {
            File.AppendAllText("Results.txt", $"{clientId}: {averageConsumption:F4} gallons/min{Environment.NewLine}");
        }
    }
}
