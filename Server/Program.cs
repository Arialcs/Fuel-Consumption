using System;
using System.Collections.Generic;
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

        string clientId = reader.ReadLine(); // Read airplane ID
        Console.WriteLine($"Client connected: {clientId}");

        double? previousFuel = null;
        DateTime? previousTime = null;
        double totalFuelUsed = 0;
        double totalTime = 0;
        bool headerSkipped = false;

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!headerSkipped && line.StartsWith("FUEL TOTAL QUANTITY"))
            {
                Console.WriteLine($"Header skipped for {clientId}");
                headerSkipped = true;
                continue;
            }

            if (line == "EOF")
            {
                double averageConsumption = totalTime > 0 ? totalFuelUsed / totalTime : 0;
                Console.WriteLine($"Final Average Fuel Consumption for {clientId}: {averageConsumption:F4} gallons/min");

                lock (FileLock)
                {
                    File.AppendAllText("Results.txt", $"{clientId}: {averageConsumption:F4} gallons/min{Environment.NewLine}");
                }
                break;
            }

            // Process telemetry line
            string[] parts = line.Split(',');

            if (parts.Length >= 2)
            {
                string timestamp = parts[0].Trim(); // First element
                string fuelRemainingStr = parts[1].Trim(); // Second element

                if (DateTime.TryParseExact(timestamp, "d_M_yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time) &&
                    double.TryParse(fuelRemainingStr, out double fuelRemaining))
                {
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

        client.Close();
        Console.WriteLine($"Connection closed with {clientId}");
    }
}
