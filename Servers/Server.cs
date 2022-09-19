using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SocketDoudizhuServer.Controller;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.DAO;


namespace SocketDoudizhuServer.Servers
{
    class Server
    {
        public UserData userData;
        ControllerManager controllerManager;
        Socket socket;
        EndPoint localIP;
        public List<Client> allTempClient;
        public Dictionary<string,Client> allClient;

        static Server instance;
        int port;

        public static Server Instance { get => instance; }
        public ControllerManager ControllerManager { get => controllerManager;  }

        public Server( int port )
        {
            instance = this;
            this.port = port;
            Initial();
        }

        void Initial() 
        {
            userData = new UserData();
            controllerManager = ControllerManager.Instance;
            allTempClient = new List<Client>();
            allClient = new Dictionary<string , Client>();
            InitialSocket();
        }
        void InitialSocket(  ) 
        {
            socket = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            localIP = new IPEndPoint(IPAddress.Any , port);
            socket.Bind(localIP);
            socket.Listen(0);
            Console.WriteLine("TCP服务开启成功");
            StartAccept();
            Console.WriteLine("开始接收远程客户端");
            StartAccept();
        }
        void StartAccept( ) 
        {

            socket.BeginAccept(AcceptCallback,null);
        
        }
        void AcceptCallback( IAsyncResult ar ) 
        {

            Socket client = socket.EndAccept(ar);

            allTempClient.Add(new Client(this , client));
            StartAccept();
        }

        public void HandleRequest( MainPack pack ,Client client) 
        {
            ControllerManager.HandleRequest(pack, client);
        }
    }
}
