using System;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.IO.Pipes;
#else
using System.Net.Sockets;
#endif
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace DLS.Ports {

    public enum PortInterfaceType {
        PipeOrSocket,
        TCP,
        UDP
    }

    public static class PortCommunicationManager 
    {
        private const int BUFFER_SIZE = 256; // Maximum ports we might need
        
        // Double-buffered input system
        private static byte[][] inputBuffers = { new byte[BUFFER_SIZE], new byte[BUFFER_SIZE] };
        private static volatile int activeInputBuffer = 0;
        
        // Output buffer (single writer, multiple readers OK)
        private static byte[] outputBuffer = new byte[BUFFER_SIZE];
        
        private static Thread commThread;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private static NamedPipeServerStream winPipe;
#endif
        private static Socket currentSock;
        private static bool running = false;

        public static void Initialize(int interfaceType, int port)
        {
            if (running) return;

            InitializeBuffers();

            running = true;

            if(interfaceType == (int) PortInterfaceType.PipeOrSocket)
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                
                commThread = new Thread(WindowsPipeThread);
#else
                commThread = new Thread(UnixSocketThread);
#endif
            }
            else if (interfaceType == (int) PortInterfaceType.TCP)
            {
                commThread = new Thread(() => TCPThread(port));
            }
            else if (interfaceType == (int) PortInterfaceType.UDP)
            {
                commThread = new Thread(() => UDPThread(port));
            }

            commThread.Priority = ThreadPriority.Highest;
            commThread.Start();
        }

        public static void InitializeBuffers()
        {
            // Clear input buffers
            Array.Clear(inputBuffers[0], 0, BUFFER_SIZE);
            Array.Clear(inputBuffers[1], 0, BUFFER_SIZE);
            
            // Clear output buffer
            Array.Clear(outputBuffer, 0, BUFFER_SIZE);
            
            // Reset active buffer index
            activeInputBuffer = 0;
        }

        // Port_In chips
        public static byte ReadInputPort(int port)
        {
            // Read from inactive buffer (always consistent)
            return inputBuffers[1 - activeInputBuffer][port];
        }

        // Port_Out chips
        public static void WriteOutputPort(int port, byte data)
        {
            outputBuffer[port] = data;
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private static void WindowsPipeThread()
        {
            var pipe = new NamedPipeServerStream(
                "dls_pipe",
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte
            );

            winPipe = pipe;
            try
            {
                pipe.WaitForConnection();
            }
            catch
            {
                // Thread stopped before connection
                running = false;
                return;
            }
            
            byte[] tempBuffer = new byte[BUFFER_SIZE];
            while (running)
            {
                // 1. Read external input
                if (pipe.IsConnected)
                {
                    try
                    {
                        if (pipe.Read(tempBuffer, 0, BUFFER_SIZE) > 0)
                        {
                            // Copy to inactive buffer
                            Buffer.BlockCopy(tempBuffer, 0, inputBuffers[1 - activeInputBuffer], 0, BUFFER_SIZE);

                            // Flip the buffers
                            activeInputBuffer = 1 - activeInputBuffer;
                        }
                    }
                    catch
                    {}
                    
                }

                // 2. Write output
                try
                {
                    pipe.Write(outputBuffer, 0, BUFFER_SIZE);
                    pipe.Flush();
                }
                catch
                {}
            }
        }
#else
        private static void UnixSocketThread()
        {
            string socketPath = "/tmp/dls.sock";
            if(System.IO.File.Exists(socketPath))
            {
                System.IO.File.Delete(socketPath);
            }

            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endPoint = new UnixDomainSocketEndPoint(socketPath);
            socket.Bind(endPoint);
            socket.Listen(1);
            currentSock = socket;

            using var client = socket.Accept();
            CommunicationLoop(client);
        }
#endif

        private static void TCPThread(int port)
        {
            using var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tcpEndPoint = new IPEndPoint(IPAddress.Any, port);
            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(1);
            currentSock = tcpSocket;

            try
            {
                using var client = tcpSocket.Accept();
                CommunicationLoop(client);
            }
            catch
            {
                // Thread stopped before connection
            }
        }

        private static void UDPThread(int port)
        {
            using var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var udpEndPoint = new IPEndPoint(IPAddress.Any, port);
            udpSocket.Bind(udpEndPoint);
            currentSock = udpSocket;

            byte[] tempBuffer = new byte[BUFFER_SIZE];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            bool connected = false;

            while (running)
            {
                try 
                {
                    // UDP is connectionless...
                    if (udpSocket.Poll(0, SelectMode.SelectRead))
                    {
                        int received = udpSocket.ReceiveFrom(tempBuffer, ref remoteEP);
                        if (received > 0)
                        {
                            Buffer.BlockCopy(tempBuffer, 0, inputBuffers[1 - activeInputBuffer], 0, BUFFER_SIZE);
                            activeInputBuffer = 1 - activeInputBuffer;
                            connected = true;
                        }
                    }

                    // ... but we still need a connection!
                    if (connected)
                    {
                        udpSocket.SendTo(outputBuffer, remoteEP);
                    }
                }
                catch
                {
                    
                }
            }
        }

        private static void CommunicationLoop(Socket client)
        {
            byte[] tempBuffer = new byte[BUFFER_SIZE];
            while (running)
            {
                // 1. Read external input
                if (client.Poll(0, SelectMode.SelectRead))
                {
                    if (client.Receive(tempBuffer) > 0)
                    {
                        Buffer.BlockCopy(tempBuffer, 0, inputBuffers[1 - activeInputBuffer], 0, BUFFER_SIZE);
                        activeInputBuffer = 1 - activeInputBuffer;
                    }
                }

                // 2. Write output
                client.Send(outputBuffer);
            }
            client.Close();
        }

        public static void Shutdown()
        {
            running = false;
            commThread?.Join(50);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            winPipe?.Close();
#endif
            currentSock?.Close();
            commThread = null;
        }
    }
}