using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerV2
{
    class Program
    {
        static async Task Main(string[] args)
        {

            await StartServer();

            Console.WriteLine("Hello World!");
        }

        static async Task StartServer()
        {
            var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            var listenAddress = new IPEndPoint(IPAddress.Loopback, 9630);
            listenSocket.Bind(listenAddress);
            listenSocket.Listen(256);
            while(true)
            {
                var accpetSocket = await listenSocket.AcceptAsync();
                var _ = ProcessConnection(accpetSocket);
            }
        }

        static async Task ProcessConnection(Socket socket)
        {
            Console.WriteLine(socket.RemoteEndPoint.ToString() + "connected");
            var pipe = new Pipe();
            var writer = pipe.Writer;
            var reader = pipe.Reader;
            var writeToPipeTask = WriteToPipe(socket, writer);
            var readFromPipeTask = ReadFromPipe(reader, ProcessLine);
            await Task.WhenAll(writeToPipeTask, readFromPipeTask);
            Console.WriteLine(socket.RemoteEndPoint.ToString() + "disconnected");
        }
        private static void ProcessLine(ReadOnlySequence<byte> buffer)
        {
            foreach (var segment in buffer)
            {
                Console.Write(Encoding.UTF8.GetString(segment.Span));
            }
            Console.WriteLine();
        }
        static async Task WriteToPipe(Socket socket,PipeWriter writer)
        {
            int minBufferSize = 4096;
            int readBytes = 0;
            while(true)
            {
                var buffer = writer.GetMemory(minBufferSize);
                readBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (readBytes == 0)
                {
                    break;
                }
                writer.Advance(readBytes);
                var fulshResult = await writer.FlushAsync();
                if(fulshResult.IsCompleted)
                {
                    break;
                }
            }
            writer.Complete();
        }

        static async Task ReadFromPipe(PipeReader reader,Action<ReadOnlySequence<byte>> operation)
        {
            while(true)
            {
                var readResult = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = readResult.Buffer;
                SequencePosition? position = null;
                do
                {
                    position = buffer.PositionOf((byte)'\n');
                    if (position != null)
                    {
                        var line = buffer.Slice(0, position.Value);
                        operation?.Invoke(line);
                        var next = buffer.GetPosition(1, position.Value);//等价于popstion+1因为需要包含'\n';
                        buffer = buffer.Slice(next);//跳过已经读取的数据；
                    }
                }
                while (position != null);
                //指示PipeReader已经消费了多少数据
                reader.AdvanceTo(buffer.Start, buffer.End);
                if (readResult.IsCompleted)
                {
                    break;
                }
            }
            reader.Complete();

        }
    }
}
