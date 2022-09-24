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
                case ActionCode.SendPoker:
                    {
                        HandleSendPoker(pack , client);
                        break;
                    }
                case ActionCode.GetGameInfo:
                {

                    HandleGetGameInfo(pack,client);
                    break;
                }
            }

        }

        //处理开始游戏请求
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
        //处理抢地主请求
        void HandleRobDizhu(MainPack pack,Client client) 
        {
            if ( pack.Returncode == ReturnCode.Success )
            {
                client.room.RobDizhu(client.username , true);
            }
            else
                client.room.RobDizhu(client.username , false);


        }
        //处理玩家获取游戏内所有信息请求
        void HandleGetGameInfo( MainPack pack , Client client ) 
        {
            if ( client.room.status != 3 ) return;
            client.room.DealPorker(client.username,1);

            MainPack pack1 = new MainPack();
            pack1.Requestcode = RequestCode.Game;
            pack1.Actioncode = ActionCode.SendPoker;
            pack1.Returncode = ReturnCode.Success;
            foreach ( var item in client.room.curRoomClientNames )
            {
                SendPokersInfo sendPokersInfo = new SendPokersInfo();
                sendPokersInfo.Username = item;
                var temp = client.room.GetSendPokers(item , client.room.Round);
                if ( temp != null ) 
                {
                    foreach ( var item2 in temp )
                    {
                        Poker poker = new Poker();
                        poker.Weight = item2.weight;
                        poker.Pokercolor = (int) item2.pokerColor + 1;
                        sendPokersInfo.Poker.Add(poker);
                    }

                }



                pack1.Sendpokersinfo.Add(sendPokersInfo);

            }
            client.Send(pack1);
        }
        //处理用户打出牌请求
        void HandleSendPoker( MainPack pack , Client client ) 
        {
           
            Dealer.PokerTypeUtils curType;
            //成功打出
            if ( client.room.PlayerSendPoker(client.username , pack.Sendpokersinfo[0].Poker.Count==0?null: pack.Sendpokersinfo[0].Poker,out curType) )
            {
              
                pack.Returncode = ReturnCode.Success;
                if( pack.Sendpokersinfo[0].Poker != null )
                    pack.Sendpokersinfo[0].PokerTypeUtils = (int)curType;
              
                client.room.BoredCast(pack);

                int tempIndex = client.room.curRoomClientNames.IndexOf(client.username);
                client.Send(GetDealPokerPack(client.room , tempIndex));
                List<int> ignoreIndex = new List<int>(){ 0 , 1 , 2 };
                ignoreIndex.Remove(tempIndex);
                client.room.BoredCast(GetDealPokerPack(client.room ,-1, ignoreIndex.ToArray()) , client.username);

                //游戏胜利广播游戏结束信息
                if ( client.room.JudgeWinner(client.username)  ) 
                {
                    
                    MainPack pack1 = new MainPack();
                    pack1.Requestcode = requestCode;
                    pack1.Actioncode = ActionCode.GetGameInfo;
                    pack1.Returncode = ReturnCode.Success;
                    GameInfo gameInfo = new GameInfo();
                    gameInfo.Status = client.room.GameStatus;
                    gameInfo.Curusername = client.username;
                    pack1.Gameinfo = gameInfo;
                    client.room.BoredCast(pack1);
                    return;
                }
                client.room.NextBout();
            }
            //打出失败
            else 
            {
                pack.Returncode = ReturnCode.Fail;
                client.Send(pack);
            }
            
        }
        bool isSend;
        void Gaming( object obj) 
        {
            
            Thread.Sleep(1500);
            Room room = obj as Room;
           
            while ( room.status == 3 ) 
            {
                if ( room.GameStatus == 4 ) return;
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

                    if ( !isSend ) 
                    {
                        int i = 0;
                        foreach ( var item in room.curRoomClientNames )
                        {
                            GetClient(item).Send(GetDealPokerPack(room , i++));

                        }
                        isSend = true;
                    }
                   
                    
                }
                
                pack.Gameinfo = gameInfo;
                room.BoredCast(pack);
                Thread.Sleep(1000);
                
               
            }
         

        }

        //获取通知更新玩家牌组信息包
        public MainPack GetDealPokerPack(Room room, int getDetailIndex , int[] ignoreInfoIndex =null  )
        {

            MainPack pack = new MainPack();
            pack.Returncode = ReturnCode.Success;
            pack.Requestcode = RequestCode.Game;
            pack.Actioncode = ActionCode.DealPoker;
            GameInfo gameInfo = new GameInfo();
            room.GetPlayerPokers(gameInfo.Playerinfo, getDetailIndex , ignoreInfoIndex);
            pack.Gameinfo = gameInfo;

            return pack;


        }
    }
}
