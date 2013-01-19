using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;


//
// ripped from http://www.codeproject.com/KB/database/ConnectCsharpToMysql.aspx
//
// THIS IS NOT BEIBNG USED AT THE MOMENT - NO DB BEING USED CURRENTLY
//
namespace bloodhound
{

    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        //Constructor
        public DBConnect()
        {
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            //server = "10.92.16.94 ";
            //database = "loyalty_prod";
            //uid = "loyalty_prod_ro";
            //password = "yodwashGem";
            server = "localhost";
            database = "loyalty_prodV2";
            uid = "root";
            password = "password";

            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }


        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Console.Error.WriteLine("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Console.Error.WriteLine("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }
        }

    }
}







