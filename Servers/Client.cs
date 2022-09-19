using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SocketDoudizhuServer.Tools;
using Google.Protobuf;
using SoketDoudizhuProtocol;
using  MySql.Data.MySqlClient;
using SocketDoudizhuServer.Controller;

namespace SocketDoudizhuServer.Servers
{
    class Client
    {
        public MySqlConnection sqlConnection;
        Server server;
        public Socket socket;
        Message message;

        public Room room;
        public bool isLogin;

        //通信数据
        public string username;
        public int state = 1;

        string sqlConnectionStr = "database=gamedata;data source=localhost;user=root;password=123456;pooling=false;charset=utf8;port=3306";
        public Client( Server server, Socket socket) 
        {
            sqlConnection = new MySqlConnection(sqlConnectionStr);
            try 
            {
                sqlConnection.Open();
            } catch (Exception e)
            {   

                Console.WriteLine(e.Message);
            }
           
            message = new Message();
            this.server = server;
            this.socket = socket;

            StartReceive();
            Console.WriteLine("开始接收数据");
        }

        void StartReceive( ) 
        {

            socket.BeginReceive(message.Buffer,message.StartIndex,message.Remsize,SocketFlags.None, ReceiveCallback,null);
        
        }
        void ReceiveCallback(IAsyncResult ar ) 
        {
            
            try 
            {
                if ( socket == null || socket.Connected == false ) return;
                int len = socket.EndReceive(ar);
                if ( len == 0 ) 
                {
                    Console.WriteLine("接收数据为0");
                    Close();
                    return;
                }
                message.ReadBuffer(len , HandleRequest);
                StartReceive();
            } catch ( Exception e ) 
            {

                Console.WriteLine(e.Message);
                
                Close();
                return;
            }
            
            
        }
        void HandleRequest(MainPack pack) 
        {
            if ( isLogin == false ) 
            {
                if( pack.Actioncode != ActionCode.Login && pack.Actioncode != ActionCode.Register ) return;
            
            }
            Console.WriteLine("处理信息"+pack);
            server.HandleRequest(pack,this);
           
        }
        void Close( ) 
        {
            Console.WriteLine("断开");
            if(room !=null)
            room.Exit(this , out bool isExist);
            Console.WriteLine(room == null);
            if ( socket == null || socket.Connected == false ) 
            {

                this.socket = null;
                return;
            }
            socket.Close();
           
        }
        public void Send(MainPack pack ) 
        {
            if ( socket == null || socket.Connected == false ) 
            {
                Close();
                return;
            }

            Console.WriteLine("发送信息" + pack);
            socket.Send(Message.PackData(pack));
        }
        public void Bordcast( MainPack pack ,bool containSelf = false)
        {
            foreach ( var item in server.allClient ) 
            {
                if(item.Equals(this)) 
                {
                    if ( !containSelf )
                        continue;
                }
                socket.Send(Message.PackData(pack));
            }
           
        }
    }
}
