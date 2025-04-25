using System;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.IO.Pipes;
#else
using System.Net.Sockets;
#endif
using System.Threading;
public static class PortCommunicationManager 
{
    private const int BUFFER_SIZE = 256; // Maximum ports we might need
    
    // Double-buffered input system
    private static byte[][] inputBuffers = { new byte[BUFFER_SIZE], new byte[BUFFER_SIZE] };
    private static volatile int activeInputBuffer = 0;
    
    // Output buffer (single writer, multiple readers OK)
    private static byte[] outputBuffer = new byte[BUFFER_SIZE];
    
    private static Thread commThread;
    private static bool running = false;

    public static void Initialize()
    {
        if (running) return;

        InitializeBuffers();

        running = true;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        commThread = new Thread(WindowsPipeThread);
#else
        commThread = new Thread(UnixSocketThread);
#endif


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
        using var pipe = new NamedPipeServerStream(
            "dls_pipe",
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );

        pipe.WaitForConnection();
        
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
                {
                    //TODO: Handle disconnect
                }
                
            }
            

            // 2. Write output
            try
            {
                pipe.Write(outputBuffer, 0, BUFFER_SIZE);
                pipe.Flush();
            }
            catch
            {
                // TODO: Handle disconnect
            }
        }
    }
#else
    private static void UnixSocketThread()
    {
        string socketPath = "/tmp/dls.sock";
        if(System.IO.File.Exists(socketPath)) System.IO.File.Delete(socketPath);

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        var endPoint = new UnixDomainSocketEndPoint(socketPath);
        socket.Bind(endPoint);
        socket.Listen(1);

        using var client = socket.Accept();
        byte[] tempBuffer = new byte[BUFFER_SIZE];

        while (running)
        {
            // 1. Read external input
            if (client.Poll(0, SelectMode.SelectRead))
            {
                if (client.Receive(tempBuffer) > 0)
                {
                    // Copy to inactive buffer
                        Buffer.BlockCopy(tempBuffer, 0, inputBuffers[1 - activeInputBuffer], 0, BUFFER_SIZE);

                    // Flip the buffers
                    activeInputBuffer = 1 - activeInputBuffer;
                }

            }

            // 2. Write output
            client.Send(outputBuffer);
        }
    }
#endif

    public static void Shutdown()
    {
        running = false;
        commThread?.Join(50);
        commThread = null;
    }
}