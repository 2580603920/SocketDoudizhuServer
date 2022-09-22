using System;
using System.Collections.Generic;
using System.Text;
using SoketDoudizhuProtocol;
using SocketDoudizhuServer.Servers;
using MySql.Data.MySqlClient;


namespace SocketDoudizhuServer.Controller
{
    class UserController : ControllerBase
    {

        public UserController( ) : base() { }
        public override void Initial( )
        {
            base.Initial();
            requestCode = RequestCode.User;
            ControllerManager.Instance.AddController(requestCode , this);
        }
        public override void HandleRequest( MainPack pack , Client client )
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

        //登录
        public void Login( Client client , MainPack pack )
        {
            MainPack returnPack = new MainPack();
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;
            returnPack.Actioncode = pack.Actioncode;
            returnPack.Requestcode = pack.Requestcode;


            if ( GetUserdata().Login(client.sqlConnection , username , password) )
            {


                returnPack.Returncode = ReturnCode.Success;
                LoginPack loginPack = new LoginPack();
                loginPack.Username = username;
                returnPack.Loginpack = loginPack;

                Client lastClient = GetClient(username);//上次登入的CLient

                if ( lastClient != null)

                {
                    Console.WriteLine("重连");
                                     
                    client.room = lastClient.room;


                    if ( lastClient.room != null ) 
                    {
                        RoomInfo roominfo = new RoomInfo();
                        roominfo.Roomid = lastClient.room.roomID;
                        returnPack.Roominfo.Add(roominfo);
                    }
                    client.isLogin = true;
                    client.username = lastClient.username;
                    client.state = lastClient.state;

                    controllerManager.RemoveClient(client.username);
                    controllerManager.AddClient( client);
                }
                else
                {
                    //首次登录
                    client.username = username;
                    client.isLogin = true;
                    controllerManager.AddClient(client);
                }

            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
         
            }
            client.Send(returnPack);

        }
        //注册
        public void Register( Client client , MainPack pack )
        {
            MainPack returnPack = new MainPack();
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;
            returnPack.Actioncode = pack.Actioncode;
            returnPack.Requestcode = pack.Requestcode;

            if ( GetUserdata().Register(client.sqlConnection , username , password) )
            {
                returnPack.Returncode = ReturnCode.Success;

            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
               
            }

            client.Send(returnPack);
        }
       
    }
}
