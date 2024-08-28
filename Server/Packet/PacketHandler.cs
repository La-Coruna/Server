﻿using Server;
using ServerCore;

internal class PacketHandler
{
    // 어떤 session에서 조립이 되었는지, 어떤 packet인지 인자로 받음.
    public static void C_ChatHandler(PacketSession session, IPacket packet)
    {
        C_Chat chatPacket = packet as C_Chat;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.Room == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(
            () => clientSession.Room.Broadcast(clientSession,chatPacket.chat)
        );
    }
}
