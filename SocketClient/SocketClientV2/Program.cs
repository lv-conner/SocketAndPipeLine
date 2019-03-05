using System;
using System.Net.Sockets;
using System.Net;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.IO;

namespace SocketClientV2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await SendFile();
            Console.WriteLine("Hello World!");
        }


        static async Task SendFile()
        {
            var pipe = new Pipe();
            var fileWriter = pipe.Writer;
            var fileReader = pipe.Reader;
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            var serverAddress = new IPEndPoint(IPAddress.Loopback, 9630);
            await clientSocket.ConnectAsync(serverAddress);
            using (var file = File.Open(@"D:\redis.log", FileMode.Open))
            {
                var writeTask = WriteFileToPipe(file, fileWriter);
                var sendToServerTask = SendToServer(clientSocket, fileReader);
                await Task.WhenAll(writeTask, sendToServerTask);
            }
        }

        static async Task SendToServer(Socket socket,PipeReader reader)
        {
            while(true)
            {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;
                foreach (var item in buffer)
                {
                    await socket.SendAsync(item, SocketFlags.None);
                }
                try
                {
                    reader.AdvanceTo(buffer.End);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (readResult.IsCompleted)
                {
                    break;
                }
            }
            reader.Complete();
        }

        static async Task WriteFileToPipe(Stream stream,PipeWriter writer)
        {
            var minSizeBuffer = 4096;
            var readBytes = 0;
            while(true)
            {
                var buffer = writer.GetMemory(minSizeBuffer);
                readBytes = await stream.ReadAsync(buffer);
                if(readBytes == 0)
                {
                    break;
                }
                writer.Advance(readBytes);
                var flushResult = await writer.FlushAsync();
                if(flushResult.IsCompleted)
                {
                    break;
                }
            }
            writer.Complete();
        }
    }
}
