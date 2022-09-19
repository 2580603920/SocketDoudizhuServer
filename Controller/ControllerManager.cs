using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;

namespace SocketDoudizhuServer.Controller
{
    class ControllerManager
    {
        Dictionary<RequestCode , ControllerBase> allController;
        static ControllerManager instance;


        UserController userController;
        RoomController roomController;


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
            allController = new Dictionary<RequestCode , ControllerBase>();
            InitialController();
        }
        void InitialController( ) 
        {
            userController = new UserController();
            roomController = new RoomController();

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
    }
}
