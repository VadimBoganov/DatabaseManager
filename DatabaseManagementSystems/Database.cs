using System;
using System.Collections.Generic;

namespace DatabaseManager.DatabaseManagementSystems
{
    public class Database
    {
        public string Name{get;set;}
        public List<TableAction> Actions {get;set;}
    }
}