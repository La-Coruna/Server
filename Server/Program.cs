using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using ServerCore;

namespace Server
{
    internal class Program
    {
        static Listener _listener = new Listener();
        public static GameRoom Room = new GameRoom();

        static void FlushRoom()
        {
            Room.Push(() => Room.Flush());
            JobTimer.Instance.Push(FlushRoom, 250);
        }

        static void Main(string[] args)
        {
            // DNS
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);


            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            
            JobTimer.Instance.Push(FlushRoom, 250); // FlushRoom();로 써도 됨.

            while (true)
            {
                JobTimer.Instance.Flush();   
            }


        }
    }
}
