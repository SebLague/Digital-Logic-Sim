using System;
using System.IO.Pipes;
using System.Threading;
using DLS.Simulation;
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
        running = true;

        commThread = new Thread(WindowsPipeThread);
        commThread.Priority = ThreadPriority.Highest;
        commThread.Start();
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
        Thread.Sleep(1);
    }

    public static void Shutdown()
    {
        running = false;
        commThread?.Join(50);
        commThread = null;
    }
}