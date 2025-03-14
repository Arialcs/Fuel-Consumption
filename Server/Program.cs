using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    private const int Port = 12345;

    static void Main()
    {
        // Start listening for incoming connections
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine($"Server is running on port {Port}");

        while (true)
        {
            // Wait for a client connection
            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.ASCII);
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Generate a unique client ID
            string clientId = Guid.NewGuid().ToString();
            Console.WriteLine($"Client connected with ID: {clientId}");

            string line;
            bool headerIgnored = false;

            // Read data from the client
            while ((line = reader.ReadLine()) != null)
            {
                // If the line starts with "FUEL TOTAL QUANTITY", we ignore it as the header
                if (!headerIgnored && line.StartsWith("FUEL TOTAL QUANTITY"))
                {
                    Console.WriteLine($"Ignoring header line from client {clientId}: {line}");
                    headerIgnored = true;
                    continue;
                }

                // If the line is "EOF", we close the connection
                if (line == "EOF")
                {
                    Console.WriteLine($"Closing connection with client: {clientId}");
                    break;
                }

                // Try to parse the data (split by commas)
                string[] parts = line.Split(',');

                if (parts.Length == 3)
                {
                    // Data is in the correct format (e.g., timestamp and value)
                    string timestamp = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Print the received data
                    Console.WriteLine($"Received data from client {clientId}: Timestamp = {timestamp}, Value = {value}");
                }
                else
                {
                    // Invalid data format
                    Console.WriteLine($"Invalid data format received from client {clientId}: {line}");
                }
            }

            // Close the connection
            client.Close();
        }
    }
}
