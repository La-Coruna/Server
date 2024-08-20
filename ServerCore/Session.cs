using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]...
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {   // 패킷을 파싱하는 함수
            int processLen = 0;

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                // 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                // 여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);


        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                    RegisterSend();
            }

        }

        public void Disconnect()
        {
            // _disconnected = 1; // 이런 식으로 하면, 멀티쓰레드 환경에서 문제가 될 것임.
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            while(_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }
            _sendArgs.BufferList = _pendingList;

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock(_lock)
            { 
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    // TODO
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnRecvCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment; // recvBuffer에서 쓸 수 있는 유효범위만 찝어줌.
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); // Q1) 왜 단순하게 이렇게 안함? _recvArgs.SetBuffer(_recvBuffer.WriteSegment);


            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnRecvCompleted(null, _recvArgs);
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write 커서 이동 (데이터를 전송한 만큼)
                    if(_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고, 얼마나 처리했는지 받는다.
                    // 컨텐츠 쪽으로 데이터를 넘겨주는 것은 OnRecv()를 통해 했었음.
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if(processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    // Read 커서 이동 (데이터를 처리한 만큼)
                    if(_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }

        #endregion
    }
}


/*
코드흐름

1. Send를 했다.
_lock을 잡음
_sendQueue에 sendBuff(보낼 메세지)를 넣어줌. 
_pendingList가 비어 있다면, 내가 1빠로 들어온 것. -> 즉, registerSend를 호출
_pendingList가 채워져 있다면, 이미 예약된 목록이 있으니까 스킵(_sendQueue에만 넣어줌).

2. RegisterSend()에 들어오면, (_pendingList가 비어 있는 상태)
_sendQueue에 있는 buff들을 _pendingList에 집어 넣음
_sendArgs.BufferList에 _pendingList를 연결해줌.
_socket.SendAsync(_sendArgs)를 통해, BufferList를 전송함. (_sendArgs의 SetBuffer와 BufferList는 둘 중 하나만 사용해야 함.)
_socket.SendAsync(_sendArgs)가 바로 보내질 수 있으면 pending 은 false가 되어서 바로 OnSendCompleted가 호출 될 것임.
만약 바로 보내질 수 없으면, 언젠가 완료가 되고 이벤트가 호출되어서 OnSendCompleted가 호출 될 것임.

3. OnSendCompleted가 호출됨.
성공적으로 보냈다면, 다음 단계를 보낼 준비를 해야함.
_pendingList를 Clear해주고,
혹시 모르니까 BufferList를 null로 밀어줌. (사실 넣어줄 필요는 없기는 한데, 깔끔하게 구분을 하기 위해서 넣어줌.)
_sendQueue가 채워져 있는지 다시 확인해 봄. 만약 채워져 있다면, 다시 RegisterSend()를 호출해서 보내기 예약을 걸어둠. 

 */