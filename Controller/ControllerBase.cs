using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;
using SocketDoudizhuServer.DAO;

namespace SocketDoudizhuServer.Controller
{
    class ControllerBase 
    {
        public RequestCode requestCode;
        public ControllerManager controllerManager;
        
        //static ControllerBase instance;

        
        //public static ControllerBase Instance
        //{

        //    get
        //    {
        //        if ( instance == null )
        //            instance = new ControllerBase();
        //        return instance;

        //    }

        //}

        public ControllerBase( ) 
        {
            Initial();

        }
        public virtual void Initial( ) 
        {
            controllerManager = ControllerManager.Instance;
        }
       
        public virtual void HandleRequest( MainPack pack ,Client client) 
        {
        
            
        }
        public Client GetClient( string username )
        {

            return controllerManager.GetClient(username);

        }
        public UserData GetUserdata( )
        {
            return  controllerManager.GetUserdata();

        }

    }
}
