using DummyClient;
using ServerCore;

internal class PacketHandler
{
    // 어떤 session에서 조립이 되었는지, 어떤 packet인지 인자로 받음.
    public static void S_ChatHandler(PacketSession session, IPacket packet)
    {
        S_Chat chatPacket = packet as S_Chat;
        ServerSession serverSession = session as ServerSession;

        //if(chatPacket.playerId == 1)
            Console.WriteLine(chatPacket.chat);
    }

    public static void S_TestHandler(PacketSession session, IPacket packet)
    {
    }
}
