using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class PacketHandler
    {
        // 어떤 session에서 조립이 되었는지, 어떤 packet인지 인자로 받음.
        public static void PlayerInfoReqHandler(PacketSession session, IPacket packet)
        {
            PlayerInfoReq p = packet as PlayerInfoReq;

            Console.WriteLine($"Player InfoReq: {p.playerId}, {p.name}");
            foreach (PlayerInfoReq.Skill skill in p.skills)
            {
                Console.WriteLine($"Skill({skill.id})({skill.level})({skill.duration})");
            }
        }
    }
}
