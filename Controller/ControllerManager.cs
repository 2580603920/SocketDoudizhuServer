using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;
using SocketDoudizhuServer.DAO;

namespace SocketDoudizhuServer.Controller
{
    class ControllerManager
    {
        Dictionary<RequestCode , ControllerBase> allController;
        static ControllerManager instance;
        Server server;

        UserController userController;
        RoomController roomController;
        GameController gameController;
        void InitialController( )
        {
            userController = new UserController();
            roomController = new RoomController();
            gameController = new GameController();
        }
        public static ControllerManager Instance 
        {

            get 
            {
                if ( instance == null )
                    instance = new ControllerManager();
                return  instance;

            } 
        
        }
         ControllerManager( ) 
        {
            instance = this;
            server = Server.Instance;
            allController = new Dictionary<RequestCode , ControllerBase>();
            InitialController();
        }
       

        public void AddController( RequestCode requestCode, ControllerBase controller ) 
        {
            allController.Add(requestCode , controller);
        }

        public T GetController<T>( RequestCode requestCode ) where T: ControllerBase
        {
            ControllerBase controller = null;
            if ( allController.ContainsKey(requestCode) ) 
            {
                controller = allController[requestCode];
            }
            return controller as T;
        }
        public void HandleRequest( MainPack pack, Client client) 
        {
            allController[pack.Requestcode].HandleRequest(pack, client);
        }

        public Client GetClient( string username )
        {

            return server.GetClient(username);

        }
        public UserData  GetUserdata()
        {
            return server.GetUserData;
        }
        public void AddClient( Client client )
        {
            server.AddClient(client);
        }
        public void RemoveClient( string username )
        {
            server.RemoveClient(username);
        }
    }
}
