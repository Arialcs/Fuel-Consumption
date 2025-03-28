using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class Server
{
    private const int Port = 12345;
    private static readonly object FileLock = new object();
    private static ConcurrentDictionary<string, ClientData> clientData = new ConcurrentDictionary<string, ClientData>();

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine($"Server running on port {Port}");
        LogToFile("Server started and listening on port " + Port);

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.IsBackground = true;
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        string clientId = null;

        try
        {
            clientId = reader.ReadLine(); // Read client ID
            if (clientId != null)
            {
                Console.WriteLine($"Client connected: {clientId}");
                LogToFile($"Client connected: {clientId}");
            }

            var data = clientData.GetOrAdd(clientId, new ClientData());
            bool headerSkipped = false;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line == "EOF")
                {
                    // Handle EOF message
                    Console.WriteLine($"[EOF] Received for client {clientId}. Calculating average.");
                    LogToFile($"[EOF] Received for client {clientId}. Calculating average.");
                    CalculateAndSaveAverage(clientId, data); // Trigger calculation
                    break; // End processing for this client
                }

                // Skip header
                if (!headerSkipped && line.StartsWith("FUEL TOTAL QUANTITY"))
                {
                    headerSkipped = true;
                    continue;
                }

                // Process telemetry & print raw data
                ProcessTelemetry(clientId, line, data);
            }
        }
        catch (IOException ex)
        {
            // If the client disconnects unexpectedly during data transmission, handle it gracefully
            Console.WriteLine($"Connection lost with {clientId}: {ex.Message}");
            LogToFile($"Connection lost with {clientId}: {ex.Message}");
        }
        finally
        {
            // Ensure the calculation is always triggered, even if the client disconnects mid-transmission
            if (clientId != null && clientData.ContainsKey(clientId))
            {
                Console.WriteLine($"[Disconnection] Calculating average for client {clientId}.");
                LogToFile($"[Disconnection] Calculating average for client {clientId}.");
                CalculateAndSaveAverage(clientId, clientData[clientId]); // Always calculate the average
            }

            client.Close();
            Console.WriteLine($"Connection closed for {clientId}");
            LogToFile($"Connection closed for {clientId}");
        }
    }


    static void ProcessTelemetry(string clientId, string line, ClientData data)
    {
        // Skip lines that contain "FUEL TOTAL QUANTITY" (Header or irrelevant info)
        if (line.Contains("FUEL TOTAL QUANTITY"))
        {
            return;
        }

        // Skip empty or malformed lines
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string[] parts = line.Split(',');

        if (parts.Length < 2)
        {
            Console.WriteLine($"Malformed line from {clientId}: {line}");
            LogToFile($"Malformed line from {clientId}: {line}");
            return;
        }

        // The first part is the timestamp, and the second part is the fuel remaining
        string rawTimestamp = parts[1].Trim(); // Adjusted to get the second part (timestamp)
        string fuelRemainingStr = parts[2].Trim(); // Adjusted to get the third part (fuel remaining)

        // Normalize timestamp (Replace underscores with slashes for better parsing)
        string timestamp = rawTimestamp.Replace('_', '/');

        // Try parsing timestamp & fuel
        if (DateTime.TryParse(timestamp, out DateTime time) && double.TryParse(fuelRemainingStr, out double fuelRemaining))
        {
            // Print Raw Data
            Console.WriteLine($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");
            LogToFile($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");

            // Fuel consumption calculation fix (Prevent division by zero)
            if (data.PreviousFuel.HasValue && data.PreviousTime.HasValue)
            {
                double fuelUsed = data.PreviousFuel.Value - fuelRemaining;
                double timeElapsed = (time - data.PreviousTime.Value).TotalMinutes;

                if (fuelUsed >= 0 && timeElapsed > 0) // Ensuring valid calculations
                {
                    data.TotalFuelUsed += fuelUsed;
                    data.TotalTime += timeElapsed;
                }
            }

            data.PreviousFuel = fuelRemaining;
            data.PreviousTime = time;
        }
        else
        {
            Console.WriteLine($"Invalid data format from {clientId}: {line}");
            LogToFile($"Invalid data format from {clientId}: {line}");
        }
    }


    static void CalculateAndSaveAverage(string clientId, ClientData data)
    {
        double avgConsumption = data.TotalTime > 0 ? data.TotalFuelUsed / data.TotalTime : 0;
        Console.WriteLine($"[{clientId}] Average Fuel Consumption: {avgConsumption:F4} gallons/min");
        LogToFile($"[{clientId}] Average Fuel Consumption: {avgConsumption:F4} gallons/min");

        // ✅ Store results in CSV instead of plain text
        lock (FileLock)
        {
            File.AppendAllText("Results.csv", $"{clientId},{avgConsumption:F4}{Environment.NewLine}");
        }

        clientData.TryRemove(clientId, out _);
    }

    // ✅ Logging system to track errors, connections, and processing info
    static void LogToFile(string message)
    {
        lock (FileLock)
        {
            File.AppendAllText("server_log.txt", $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}

// ✅ Data class to store fuel usage info per client
public class ClientData
{
    public double TotalFuelUsed { get; set; }
    public double TotalTime { get; set; }
    public double? PreviousFuel { get; set; }
    public DateTime? PreviousTime { get; set; }
}
