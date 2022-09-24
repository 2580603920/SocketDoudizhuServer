using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using Google.Protobuf.Collections;
using SocketDoudizhuServer.Controller;
using System.Linq;
using System.Threading;

namespace SocketDoudizhuServer.Servers
{
    class Room
    {
        public List<string> curRoomClientNames;
        public RoomController roomController;

        #region 游戏相关
        Dealer dealer;
        //地主候选队列
        Queue<int> diZhuIndex;
        //当前回合玩家的索引
        int curBoutPlayerIndex = 0;
        public int CurBoutPlayerIndex { get { return curBoutPlayerIndex; } }
        //地主
        string diZhuName;
        public string DiZhuName { get { return diZhuName; } }
        
        //上回合回合玩家的索引
        int LastBoutPlayerIndex = -1;
        //回合数
        int BoutNum;
        public int GetBoutNum { get { return BoutNum; } }
        //回合时间计时器
        int timer = -1;
        public int Timer { get { return timer; } }
        //回合时间
        int boutTime = 1000;
        //轮次，3回合一轮
        int round;
        public int Round { get { return round; } }
        //游戏状态, 0,表示游戏结束，1表示游戏开始，2表示游戏在抢地主阶段，3表示游戏正在进行，4表示游戏失败，5表示游戏胜利
        int gameStatus;
        public int GameStatus { get { return gameStatus; } }
        #endregion
        //通信数据
        public string hostname;
        public int maxClientNum=3;
        public int curClientNum=0;
        public string roomID;
        public string roomTitle;
        //房间状态：0房间不存在，1房间可加入，2房间已满，3房间已经开始游戏
        public int status;

        //此轮中上家出的最大牌
        List<Dealer.Poker> lastSendPokers = null; 



        public Room(Client host ,string roomID,string roomTitle) 
        {
            roomController = ControllerManager.Instance.GetController<RoomController>(RequestCode.Room);
            roomController.RegisterRoom(roomID , this);
            curRoomClientNames = new List<string>();
            this.roomID = roomID;
            this.roomTitle = roomTitle;
            hostname = host.username;
            host.room = this;
            Join(host);
            status = 1;
        }
        public ReturnCode Join( Client client )
        {
          
            if ( curRoomClientNames.Contains(client.username) ) return ReturnCode.Fail;
            if ( curClientNum + 1 > maxClientNum ) 
            {
                status = 2;
                return ReturnCode.RoomFull;                
            }
            client.room = this;
             curClientNum++;
            curRoomClientNames.Add(client.username);
           
            return ReturnCode.Success;
        }
        public void  Destroy( ) 
        {
            foreach ( var item in curRoomClientNames ) 
            {
              
                if ( roomController.GetClient(item).room == null ) continue;
                roomController.GetClient(item).room = null;
            
            }

            curRoomClientNames.Clear();
            roomController.UnRegisterRoom(roomID);
        }
        
        public bool Exit(string username,out bool isExist ) 
        {
            isExist = true;
            if ( status == 3 ) return false;
            if ( !curRoomClientNames.Contains(username) ) return true;

            curClientNum--;
            curRoomClientNames.Remove(username);
            roomController.GetClient(username).room = null;

            //房主退出房间
            if ( username == hostname || curClientNum <= 0 ) 
            {
                isExist = false;
                //通知房间其他人房间解散
                MainPack pack1 = new MainPack();
                pack1.Requestcode = RequestCode.Room;
                pack1.Actioncode = ActionCode.ExitRoom;
                pack1.Returncode = ReturnCode.Success;
                RoomInfo info = new RoomInfo();
                info.Roomid = roomID;
                pack1.Roominfo.Add(info);
                BoredCast(pack1, username);
                Destroy();

            } 
            
            return true;
        }
        public RepeatedField<PlayerInfo> GetClientInfo() 
        {
            RepeatedField<PlayerInfo> playerInfos = new RepeatedField<PlayerInfo>();
            foreach ( var item in curRoomClientNames ) 
           
            {
              
                PlayerInfo info = new PlayerInfo();
                info.Username = item;
                info.Coin = "1000";
                info.Status =roomController.GetClient(item).state;
                playerInfos.Add(info);
              
            }

            return playerInfos;

        }
        public void BoredCast(MainPack pack,string ignoreUser = null ) 
        {
            foreach ( var item in curRoomClientNames )
            {
                
                if ( item.Equals(ignoreUser) ) continue;
                roomController.GetClient(item).Send(pack);
            }

        }

      
        public bool StartGame(string username) 
        {
            if ( status == 3 || username != hostname ||curClientNum != 3) return false;
            
            gameStatus = 1;
            status = 3;
            dealer = new Dealer();
            diZhuIndex = new Queue<int>();
            dealer.Initial();
            dealer.DealPorker();
            DealPorker(username , gameStatus);
            gameStatus = 2;//抢地主阶段

           
           
            return true;
        }
        //发往客户端玩家手牌信息
        public void DealPorker( string username,int gameStatus ) 
        {

            //给房间内的玩家发送自己分配到的牌组 i代表玩家索引
            for ( int i = 0 ; i < curRoomClientNames.Count ; i++ ) 
            {
                MainPack pack = new MainPack();
                pack.Returncode = ReturnCode.Success;
                pack.Requestcode = RequestCode.Game;
                pack.Actioncode = ActionCode.GetGameInfo;

              
                GameInfo gameInfo = new GameInfo();
                gameInfo.Status = gameStatus; //游戏开始阶段

                //写入房间内玩家的信息
            
                GetPlayerPokers(gameInfo.Playerinfo,i);

                pack.Gameinfo = gameInfo;

                roomController.GetClient(curRoomClientNames[i]).Send(pack);

            }
           

        }


        //获取玩家牌组信息，指定要明牌玩家的索引
        public void GetPlayerPokers( RepeatedField<PlayerInfo> playerInfos , int getDetailIndex , int[] ignoreInfoIndex =null) 
        {
          
                for ( int j = 0 ; j < curRoomClientNames.Count ; j++ )
                {
                    if( ignoreInfoIndex !=null)
                        if ( ignoreInfoIndex.Contains(j) ) continue;
                    PlayerInfo playerInfo = new PlayerInfo();
                    playerInfo.Username = curRoomClientNames[j];
                    playerInfo.Curpokernum = dealer.allPlayerPoker[j].Count;

                    //取对应玩家的牌组
                    if ( j == getDetailIndex )
                    {

                        foreach ( var item in dealer.allPlayerPoker[j] )
                        {
                            Poker poker = new Poker();
                            poker.Weight = item.weight;

                            poker.Pokercolor = (int) item.pokerColor + 1;//1梅花、2方块、3红桃、4黑桃
                            playerInfo.Poker.Add(poker);
                        }
                    }
                    playerInfos.Add(playerInfo);
                }
               

            

        }
        //抢地主
        public bool RobDizhu( string username ,bool isRobDizhu = true )
        {
            if ( curRoomClientNames[curBoutPlayerIndex] != username || gameStatus != 2) return false;
            if ( isRobDizhu )
                diZhuIndex.Enqueue(curRoomClientNames.IndexOf(username));
            else 
            {
                //地主不叫
                if ( diZhuIndex.Count != 0 && username == hostname)
                    diZhuIndex.Dequeue();
            }

            NextBout();

            return true;
        }
       public void NextBout( ) 
        {
            curBoutPlayerIndex = ( curBoutPlayerIndex + 1 ) % 3;
            timer = boutTime + 1;
        }

       
        void RoundChange( ) 
        {
            //每轮开始没人接牌则清空桌面
            if ( round > 2  && lastSendPokers == null) 
            {
                MainPack pack = new MainPack();
                pack.Requestcode = RequestCode.Game;
                pack.Actioncode = ActionCode.SendPoker;
                pack.Returncode = ReturnCode.Success;
                foreach (var item in curRoomClientNames) 
                {
                    SendPokersInfo sendPokersInfo = new SendPokersInfo();
                    sendPokersInfo.Username = item;
                    pack.Sendpokersinfo.Add(sendPokersInfo);

                }
                BoredCast(pack);
               

            }
           
        }
        
        //调用一次减少一秒
        public void Update() 
        {
            if ( timer == 0 ) //回合结束
            {

                curBoutPlayerIndex = ( curBoutPlayerIndex + 1 ) % 3;
               
            }
            //新回合开始
            if ( curBoutPlayerIndex != LastBoutPlayerIndex ) 
            {
                int newRound = ( BoutNum / 3 ) + 1;
                if ( round != newRound )
                {
                    round = newRound;
                    RoundChange();

                }
                BoutNum++;
                
                LastBoutPlayerIndex = curBoutPlayerIndex;
                timer = boutTime+1;
                if ( round != 1 ) 
                {
                    MainPack pack = new MainPack();
                    pack.Requestcode = RequestCode.Game;
                    pack.Actioncode = ActionCode.SendPoker;
                    pack.Returncode = ReturnCode.Success;
                    SendPokersInfo sendPokersInfo = new SendPokersInfo();
                    sendPokersInfo.Username = curRoomClientNames[curBoutPlayerIndex];
                    pack.Sendpokersinfo.Add(sendPokersInfo);
                    roomController.GetClient(curRoomClientNames[curBoutPlayerIndex]).Send(pack);
                }
                
            }

            // 抢地主阶段结束处理事件
            if ( BoutNum == 4 && timer == boutTime + 1 ) 
                {   
                    // 有人叫或抢过
                    if ( diZhuIndex.Count != 0 )
                    {

                        diZhuName = curRoomClientNames[diZhuIndex.Peek()];
                        curBoutPlayerIndex = diZhuIndex.Peek();

                    }
                    //无人叫或抢过
                    else
                    {

                        diZhuName = hostname;
                        curBoutPlayerIndex = 0;
                    }
                    LastBoutPlayerIndex = curBoutPlayerIndex;

                    diZhuIndex.Clear();
                    dealer.allPlayerPoker[curRoomClientNames.IndexOf(diZhuName)] = dealer.allPlayerPoker[curRoomClientNames.IndexOf(diZhuName)].Concat(dealer.diZhuPai).ToList(); // 将地主牌插入地主玩家牌组
                    dealer.SortPork(dealer.allPlayerPoker[curRoomClientNames.IndexOf(diZhuName)]);

                    gameStatus = 3; 
                }

            if ( timer == 1 ) 
            {
                //自动出牌
                if ( lastSendPokers == null && gameStatus == 3 )
                {
                    MainPack pack = new MainPack();
                    pack.Requestcode = RequestCode.Game;
                    pack.Actioncode = ActionCode.SendPoker;
                    SendPokersInfo sInfo = new SendPokersInfo();
                    sInfo.Username = curRoomClientNames[curBoutPlayerIndex];

                    Poker poker = new Poker();
                    Dealer.Poker dPoker = dealer.allPlayerPoker[curBoutPlayerIndex].First();
                    poker.Weight = dPoker.weight;
                    poker.Pokercolor = (int)dPoker.pokerColor+1;
                    sInfo.Poker.Add(poker);
                    pack.Sendpokersinfo.Add(sInfo);
                    ControllerManager.Instance.GetController<GameController>(RequestCode.Game).HandleRequest(pack , roomController.GetClient(sInfo.Username));

                }
            }
            timer--;
            //回合时间耗尽，进入下一个玩家回合
          


        }
        void GameOver( ) 
        {
            timer = -1;
            lastSendPokers = null;
            BoutNum = 0;
            LastBoutPlayerIndex = -1;
            curBoutPlayerIndex = 0;
            diZhuName = null;
            gameStatus = 4;
            status = 2;
            dealer = null;
            diZhuIndex.Clear();
        }
        public List<Dealer.Poker> GetDiZhuPokers( ) 
        {
            return dealer.diZhuPai;
        
        }

       
        
        Dictionary<string , Dictionary<int,List<Dealer.Poker>>> allPlayerSendPokers = new Dictionary<string , Dictionary<int , List<Dealer.Poker>>>();//所有玩家每轮已经出的牌
        void AddSendPokers( string username , List<Dealer.Poker> pokers ) 
        {
            if ( allPlayerSendPokers.ContainsKey(username) == false ) 
            {
                allPlayerSendPokers.Add(username , new Dictionary<int, List<Dealer.Poker>>());
            }
            if ( allPlayerSendPokers[username].ContainsKey(round) == false ) 
            {
                allPlayerSendPokers[username].Add(round , pokers);
            }
            allPlayerSendPokers[username][round] = pokers;
        }
        public List<Dealer.Poker> GetSendPokers( string username , int roundNum ) 
        {
            List<Dealer.Poker> pokers = null;
            if ( allPlayerSendPokers.ContainsKey(username) ) 
            {

                if ( allPlayerSendPokers[username].ContainsKey(roundNum) )
                {
                    pokers = allPlayerSendPokers[username][roundNum];
                }
            }
           

            return pokers;
        }
        //将包中的扑克数据转为Deal类中的扑克数据
        Dealer.Poker ParseDealPoker( Poker packPoker) 
        {
            Dealer.Poker poker = new Dealer.Poker();
            poker.weight = packPoker.Weight;
            poker.pokerColor = (Dealer.PokerColor) ( packPoker.Pokercolor - 1 );
            return poker;
        }

        //判断赢家
        public bool JudgeWinner(string username ) 
        {
            if ( dealer.allPlayerPoker[curRoomClientNames.IndexOf(username)].Count == 0 ) 
            {
                GameOver();
                
                return true;
            }
            return false;


        }
        //玩家出牌
        public bool PlayerSendPoker(string username,RepeatedField<Poker> pokers ,out Dealer.PokerTypeUtils typeUtils) 
        {
            if (  round > 2 && lastSendPokers == GetSendPokers(username , round - 1) ) lastSendPokers = null;
            if ( lastSendPokers !=null)
            Console.WriteLine(lastSendPokers[0].weight);

            typeUtils = Dealer.PokerTypeUtils.None;
            
            //非本回合不能操作
            if ( curRoomClientNames[curBoutPlayerIndex] != username ) return false;
         
            //一轮内其他玩家没有出牌（都要不起），自己不能不出牌
            if ( pokers == null && lastSendPokers == null ) return false;
            //本轮有上家出牌可以不出牌
            if ( pokers == null && lastSendPokers != null ) return true;
           

            List<Dealer.Poker> tempPokers = new List<Dealer.Poker>();
            foreach ( var item in pokers ) 
            {
                tempPokers.Add(ParseDealPoker(item));
            }
            Dealer.PokerType tmepPokerType = dealer.GetPokersType(tempPokers);
           
            //牌型不合法
            if ( tmepPokerType.porkerTypeUtils == Dealer.PokerTypeUtils.None && pokers != null ) return false;
            typeUtils = tmepPokerType.porkerTypeUtils;

            Console.WriteLine(1);
            List<Dealer.Poker> tempPlayerPokers = dealer.allPlayerPoker[curRoomClientNames.IndexOf(username)].ToList();
            if ( pokers != null ) 
            {
                foreach ( var item in dealer.allPlayerPoker[curRoomClientNames.IndexOf(username)] )
                {
                    foreach ( var item2 in tempPokers )
                    {
                        if ( item.weight == item2.weight && item.pokerColor == item2.pokerColor ) tempPlayerPokers.Remove(item);
                    }

                }
                Console.WriteLine(2);
                //玩家手牌不存在
                if ( tempPlayerPokers.Count != dealer.allPlayerPoker[curRoomClientNames.IndexOf(username)].Count - tempPokers.Count )
                    return false;
            }
            

            //一轮中其他玩家没有出牌，自己可以出任意符合牌型的牌
            if ( lastSendPokers == null )
            {
                Console.WriteLine(3);
                if ( tmepPokerType.porkerTypeUtils == Dealer.PokerTypeUtils.None ) return false;
            }
            //上家有出牌
            else 
            {
                Console.WriteLine(tmepPokerType.porkerTypeUtils+","+ dealer.GetPokersType(lastSendPokers).porkerTypeUtils);
                Console.WriteLine(BoutNum + "," + round);

                Dealer.PokerTypeUtils LastTypeUtils =  dealer.GetPokersType(lastSendPokers).porkerTypeUtils;

                //牌型和上家牌型判断
                //上家是王炸
                if ( LastTypeUtils == Dealer.PokerTypeUtils.KingBomb ) return false;
                //当前出的王炸不需要比牌
                else if ( tmepPokerType.porkerTypeUtils == Dealer.PokerTypeUtils.KingBomb ) { }
                //当前出的炸弹，上家不是炸弹，不需要比牌
                else if ( tmepPokerType.porkerTypeUtils == Dealer.PokerTypeUtils.Bomb && LastTypeUtils != Dealer.PokerTypeUtils.Bomb )
                {


                }
                //判断牌型是否合法
                else if ( tmepPokerType.porkerTypeUtils != dealer.GetPokersType(lastSendPokers).porkerTypeUtils ) return false;
                //上家不是炸弹，当前也不是炸弹
                else
                {

                    //比牌阶段
                    //比上家大
                    if ( dealer.ComparePoker(tempPokers , lastSendPokers) )
                    {
                        Console.WriteLine(5);

                        AddSendPokers(username , tempPokers); //将打出的牌存入容器

                    }
                    //比上家小
                    else
                    {
                        Console.WriteLine(6);
                        return false;

                    }

                }


            }
            dealer.allPlayerPoker[curRoomClientNames.IndexOf(username)] = tempPlayerPokers; //成功打出后从列表中删除打出的牌
            AddSendPokers(username , tempPokers); //将打出的牌存入容器
            lastSendPokers = tempPokers;
            return true;
        
        }

    }

    public class Dealer
    {
        //牌型
        public enum PokerTypeUtils
        {
            None,
            Single, //单张，一张任意牌
            Double,//对子，二张相同的牌
            Trebling,//三张，三张相同的牌
            TreblingWithOne,//三带一，三张相同的牌+一张任意牌
            TreblingWithTwo,//三带二，三张相同的牌+对子
            Bomb,//炸弹，四张相同的牌
            QuadrupleWithSingle,//四带一，四张相同的牌+2张任意牌
            QuadrupleWithDouble,//四带二，四张相同的牌+对子
            KingBomb,//王炸，大小王
            Straight,//顺子，5~12张单张，最小3，最大A
            ContinuousDouble,//连队，6~20张，至少连续3个对子(3~10对)，最小3，最大A
            Plane,//飞机，6~18张，至少连续两个3张
            PlaneWithSingle,
            PlaneWithDouble,
        }
        public class PokerType
        {
            public PokerTypeUtils porkerTypeUtils;
            public int weight;
        }
        public enum PokerColor
        {
            Meihua,
            Fangkuai,
            Hongtao,
            Heitao,
            Max
        }
        public class Poker
        {
            public int weight; //从3开始~15
            public PokerColor pokerColor;
        }


        Queue<Poker> forDealPorks;//洗牌后的牌堆
        List<Poker> allPork;//牌库
        public List<Poker> player1Pokers, player2Pokers, player3Pokers, diZhuPai,lastPlayerOutPokers;
        public List<List<Poker>> allPlayerPoker;
       

        public Dealer( ) 
        {
            forDealPorks = new Queue<Poker>();
            allPork = new List<Poker>();
            player1Pokers = new List<Poker>();
            player2Pokers = new List<Poker>();
            player3Pokers = new List<Poker>();
            diZhuPai = new List<Poker>();
            allPlayerPoker = new List<List<Poker>>();
            allPlayerPoker.Add(player1Pokers);
            allPlayerPoker.Add(player2Pokers);
            allPlayerPoker.Add(player3Pokers);


        }
        public void Initial( ) 
        {
            InitalPork();
            WashPork();

        }
        void InitalPork( )
        {
            //普通牌
            for ( int i = 0 ; i < (int) PokerColor.Max ; i++ )
            {
                for ( int j = 3 ; j < 16 ; j++ )
                {
                    Poker poker = new Poker();
                    poker.weight = j;
                    poker.pokerColor = (PokerColor) i;
                    allPork.Add(poker);
                }

            }
            //大小王

            Poker xiaowang = new Poker();
            xiaowang.weight = 100;
            xiaowang.pokerColor = PokerColor.Max;
            allPork.Add(xiaowang);

            Poker dawang = new Poker();
            dawang.weight = 101;
            dawang.pokerColor = PokerColor.Max;
            allPork.Add(dawang);

        }
        //洗牌
        public void WashPork( )
        {
            Random random = new Random();
            while ( true )
            {
                int index = random.Next(0 , 54);
                if ( forDealPorks.Contains(allPork[index]) ) continue;

                forDealPorks.Enqueue(allPork[index]);
                if ( forDealPorks.Count == 54 ) return;
            }


        } 

        //比牌
        public bool ComparePoker( List<Poker> compare , List<Poker> compared )
        {

            PokerType compareType = GetPokersType(compare);
            PokerType comparedType = GetPokersType(compared);

            if ( compareType.porkerTypeUtils != comparedType.porkerTypeUtils ) return false;

            if ( compareType.weight > comparedType.weight ) return true;

            return false;
        }
        //获取手牌类型和牌组权重
        public PokerType GetPokersType( List<Poker> pokers = null )
        {
            PokerType porkerType = new PokerType();
            porkerType.porkerTypeUtils = PokerTypeUtils.None;
            if ( pokers == null )
            {
                porkerType.weight = 0;
                return porkerType;
            }

            //权重作为key
            var tempDict = pokers.GroupBy(c => c.weight).ToDictionary(k => k.Key , k => k.Count());
            tempDict = ( from pair in tempDict orderby pair.Value descending select pair ).ToDictionary(k => k.Key , v => v.Value);

            //最大重复牌面数
            int MaxNum = tempDict.First().Value;
            //不同牌面的数量
            int TypeNum = tempDict.Count;

            switch ( MaxNum )
            {
                //当前手牌都是单张
                case 1:
                    {
                        //单张牌型
                        if ( TypeNum == 1 )
                        {
                            porkerType.weight = tempDict.First().Key;
                            porkerType.porkerTypeUtils = PokerTypeUtils.Single;

                        }
                        //王炸牌型
                        if ( TypeNum == 2 )
                        {
                            int tempWeight = 0;
                            foreach ( var item in tempDict )
                            {
                                tempWeight += item.Key;
                            }
                            if ( tempWeight == 201 )
                            {
                                porkerType.weight = tempWeight;
                                porkerType.porkerTypeUtils = PokerTypeUtils.KingBomb;
                            }

                        }

                        //顺子牌型
                        if ( TypeNum >= 5 && TypeNum <= 12 )
                        {
                            int tempWeight = 0;
                            bool isContinuous = true;

                            var sortList = ( from pair in tempDict orderby pair.Key ascending select pair ).ToList();

                            for ( int i = 0 ; i < sortList.Count - 1 ; i++ )
                            {
                                if ( sortList[i].Key + 1 != sortList[i + 1].Key )
                                {
                                    isContinuous = false;
                                    break;

                                }
                                tempWeight += sortList[i].Key;
                            }
                            if ( sortList.Last().Key > 14 ) isContinuous = false;
                            tempWeight += sortList.Last().Key;
                        
                            
                            if ( isContinuous )
                            {

                                porkerType.weight = tempWeight;
                                porkerType.porkerTypeUtils = PokerTypeUtils.Straight;

                            }

                        }
                        break;
                    }
                //当前手牌最大重复是两张
                case 2:
                    {
                        //单对牌型
                        if ( TypeNum == 1 )
                        {

                            int tempWeight = tempDict.First().Key * 2;
                            porkerType.weight = tempWeight;
                            porkerType.porkerTypeUtils = PokerTypeUtils.Double;
                        }
                        //连对牌型
                        if ( TypeNum >= 3 && TypeNum <= 12 )
                        {

                            int tempWeight = 0;
                            bool isContinuous = true;

                            var sortList = ( from pair in tempDict orderby pair.Key ascending select pair ).ToList();

                            for ( int i = 0 ; i < sortList.Count - 1 ; i++ )
                            {
                                if ( sortList[i].Key + 1 != sortList[i + 1].Key || sortList[i].Value != 2 )
                                {
                                    isContinuous = false;
                                    break;
                                }
                                tempWeight += sortList[i].Key;
                            }
                            if ( sortList.Last().Key > 14 ) isContinuous = false;
                            tempWeight += sortList.Last().Key;

                            if ( isContinuous )
                            {

                                porkerType.weight = tempWeight * 2;
                                porkerType.porkerTypeUtils = PokerTypeUtils.ContinuousDouble;

                            }
                        }

                        break;
                    }
                //当前手牌最大重复是三张
                case 3:
                    {
                        //三张牌型
                        if ( TypeNum == 1 )
                        {

                            porkerType.weight = tempDict.First().Key * 3;
                            porkerType.porkerTypeUtils = PokerTypeUtils.Trebling;

                        }
                        //三带一牌型
                        if ( TypeNum == 2 && tempDict.ElementAt(1).Value == 1 )
                        {

                            porkerType.weight = tempDict.First().Key * 3;
                            porkerType.porkerTypeUtils = PokerTypeUtils.TreblingWithOne;

                        }
                        //三对子牌型
                        if ( TypeNum == 2 && tempDict.ElementAt(1).Value == 2 )
                        {

                            porkerType.weight = tempDict.First().Key * 3;
                            porkerType.porkerTypeUtils = PokerTypeUtils.TreblingWithTwo;

                        }
                        //飞机牌型
                        if ( TypeNum >= 2 && TypeNum <= 6 )
                        {

                            bool isContinous = true;
                            int tempWeight = 0;

                            var sortList = ( from pair in tempDict orderby pair.Key ascending select pair ).ToList();

                            var singleList = new List<KeyValuePair<int , int>>();
                            var doubleList = new List<KeyValuePair<int , int>>();
                            var treblingList = new List<KeyValuePair<int , int>>();

                            foreach ( var item in sortList )
                            {
                                if ( item.Value == 1 ) singleList.Add(item);
                                else if ( item.Value == 2 ) doubleList.Add(item);
                                else treblingList.Add(item);
                            }

                            for ( int i = 0 ; i < treblingList.Count - 1 ; i++ )
                            {
                                if ( treblingList[i].Key + 1 != treblingList[i + 1].Key )
                                {
                                    isContinous = false;
                                    break;
                                }
                                tempWeight += treblingList[i].Key;
                            }

                            //最大到A
                            if ( treblingList.Last().Key > 14 ) isContinous = false;
                            tempWeight += treblingList.Last().Key;


                            if ( isContinous && singleList.Count == 0 && doubleList.Count == 0 )
                            {

                                porkerType.weight = tempWeight * 3;
                                porkerType.porkerTypeUtils = PokerTypeUtils.Plane;
                            }

                        }
                        //飞机带单张牌型
                        if ( TypeNum >= 4 && TypeNum <= 10 )
                        {


                            bool isContinous = true;
                            int tempWeight = 0;

                            var sortList = ( from pair in tempDict orderby pair.Key ascending select pair ).ToList();

                            var singleList = new List<KeyValuePair<int , int>>();
                            var doubleList = new List<KeyValuePair<int , int>>();
                            var treblingList = new List<KeyValuePair<int , int>>();

                            foreach ( var item in sortList )
                            {
                                if ( item.Value == 1 ) singleList.Add(item);
                                else if ( item.Value == 2 ) doubleList.Add(item);
                                else treblingList.Add(item);
                            }

                            for ( int i = 0 ; i < treblingList.Count - 1 ; i++ )
                            {
                                if ( treblingList[i].Key + 1 != treblingList[i + 1].Key )
                                {
                                    isContinous = false;
                                    break;
                                }
                                tempWeight += treblingList[i].Key;
                            }

                            //最大到A
                            if ( treblingList.Last().Key > 14 ) isContinous = false;

                            tempWeight += treblingList.Last().Key;

                            if ( isContinous && singleList.Count == treblingList.Count && doubleList.Count == 0 )
                            {

                                porkerType.weight = tempWeight * 3;
                                porkerType.porkerTypeUtils = PokerTypeUtils.PlaneWithSingle;
                            }

                        }
                        //飞机带对牌型
                        if ( TypeNum >= 4 && TypeNum <= 8 )
                        {
                            bool isContinous = true;
                            int tempWeight = 0;

                            var sortList = ( from pair in tempDict orderby pair.Key ascending select pair ).ToList();

                            var singleList = new List<KeyValuePair<int , int>>();
                            var doubleList = new List<KeyValuePair<int , int>>();
                            var treblingList = new List<KeyValuePair<int , int>>();

                            foreach ( var item in sortList )
                            {
                                if ( item.Value == 1 ) singleList.Add(item);
                                else if ( item.Value == 2 ) doubleList.Add(item);
                                else treblingList.Add(item);
                            }

                            for ( int i = 0 ; i < treblingList.Count - 1 ; i++ )
                            {
                                if ( treblingList[i].Key + 1 != treblingList[i + 1].Key )
                                {
                                    isContinous = false;
                                    break;
                                }
                                tempWeight += treblingList[i].Key;
                            }

                            //最大到A
                            if ( treblingList.Last().Key > 14 ) isContinous = false;

                            tempWeight += treblingList.Last().Key;

                            if ( isContinous && singleList.Count == 0 && doubleList.Count == treblingList.Count )
                            {

                                porkerType.weight = tempWeight * 3;
                                porkerType.porkerTypeUtils = PokerTypeUtils.PlaneWithDouble;
                            }

                        }

                        break;
                    }
                //当前最大重复是四张
                case 4:
                    {
                        //炸弹牌型
                        if ( TypeNum == 1 )
                        {

                            porkerType.weight = tempDict.First().Key * 4;
                            porkerType.porkerTypeUtils = PokerTypeUtils.Bomb;
                        }
                        //四带两单张 和 四带两对牌型
                        if ( TypeNum == 3 )
                        {
                            bool isSinglePass = true;
                            bool isDoublePass = true;

                            foreach ( var item in tempDict )
                            {
                                if ( item.Value == 2 ) isSinglePass = false;
                                if ( item.Value == 1 ) isDoublePass = false;

                            }


                            if ( isSinglePass )
                            {

                                porkerType.weight = tempDict.First().Key * 4;
                                porkerType.porkerTypeUtils = PokerTypeUtils.QuadrupleWithSingle;

                            }
                            if ( isDoublePass )
                            {
                                porkerType.weight = tempDict.First().Key * 4;
                                porkerType.porkerTypeUtils = PokerTypeUtils.QuadrupleWithDouble;

                            }

                        }

                        break;
                    }
                    
            }

            if ( porkerType.porkerTypeUtils == PokerTypeUtils.None ) porkerType.weight = 0;

            return porkerType;

        }
        //牌组排序（升序）
        public void SortPork( List<Poker> pokers )
        {
            if ( pokers.Count == 0 ) return;
            //按权重分类
            Dictionary<int , List<Poker>> tempDict = new Dictionary<int , List<Poker>>();

            foreach ( var item in pokers )
            {
                if ( !tempDict.ContainsKey(item.weight) )
                {

                    tempDict.Add(item.weight , new List<Poker>());

                }
                tempDict[item.weight].Add(item);
            }
            var sortResult1 = ( from pair in tempDict orderby pair.Key ascending select pair ).ToDictionary(k => k.Key , v => v.Value);

            tempDict.Clear();


            foreach ( var item in sortResult1 )
            {
                item.Value.Sort(( a , b ) => (int) a.pokerColor <= (int) b.pokerColor ? -1 : 1);
                tempDict.Add(item.Key , item.Value);
            }
            pokers.Clear();
            foreach ( var item in tempDict )
            {
                foreach ( var item1 in item.Value )
                {
                    pokers.Add(item1);

                }

            }
        }

        //发牌
        public void DealPorker( ) 
        {
            int i = 0;
            while ( forDealPorks.Count > 3 )
            {
                Poker temp = forDealPorks.Dequeue();
                

                switch ( i++ % 3 ) 
                {
                    case 0:
                        player1Pokers.Add(temp);
                        break;
                    case 1:
                        player2Pokers.Add(temp);
                        break;
                    case 2:
                        player3Pokers.Add(temp);
                        break;

                }
            }
            while ( forDealPorks.Count != 0 )
            {

                diZhuPai.Add(forDealPorks.Dequeue());

            }
            SortPork(player1Pokers);
            SortPork(player2Pokers);
            SortPork(player3Pokers);
            SortPork(diZhuPai);

        }

        //清理
        public void Clear( ) 
        {
            player1Pokers.Clear();
            player2Pokers.Clear();
            player3Pokers.Clear();
            diZhuPai.Clear();
            forDealPorks.Clear();
        }







    }
   
}
