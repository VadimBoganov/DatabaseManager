using System;

namespace DatabaseManager.DatabaseManagementSystems
{
    public class TableAction
    {
        public string Name{get;set;}
        public string TableName{get;set;}
        public short IntervalHours{get;set;}
        public short HistoryDay{get;set;}
        public int Timeout{get;set;}
        public int UpdateTimeInMin{get;set;}
    }
}