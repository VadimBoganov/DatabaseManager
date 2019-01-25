using System;
using System.Collections.Generic;
using System.Threading;
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

                // foreach(var system in systems)
                //     system.DoActions();


                while(true)
                {
                    try
                    {
                        if(DateTime.Now.Date.AddDays(1).AddMinutes(5) > DateTime.Now && DateTime.Now > DateTime.Now.Date.AddDays(1).AddMinutes(-5))
                        {
                            foreach(var system in systems)
                                system.DoActions();
                            Thread.Sleep(600000);
                        }
                    }
                    catch(Exception ex)
                    {
                        ConsoleLogger.Write(LogStatus.Error, ex.Message);
                    }
                    finally
                    {
                        Thread.Sleep(10000);
                    }
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.Write(LogStatus.Error, ex.Message);
            }
        }
    }
}
