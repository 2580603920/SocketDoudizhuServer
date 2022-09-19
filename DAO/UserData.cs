using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace SocketDoudizhuServer.DAO
{
    class UserData
    {
        MySqlCommand mySqlCommand;
       
        //登录
        public bool Login(MySqlConnection mySqlConnection ,string username,string password) 
        {
            string loginCommand = "SELECT * FROM userdata WHERE username='"+ username + "' AND password='"+password+"';";
            MySqlDataReader reader;
            try
            {
                mySqlCommand = new MySqlCommand(loginCommand , mySqlConnection);
                 reader = mySqlCommand.ExecuteReader();

                if ( reader.Read() )
                {

                    //登录成功
                    reader.Close();
                    return true;
                }
                reader.Close();
                return false;
            }
            catch ( Exception e )
            {
                Console.WriteLine(e.Message);
                return false;
            }
            
        }

        //注册
        public bool Register( MySqlConnection mySqlConnection , string username , string password )
        {
            
            string loginCommand = "Insert Into userdata (username,password) VALUES ('" + username + "','" + password + "')";
            
            try
            {
                mySqlCommand = new MySqlCommand(loginCommand , mySqlConnection);
               

                if ( mySqlCommand.ExecuteNonQuery()>0 )
                {

                   
                    
                    return true;
                }
               
                return false;
            }
            catch ( Exception e )
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }


    }
}
