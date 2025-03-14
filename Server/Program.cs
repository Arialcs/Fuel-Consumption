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
                Console.WriteLine($"Client connected: {clientId}");

            var data = clientData.GetOrAdd(clientId, new ClientData());
            bool headerSkipped = false;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line == "EOF")
                {
                    CalculateAndSaveAverage(clientId, data);
                    break;
                }

                // Skip header
                if (!headerSkipped && line.StartsWith("FUEL TOTAL QUANTITY"))
                {
                    headerSkipped = true;
                    continue;
                }

                // ✅ Process telemetry & print raw data
                ProcessTelemetry(clientId, line, data);
            }
        }
        catch (IOException)
        {
            Console.WriteLine($"Connection lost with {clientId}");
        }
        finally
        {
            if (clientId != null)
            {
                CalculateAndSaveAverage(clientId, clientData[clientId]);
            }

            client.Close();
            Console.WriteLine($"Connection closed for {clientId}");
        }
    }


    static void ProcessTelemetry(string clientId, string line, ClientData data)
    {
        string[] parts = line.Split(',');

        if (parts.Length >= 2)
        {
            string rawTimestamp = parts[0].Trim();
            string fuelRemainingStr = parts[1].Trim();

            // Normalize timestamp
            string timestamp = rawTimestamp.Replace('_', '/');

            // Try parsing timestamp & fuel
            if (DateTime.TryParse(timestamp, out DateTime time) && double.TryParse(fuelRemainingStr, out double fuelRemaining))
            {
                // ✅ Print Raw Data
                Console.WriteLine($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");

                // Calculate fuel usage
                if (data.PreviousFuel.HasValue && data.PreviousTime.HasValue)
                {
                    double fuelUsed = data.PreviousFuel.Value - fuelRemaining;
                    double timeElapsed = (time - data.PreviousTime.Value).TotalMinutes;

                    if (fuelUsed >= 0 && timeElapsed > 0)
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
            }
        }
        else
        {
            Console.WriteLine($"Malformed line from {clientId}: {line}");
        }
    }



    static void CalculateAndSaveAverage(string clientId, ClientData data)
    {
        double avgConsumption = data.TotalTime > 0 ? data.TotalFuelUsed / data.TotalTime : 0;
        Console.WriteLine($"[{clientId}] Average Fuel Consumption: {avgConsumption:F4} gallons/min");

        lock (FileLock)
        {
            File.AppendAllText("Results.txt", $"{clientId}: {avgConsumption:F4} gallons/min{Environment.NewLine}");
        }

        clientData.TryRemove(clientId, out _);
    }
}

public class ClientData
{
    public double TotalFuelUsed { get; set; }
    public double TotalTime { get; set; }
    public double? PreviousFuel { get; set; }
    public DateTime? PreviousTime { get; set; }
}
