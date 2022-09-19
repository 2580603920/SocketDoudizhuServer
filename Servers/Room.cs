using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using Google.Protobuf.Collections;
using SocketDoudizhuServer.Controller;
using System.Linq;

namespace SocketDoudizhuServer.Servers
{
    class Room
    {
        public List<Client> clients;
        public RoomController roomController;
        Dealer dealer;

        //通信数据
        public string hostname;
        public int maxClientNum=3;
        public int curClientNum=0;
        public string roomID;
        public string roomTitle;
        public int status;
       
        
        public Room(Client host ,string roomID,string roomTitle) 
        {
            roomController = ControllerManager.Instance.GetController<RoomController>(RequestCode.Room);
            roomController.RegisterRoom(roomID , this);
            clients = new List<Client>();
            this.roomID = roomID;
            this.roomTitle = roomTitle;
            hostname = host.username;
            host.room = this;
            Join(host);
            status = 1;
            
        }
        public ReturnCode Join( Client client )
        {
          
            if ( clients.Contains(client)) return ReturnCode.Fail;
            if ( curClientNum + 1 > maxClientNum ) 
            {
                status = 2;
                return ReturnCode.RoomFull;                
            }
            client.room = this;
             curClientNum++;
            clients.Add(client);
           
            return ReturnCode.Success;
        }
        public void  Destroy( ) 
        {
            foreach ( var item in clients ) 
            {
              
                if ( item.room == null ) continue;
                item.room = null;
            
            }

            clients.Clear();
            roomController.UnRegisterRoom(roomID);
        }
        public void ReplaceClient(Client oldClient,Client newcCient ) 
        {
            clients[clients.IndexOf(oldClient)] = newcCient;

        }
        public bool Exit(Client client,out bool isExist ) 
        {
            isExist = true;
            if ( status == 3 ) return false;
            if ( !clients.Contains(client) ) return true;

            curClientNum--;
            clients.Remove(client);
            client.room = null;

            //房主退出房间
            if ( client.username == hostname || curClientNum <= 0 ) 
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
                BoredCast(pack1,client);
                Destroy();

            } 
            
            return true;
        }
        public RepeatedField<PlayerInfo> GetClientInfo() 
        {
            RepeatedField<PlayerInfo> playerInfos = new RepeatedField<PlayerInfo>();
            foreach ( var item in clients ) 
           
            {
              
                PlayerInfo info = new PlayerInfo();
                info.Username = item.username;
                info.Coin = "1000";
                info.Status = item.state;
                playerInfos.Add(info);
              
            }

            return playerInfos;

        }
        public void BoredCast(MainPack pack,Client ignoreClient=null ) 
        {
            foreach ( var item in clients )
            {
                if ( item.Equals(ignoreClient) || item == null) continue;
                item.Send(pack);
            }

        }
        public bool StartGame( ) 
        {
            if ( status == 3  ) return false;
            status = 3;
            dealer = new Dealer();
            dealer.Initial();

            return true;
        }
        public void DealPorker( ) 
        {
            dealer.DealPorker();

            for ( int i = 0 ; i < clients.Count ; i++ ) 
            {
                //MainPack pack = new MainPack();
                //pack.Requestcode = RequestCode.Game;
                //pack.Actioncode = ActionCode
                //dealer.allPlayerPoker[i];
                //clients[i].Send();
            
            }
           
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

            PokerType compareType = GetType(compare);
            PokerType comparedType = GetType(compared);

            if ( compareType.porkerTypeUtils != comparedType.porkerTypeUtils ) return false;

            if ( compareType.weight > comparedType.weight ) return true;

            return false;
        }
        //获取手牌类型和牌组权重
        PokerType GetType( List<Poker> pokers = null )
        {
            PokerType porkerType = new PokerType();
            if ( pokers == null )
            {
                porkerType.porkerTypeUtils = PokerTypeUtils.None;
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
                            if ( sortList.Last().Key > 12 ) isContinuous = false;
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
                        if ( TypeNum >= 3 && TypeNum <= 10 )
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
                            if ( sortList.Last().Key > 12 ) isContinuous = false;
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
                            if ( treblingList.Last().Key > 12 ) isContinous = false;
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
                            if ( treblingList.Last().Key > 12 ) isContinous = false;

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
                            if ( treblingList.Last().Key > 12 ) isContinous = false;

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
                while ( forDealPorks.Count != 0 )
                {
                                      
                    diZhuPai.Add(forDealPorks.Dequeue());

                }

            }
            
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
