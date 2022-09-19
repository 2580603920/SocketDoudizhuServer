using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;

namespace SocketDoudizhuServer.Controller
{
    class ControllerBase 
    {
        public RequestCode requestCode;
        public Server server;
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
           
            server = Server.Instance;
        }
       
        public virtual void HandleRequest( MainPack pack ,Client client) 
        {
        
            
        }


    }
}
