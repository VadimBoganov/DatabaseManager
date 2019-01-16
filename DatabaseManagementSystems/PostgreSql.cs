using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace DatabaseManager.DatabaseManagementSystems
{
    public class PostgreSQL : IDatabaseManagementSystem
    {
        public string Name { get; set; }
        public List<Connection> Connections { get ; set ; }

        public void DoActions()
        {

        }
    }
}