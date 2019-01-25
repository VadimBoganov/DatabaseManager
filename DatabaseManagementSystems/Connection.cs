using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace DatabaseManager.DatabaseManagementSystems
{
    public class Connection
    {
        public string Server{get;set;}
        public int Port{get;set;}
        public string User{get;set;}
        public string Password{get;set;}
        public string DatabaseName{get;set;}
        public List<TableAction> Actions{get;set;}

    }
}