using System;
using System.Collections.Generic;
using DatabaseManager.DatabaseManagementSystems;

namespace DatabaseManager
{
    class Manager
    {
        static void Main(string[] args)
        {
            try
            {
                Deserializer deserializer = new Deserializer("config.xml");
                var systems = deserializer.Deserialize<List<IDatabaseManagementSystem>>();

                foreach(var system in systems)
                    system.DoActions();

            }
            catch(Exception ex)
            {
                ConsoleLogger.Write(LogStatus.Error, ex.Message);
            }
        }
    }
}
