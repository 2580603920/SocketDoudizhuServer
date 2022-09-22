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
            ControllerManager.Instance.AddController(requestCode , this);
        }


        public override void HandleRequest( MainPack pack , Client client )
        {
            base.HandleRequest(pack , client);
            switch ( pack.Actioncode )
            {
                case ActionCode.StartGame:
                    {

                        HandleStartGame(pack , client);
                        break;
                    }
                case ActionCode.RobHost:
                    {
                        HandleRobDizhu(pack , client);
                        break;
                    }
            }

        }

        //开始游戏
        void HandleStartGame( MainPack pack , Client client )
        {
            MainPack returnPack = new MainPack();

            returnPack.Requestcode = requestCode;
            returnPack.Actioncode = pack.Actioncode;
            if ( client.room.StartGame(client.username) )
            {

                returnPack.Returncode = ReturnCode.Success;

            }
            else
            {
                returnPack.Returncode = ReturnCode.Fail;

            }
            client.Send(returnPack);
            //开始游戏
            ThreadPool.QueueUserWorkItem(Gaming , client.room);
        }
        void HandleRobDizhu(MainPack pack,Client client) 
        {
            if ( pack.Returncode == ReturnCode.Success )
            {
                client.room.RobDizhu(client.username , true);
            }
            else
                client.room.RobDizhu(client.username , false);


        }
        void Gaming( object obj) 
        {
          
            Thread.Sleep(3000);
            Room room = obj as Room;
           
            while ( room.status == 3 ) 
            {
                room.Update();
                //发包
                MainPack pack = new MainPack();
                pack.Requestcode = requestCode;
                pack.Actioncode = ActionCode.GetGameInfo;
                pack.Returncode = ReturnCode.Success;
                GameInfo gameInfo = new GameInfo();
                gameInfo.Status = room.GameStatus;
                gameInfo.Curusername = room.curRoomClientNames[room.CurBoutPlayerIndex];
                if( room.DiZhuName !=null)
                    gameInfo.Dizhu = room.DiZhuName;
                gameInfo.Playertimes = room.Timer+1;
                //抢地主结束后发地主牌和更新玩家手牌
                if ( room.GameStatus == 3 ) 
                {
                    foreach ( var item in room.GetDiZhuPokers() ) 
                    {
                        Poker poker = new Poker();
                        poker.Weight = item.weight;
                        poker.Pokercolor =(int)item.pokerColor + 1;
                        gameInfo.Dizhupoker.Add(poker);
                    }

                    int i = 0;
                    foreach ( var item in room.curRoomClientNames ) 
                    {
                        GetClient(item).Send(GetDealPokerPack(room,i++));
                    
                    }
                    
                }
                
                pack.Gameinfo = gameInfo;
                room.BoredCast(pack);
                Thread.Sleep(1000);
                
               
            }
         

        }

        //通知更新玩家牌组信息包
        public MainPack GetDealPokerPack(Room room,int index)
        {

            MainPack pack = new MainPack();
            pack.Returncode = ReturnCode.Success;
            pack.Requestcode = RequestCode.Game;
            pack.Actioncode = ActionCode.DealPoker;
            GameInfo gameInfo = new GameInfo();
            room.GetPlayerPokers(gameInfo.Playerinfo, index);
            pack.Gameinfo = gameInfo;

            return pack;


        }
    }
}
