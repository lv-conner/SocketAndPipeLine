using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server!");
            StartSever().GetAwaiter().GetResult();
            Console.ReadKey();
        }


        public static async Task StartSever()
        {
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            EndPoint listenAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            listenSocket.Bind(listenAddress);
            listenSocket.Listen(10);
            while (true)
            {
                var accpetSocket = await listenSocket.AcceptAsync();
                Task.Run(async () =>
                {
                    await ProcessSocket(accpetSocket);
                });
            }
        }

        public static async Task ProcessSocket(Socket socket)
        {
            var bufferSize = 4096;
            var innerBuff = new byte[bufferSize];
            var buffer = new ArraySegment<byte>(innerBuff);
            List<byte> receviedMessage = new List<byte>();
            while(true)
            {
                int receviedCount = await socket.ReceiveAsync(buffer, SocketFlags.None);
                while (receviedCount > bufferSize)
                {
                    receviedMessage.AddRange(buffer.Slice(0,receviedCount));
                    receviedCount = await socket.ReceiveAsync(buffer, SocketFlags.None);
                }
                receviedMessage.AddRange(buffer.Slice(0, receviedCount));
                var message = Encoding.UTF8.GetString(receviedMessage.ToArray());
                receviedMessage.Clear();
                Console.WriteLine("recevied from client\t" + message);
                await socket.SendAsync(GetSendMessageBuffer(message), SocketFlags.None);
            }

        }

        public static List<ArraySegment<byte>> GetSendMessageBuffer(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var bufferSize = 4096;
            List<ArraySegment<byte>> sendMessage = new List<ArraySegment<byte>>();
            int count = 0;
            if (messageBytes.Length > bufferSize)
            {
                for (int i = bufferSize; i < messageBytes.Length + bufferSize; i += bufferSize)
                {
                    if (i > messageBytes.Length)
                    {
                        count = messageBytes.Length + bufferSize - i;
                    }
                    else
                    {
                        count = bufferSize;
                    }
                    var msgPart = new ArraySegment<byte>(messageBytes, i - bufferSize, count);
                    sendMessage.Add(msgPart);
                }
            }
            else
            {
                var msgPart = new ArraySegment<byte>(messageBytes, 0, messageBytes.Length);
                sendMessage.Add(msgPart);
            }
            return sendMessage;
        }


    }


    class ReceviedByte
    {
        public ReceviedByte(ArraySegment<byte> message,int count)
        {
            MessageSegment = message;
            Count = count;
        }
        public ArraySegment<byte> MessageSegment { get; set; }
        public int Count { get; set; }
    }
}
