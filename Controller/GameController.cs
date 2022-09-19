using SocketDoudizhuServer.Servers;
using SoketDoudizhuProtocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SocketDoudizhuServer.Controller
{
    class GameController : ControllerBase
    {
        

        public GameController( ) : base() { }
        public override void Initial( )
        {
            base.Initial();
            requestCode = RequestCode.Game;
        }


        public override void HandleRequest( MainPack pack , Client client )
        {
            base.HandleRequest(pack , client);
            switch ( pack.Actioncode ) 
            {
                case ActionCode.StartGame: 
                    {

                        ThreadPool.QueueUserWorkItem(StartGame);
                        break;
                    }
            }

        }

        //开始游戏
        void StartGame(object obj ) 
        {
            for ( int i = 0 ; i < 5 ; i++ ) 
            {

                Thread.Sleep(1000);
            }
            
        }


    }
}
