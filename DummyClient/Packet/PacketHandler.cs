using ServerCore;

internal class PacketHandler
{
    // 어떤 session에서 조립이 되었는지, 어떤 packet인지 인자로 받음.
    public static void C_PlayerInfoReqHandler(PacketSession session, IPacket packet)
    {
        C_PlayerInfoReq p = packet as C_PlayerInfoReq;

        Console.WriteLine($"Player InfoReq: {p.playerId}, {p.name}");
        foreach (C_PlayerInfoReq.Skill skill in p.skills)
        {
            Console.WriteLine($"Skill({skill.id})({skill.level})({skill.duration})");
        }
    }

    public static void S_TestHandler(PacketSession session, IPacket packet)
    {
    }
}
