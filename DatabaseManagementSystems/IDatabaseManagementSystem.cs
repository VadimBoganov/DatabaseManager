using System;
using System.Collections.Generic;



namespace DatabaseManager.DatabaseManagementSystems
{
    interface IDatabaseManagementSystem
    {
        string Name{get;set;}
        List<Connection> Connections{get;set;}
        void DoActions();
    }
}