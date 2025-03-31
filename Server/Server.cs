using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class Server
{
    private const int Port = 12345; // Port number for the server to listen on
    private static readonly object FileLock = new object(); // Lock object to prevent file access conflicts between threads
    private static ConcurrentDictionary<string, ClientData> clientData = new ConcurrentDictionary<string, ClientData>(); // Holds client data, ensuring thread-safety
    private static readonly int ClientTimeoutInSeconds = 3; // Timeout threshold for client disconnection, in seconds

    // Main method to start the server
    static void Main()
    {

        TcpListener server = new TcpListener(IPAddress.Any, Port); // Listen for connections on all available IP addresses and the specified port
        server.Start(); // Start the server
        Console.WriteLine($"Server running on port {Port}"); // Log the server's status
        LogToFile("Server started and listening on port " + Port); // Log server start to file

        // Main loop to accept incoming client connections
        while (true)
        {
            TcpClient client = server.AcceptTcpClient(); // Accept a new client connection
            Thread clientThread = new Thread(() => HandleClient(client)); // Create a new thread to handle the client
            clientThread.IsBackground = true; // Set the thread to background so it doesn't block the main thread
            clientThread.Start(); // Start the client handling thread
        }
    }

    // Method to handle communication with a client
    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream(); // Get the network stream to read and write data
        StreamReader reader = new StreamReader(stream, Encoding.ASCII); // Set up the reader for incoming data
        string clientId = null; // Variable to store the client ID

        try
        {
            clientId = reader.ReadLine(); // Read the client ID from the first line of data
            if (clientId != null)
            {
                Console.WriteLine($"Client connected: {clientId}"); // Log client connection
                LogToFile($"Client connected: {clientId}"); // Log client connection to file
            }

            // Initialize or retrieve client data from the dictionary
            var data = clientData.GetOrAdd(clientId, new ClientData());
            bool headerSkipped = false; // Flag to indicate if header has been skipped
            string line;

            DateTime lastReadTime = DateTime.Now; // Track the time of the last read to detect client timeouts

            // Process the client's incoming data line by line
            while ((line = reader.ReadLine()) != null)
            {
                // Check if client has been inactive for too long (timeout check)
                if ((DateTime.Now - lastReadTime).TotalSeconds > ClientTimeoutInSeconds)
                {
                    Console.WriteLine($"[Timeout] No data received from {clientId} for more than {ClientTimeoutInSeconds} seconds. Disconnecting...");
                    LogToFile($"[Timeout] No data received from {clientId} for more than {ClientTimeoutInSeconds} seconds. Disconnecting...");
                    break; // Break the loop if timeout occurs
                }

                lastReadTime = DateTime.Now; // Update last read time when data is received

                if (line == "EOF") // Check for EOF signal
                {
                    // Handle EOF message (end of transmission)
                    Console.WriteLine($"[EOF] Received for client {clientId}. Calculating average.");
                    LogToFile($"[EOF] Received for client {clientId}. Calculating average.");
                    CalculateAndSaveAverage(clientId, data); // Trigger calculation of fuel consumption average
                    break; // Exit the loop after EOF
                }

                // Skip the header if it's the first line containing fuel total information
                if (!headerSkipped && line.StartsWith("FUEL TOTAL QUANTITY"))
                {
                    headerSkipped = true;
                    continue; // Skip this line and move to the next
                }

                // Process the telemetry data and print raw data
                ProcessTelemetry(clientId, line, data);
            }
        }
        catch (IOException ex)
        {
            // Handle client disconnection or read errors
            Console.WriteLine($"Connection lost with {clientId}: {ex.Message}");
            LogToFile($"Connection lost with {clientId}: {ex.Message}");
        }
        finally
        {
            // Always attempt to calculate the average even if client disconnects unexpectedly
            if (clientId != null && clientData.ContainsKey(clientId))
            {
                Console.WriteLine($"[Disconnection] Calculating average for client {clientId}.");
                LogToFile($"[Disconnection] Calculating average for client {clientId}.");
                CalculateAndSaveAverage(clientId, clientData[clientId]); // Calculate average consumption
            }

            client.Close(); // Close the client connection
            Console.WriteLine($"Connection closed for {clientId}"); // Log client disconnection
            LogToFile($"Connection closed for {clientId}"); // Log to file
        }
    }

    // Method to process each line of telemetry data
    static void ProcessTelemetry(string clientId, string line, ClientData data)
    {
        // Skip irrelevant lines that contain fuel total quantity or empty/malformed lines
        if (line.Contains("FUEL TOTAL QUANTITY") || string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string[] parts = line.Split(','); // Split the line by commas to extract timestamp and fuel remaining

        if (parts.Length < 2)
        {
            Console.WriteLine($"Malformed line from {clientId}: {line}");
            LogToFile($"Malformed line from {clientId}: {line}");
            return;
        }

        // Extract timestamp and fuel remaining values
        string rawTimestamp = parts[1].Trim(); // Timestamp (second part of the line)
        string fuelRemainingStr = parts[2].Trim(); // Fuel remaining value (third part of the line)

        // Normalize timestamp for better parsing
        string timestamp = rawTimestamp.Replace('_', '/');

        // Try to parse timestamp and fuel remaining values
        if (DateTime.TryParse(timestamp, out DateTime time) && double.TryParse(fuelRemainingStr, out double fuelRemaining))
        {
            // Log and print raw telemetry data
            Console.WriteLine($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");
            LogToFile($"[{clientId}] Time: {timestamp}, Fuel: {fuelRemaining}");

            // Calculate fuel consumption if previous data is available
            if (data.PreviousFuel.HasValue && data.PreviousTime.HasValue)
            {
                double fuelUsed = data.PreviousFuel.Value - fuelRemaining; // Calculate fuel used
                double timeElapsed = (time - data.PreviousTime.Value).TotalMinutes; // Calculate time elapsed

                if (fuelUsed >= 0 && timeElapsed > 0) // Ensure valid data
                {
                    data.TotalFuelUsed += fuelUsed; // Accumulate total fuel used
                    data.TotalTime += timeElapsed; // Accumulate total time
                }
            }

            // Update previous fuel and timestamp for future calculations
            data.PreviousFuel = fuelRemaining;
            data.PreviousTime = time;
        }
        else
        {
            // Handle invalid data format
            Console.WriteLine($"Invalid data format from {clientId}: {line}");
            LogToFile($"Invalid data format from {clientId}: {line}");
        }
    }

    // Method to calculate the average fuel consumption for a client and store the result
    static void CalculateAndSaveAverage(string clientId, ClientData data)
    {
        double avgConsumption = data.TotalTime > 0 ? data.TotalFuelUsed / data.TotalTime : 0; // Calculate average consumption
        Console.WriteLine($"[{clientId}] Average Fuel Consumption: {avgConsumption:F4} gallons/min"); // Display the result
        LogToFile($"[{clientId}] Average Fuel Consumption: {avgConsumption:F4} gallons/min"); // Log the result to file

        // Store the result in CSV file, ensuring thread-safe access
        lock (FileLock)
        {
            try
            {
                File.AppendAllText("Results.csv", $"{clientId},{avgConsumption:F4}{Environment.NewLine}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message}"); // Handle file write errors
                LogToFile($"Error writing to file: {ex.Message}");
            }
        }

        // Remove client data from the dictionary after processing
        clientData.TryRemove(clientId, out _);
    }

    // Method to log messages to a log file
    static void LogToFile(string message)
    {
        lock (FileLock) // Ensure only one thread accesses the file at a time
        {
            try
            {
                File.AppendAllText("server_log.txt", $"{DateTime.Now}: {message}{Environment.NewLine}"); // Write the message to the log file
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error logging to file: {ex.Message}"); // Handle file write errors
            }
        }
    }
}

// ClientData class to store the telemetry data and calculations for each client
public class ClientData
{
    public double TotalFuelUsed { get; set; } // Total fuel used by the client
    public double TotalTime { get; set; } // Total time the client has been transmitting data
    public double? PreviousFuel { get; set; } // Previous fuel reading (for fuel consumption calculation)
    public DateTime? PreviousTime { get; set; } // Previous timestamp (for time difference calculation)
}
