﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Listener
    {
        Socket _listenSocket;
        Action<Socket> _onAcceptHandler; // accept가 완료 됐으면 어떻게 처리할 것이냐.

        public void Init(IPEndPoint endPoint, Action<Socket> onAccpetHandler)
        {
            // 문지기
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _onAcceptHandler += onAccpetHandler;

            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            // backlog: 최대 대기수
            _listenSocket.Listen(10);
            
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args); // 처음 낚시대를 던짐
        }

        // 클라이언트의 접속을 기다리는 함수
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null; // 낚시대를 일단 밀어줘야함. 낚시대를 재사용할 때 기존에 잔재를 없애기 위함.

            bool pending = _listenSocket.AcceptAsync(args); // 비동기 방식으로 클라이언트가 안 접속해도 바로 넘어감.
            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        // 클라이언트가 접속된 것이 확인되면 호출되는 함수.
        // 낚시대에 물고기가 잡혔으니 끌어올리고, 다시 낚시대를 던져야 함.
        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success) // 모든게 잘 처리된 경우
            {
                // TODO
                _onAcceptHandler.Invoke(args.AcceptSocket);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            RegisterAccept(args); // 낚시대를 다시 던짐
        }
         
    }
}