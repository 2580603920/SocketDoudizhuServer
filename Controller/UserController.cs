using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;
using MySql.Data.MySqlClient;


namespace SocketDoudizhuServer.Controller
{
    class UserController: ControllerBase
    {

        public UserController( ):base() { }
        public override void Initial( )
        {
            base.Initial();
            requestCode = RequestCode.User;
            ControllerManager.Instance.AddController(requestCode , this);
        }
        public override void HandleRequest( MainPack pack, Client client)
        {
            //base.HandleRequest(pack);

            switch ( pack.Actioncode )
            {
                case ActionCode.Login:
                    {
                        Login(client , pack);
                        break;
                    }
                case ActionCode.Register:
                    {

                        Register(client , pack);
                        break;
                    }
            }
        }
       
        public void Login(Client client,MainPack pack) 
        {
            MainPack returnPack = new MainPack();
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;
            returnPack.Actioncode = pack.Actioncode;
            returnPack.Requestcode = pack.Requestcode;

            if ( server.userData.Login(client.sqlConnection , username , password) )
            {


                returnPack.Returncode = ReturnCode.Success;
                LoginPack loginPack = new LoginPack();
                loginPack.Username = username;
                returnPack.Loginpack = loginPack;
               

                if ( server.allClient.ContainsKey(username) )

                {
                    Console.WriteLine("重连");                   
                    
                    if ( server.allClient[username].room != null ) 
                    {
                        Console.WriteLine("in room");
                        client.room = server.allClient[username].room;
                        RoomInfo roominfo = new RoomInfo();
                        roominfo.Roomid = client.room.roomID;
                        returnPack.Roominfo.Add(roominfo);
                        client.room.ReplaceClient(server.allClient[username] , client);
                  
                    }

                    client.isLogin = true;
                    client.username = server.allClient[username].username;
                    server.allClient[username] = client;
                    server.allTempClient.Remove(client);
                   
                }
                else
                {
                    //首次登录
                    server.allTempClient.Remove(client);
                    client.username = username;
                    client.isLogin = true;
                    server.allClient.Add(username , client);

                }

                server.allClient[username].Send(returnPack);

            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
                client.Send(returnPack);

            }
           
   
        
        }
        public void Register( Client client , MainPack pack )
        {
            MainPack returnPack = new MainPack();
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;
            returnPack.Actioncode = pack.Actioncode;
            returnPack.Requestcode = pack.Requestcode;

            if ( server.userData.Register(client.sqlConnection , username , password))
            {


                returnPack.Returncode = ReturnCode.Success;

                client.Send(returnPack);

            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
                client.Send(returnPack);

            }
         

        }
    }
}
