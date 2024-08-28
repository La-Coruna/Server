using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ClientSession : PacketSession
    {
        public int SessionId { get; set; }
        public GameRoom Room { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            Program.Room.Push(() => Program.Room.Enter(this));
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }


        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.Instance.Remove(this);
            if(Room != null)
            {
                // Room에 대한 null 체크를 하고 들어왔더라도, jobQueue에 담겨 실행시간이 밀리기 때문에, 여전히 안전하지 않다.
                // 그래서 Room을 다른 변수에 옮겨 두고 해주면 괜찮겠다.
                GameRoom room = Room;
                room.Push(()=> room.Leave(this)); // 문제가 있어서 나중에 보완해줘야 하는 코드.
                Room = null;
            }
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }
        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }

}
