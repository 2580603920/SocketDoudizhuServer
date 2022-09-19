using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;
using System.Threading;
using Google.Protobuf.Collections;

namespace SocketDoudizhuServer.Controller
{
    class RoomController:ControllerBase
    {
        public RoomController( ) : base() { }
        public Dictionary<string,Room> allRoom;
        
        public override void Initial( )
        {
            base.Initial();
            requestCode = RequestCode.Room;
            ControllerManager.Instance.AddController(requestCode , this);
            allRoom = new Dictionary<string , Room>();
            
        }
        public void RegisterRoom(string roomID, Room room ) 
        {
            if ( allRoom.ContainsKey(roomID) ) 
            {
                Console.WriteLine("房间id已存在");
            }
            allRoom.Add(roomID , room);
        }
        public void UnRegisterRoom( string roomID)
        {
            if ( !allRoom.ContainsKey(roomID) ) return;

            allRoom.Remove(roomID);

        }
        public override void HandleRequest( MainPack pack , Client client )
        {
           
            switch ( pack.Actioncode )
            {
                case ActionCode.GetRoomList:
                    {
                        MainPack returnPack = new MainPack();
                        returnPack.Requestcode = pack.Requestcode;
                        returnPack.Actioncode = pack.Actioncode;

                        GetRoomList(returnPack.Roominfo);
                        client.Send(returnPack);
                        break;

                    }
                case ActionCode.CreateRoom:
                    {
                        MainPack returnPack = new MainPack();
                        returnPack.Requestcode = pack.Requestcode;
                        returnPack.Actioncode = pack.Actioncode;

                        RoomInfo roomInfo = new RoomInfo();
                        string roomID;
                       
                        if ( CreateRoom(client , pack.Roominfo[0].Roomtile, out roomID) )
                        {

                            returnPack.Returncode = ReturnCode.Success;
                            roomInfo.Roomid = roomID;
                        }
                        else
                            returnPack.Returncode = ReturnCode.Fail;
                        
                       
                        returnPack.Roominfo.Add(roomInfo);
                        client.Send(returnPack);
                        break;

                    }
                case ActionCode.JoinRoom:
                    {
                        MainPack returnPack = new MainPack();
                        returnPack.Requestcode = pack.Requestcode;
                        returnPack.Actioncode = pack.Actioncode;

                        returnPack.Returncode = JoinRoom(pack.Roominfo[0].Roomid , client);
                        RoomInfo info = new RoomInfo();
                        info.Roomid = pack.Roominfo[0].Roomid;
                        returnPack.Roominfo.Add(info);
                        client.Send(returnPack);

                        //通知房间的其他客户端更新
                        MainPack pack1 = new MainPack();
                        pack1.Requestcode = requestCode;
                        pack1.Actioncode = ActionCode.GetRoomInfo;
                        RoomInfo info1 = new RoomInfo();
                        info.Roomid = pack.Roominfo[0].Roomid;

                        foreach ( var item in GetRoomInfo(pack.Roominfo[0].Roomid) )
                        {
                            info1.Playerinfo.Add(item);


                        }
                        pack1.Roominfo.Add(info1);

                        allRoom[pack.Roominfo[0].Roomid].BoredCast(pack1 , client);
                        break;

                    }
                case ActionCode.GetRoomInfo:
                    {
                        MainPack returnPack = new MainPack();
                        returnPack.Requestcode = pack.Requestcode;
                        returnPack.Actioncode = pack.Actioncode;

                        RoomInfo info = new RoomInfo();
                        info.Roomid = pack.Roominfo[0].Roomid;
                        
                        foreach ( var item in GetRoomInfo(pack.Roominfo[0].Roomid) )                         
                        {
                            info.Playerinfo.Add(item);

                        }
                       
                       
                        returnPack.Roominfo.Add(info);
                        client.Send(returnPack);
                        break;
                    }
                case ActionCode.ExitRoom: 
                    {
                        ExitRoom(client , pack);
                        break;
                    }
            }
        }
        RepeatedField<PlayerInfo> GetRoomInfo(string roomId ) 
        {
          
               return allRoom[roomId].GetClientInfo();
        }
        public void GetRoomList( RepeatedField<RoomInfo> roomInfos) 
        {

         
            foreach ( var item in allRoom ) 
            {
                RoomInfo roomInfo = new RoomInfo();
                roomInfo.Roomtile = item.Value.roomTitle;
                roomInfo.Playernum = item.Value.curClientNum;
                roomInfo.Landlord = item.Value.hostname;
                roomInfo.Roomid = item.Value.roomID;             
                roomInfo.Status = item.Value.status;
              
                roomInfos.Add(roomInfo);
            }

          
        }

        //退出房间
        public void  ExitRoom(Client client,MainPack pack ) 
        {

            MainPack returnPack = new MainPack();
            returnPack.Requestcode = pack.Requestcode;
            returnPack.Actioncode = pack.Actioncode;

            bool isExist = false;
            if ( allRoom.ContainsKey(pack.Roominfo[0].Roomid) && allRoom[pack.Roominfo[0].Roomid].Exit(client , out isExist) )
            {
                returnPack.Returncode = ReturnCode.Success;

                //通知房间的其他客户端更新
                if ( isExist )
                {


                    MainPack pack1 = new MainPack();
                    pack1.Requestcode = requestCode;
                    pack1.Actioncode = ActionCode.GetRoomInfo;
                    pack1.Returncode = ReturnCode.Success;
                    RoomInfo info = new RoomInfo();
                    info.Roomid = pack.Roominfo[0].Roomid;

                    foreach ( var item in GetRoomInfo(pack.Roominfo[0].Roomid) )
                    {
                        info.Playerinfo.Add(item);


                    }
                    pack1.Roominfo.Add(info);

                    allRoom[pack.Roominfo[0].Roomid].BoredCast(pack1 , client);
                }

            }
            else
                returnPack.Returncode = ReturnCode.Fail;

            client.Send(returnPack);

        }

        public bool CreateRoom( Client client,string roomTitle,out string roomID ) 
        {
            roomID = null;
            if ( client.room != null ) return false;      
            while ( true ) 
            {
                roomID = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName());
                if ( !allRoom.ContainsKey(roomID) ) 
                {
                    break;
                }
            }

           Room room = new Room(client, roomID, roomTitle);            
           return true;
        }
        public ReturnCode JoinRoom(string roomID , Client client ) 
        {
            return allRoom[roomID].Join(client);                      
        }
       
    }
}
