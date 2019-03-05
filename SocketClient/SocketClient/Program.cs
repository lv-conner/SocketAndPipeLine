using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            EndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8087);
            await clientSocket.ConnectAsync(serverAddress);
            var innerBuffer = new byte[4096];
            var buffer = new ArraySegment<byte>(innerBuffer);
            while(true)
            {
                Console.WriteLine("Enter message:");
                var message = Console.ReadLine() + Environment.NewLine;
                await clientSocket.SendAsync(GetSendMessageBuffer(message), SocketFlags.None);
                Console.WriteLine("Send Complete");
                //var receviedByte = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                //var receviedMessage = Encoding.UTF8.GetString(buffer.Array, 0, receviedByte);
                //Console.WriteLine("Recevied message:\t" + receviedMessage);
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
}
