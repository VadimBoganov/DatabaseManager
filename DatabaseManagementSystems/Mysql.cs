using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace DatabaseManager.DatabaseManagementSystems
{
    public class Mysql : IDatabaseManagementSystem
    {
        public string Name { get; set; }
        public List<Connection> Connections { get; set; }

        public void DoActions()
        {
            List<Task> tasks = new List<Task>();

            foreach(var connection in Connections)
            {
                MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder()
                {
                    Server = connection.Server,
                    Port = (uint)connection.Port,
                    UserID = connection.User,
                    Password = connection.Password,
                    SslMode = MySqlSslMode.None,
                    Database = connection.DatabaseName
                };

                MySqlConnection conn = CreateConnection(connectionStringBuilder);

                foreach(var action in connection.Actions)
                {
                    Type thisType = this.GetType();
                    MethodInfo theMethod = thisType.GetMethod(action.Name);
                    Task task = Task.Factory.StartNew(() =>
                        theMethod.Invoke(this, new object[] { action, conn })
                    );
                    tasks.Add(task);
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        public MySqlConnection CreateConnection(MySqlConnectionStringBuilder connectionStringBuilder)
        {
            MySqlConnection conn = new MySqlConnection();
            conn.ConnectionString = connectionStringBuilder.ToString();

            try
            {
                conn.Open();
                return conn;
            }
            catch
            {
                throw new System.Exception("Can't connect to server " + connectionStringBuilder.Server);
            }
        }

        public void Delete(TableAction action, MySqlConnection conn)
        {
            const int ONEFULLDAY = 24;

            try
            {
                DateTime firstMessageTime = GetFirstMessageTime(action, conn);

                //получение разницы в часах между текущим временем и полученым из БД
                TimeSpan ts = DateTime.Now - firstMessageTime;
                double intervalTime = Math.Round(ts.TotalHours);

                int limitHours = Convert.ToInt32(action.HistoryDay) * ONEFULLDAY;

                //удаление с шагом в IntervalHours часов
                while (intervalTime > limitHours)
                {
                    intervalTime -= Convert.ToDouble(action.IntervalHours);
                    var querystring = String.Format(@"DELETE t1 FROM {0} t1 INNER JOIN (SELECT `key` FROM {0} WHERE timeUtc < DATE_SUB(NOW(), INTERVAL {1} HOUR) ORDER BY `key`) t2 USING(`key`)",
                       action.TableName, intervalTime);

                    using (MySqlCommand command = new MySqlCommand(querystring, conn))
                    {
                        command.CommandTimeout = Convert.ToInt32(action.Timeout);
                        command.ExecuteNonQuery();
                    }
                }
                ConsoleLogger.Write(LogStatus.Info, "Deleted from " + conn.Database  + " gps_mes_archive in " + DateTime.Now.ToString());

                ConsoleLogger.Write(LogStatus.Info, "Start optimize " + conn.Database);
                OptimizeTable(action, conn);
                ConsoleLogger.Write(LogStatus.Info, "Optimized table " + action.TableName);

                conn.Close();
            }
            catch(Exception ex)
            {
                ConsoleLogger.Write(LogStatus.Error, ex.Message);
            }
        }

        public DateTime GetFirstMessageTime(TableAction action, MySqlConnection conn)
        {
            DateTime firstMessageTime = DateTime.MinValue;

            using (MySqlCommand command = new MySqlCommand(String.Format(@"SELECT timeUtc FROM {0} ORDER BY `key` LIMIT 1",
                action.TableName), conn))
            {
                command.CommandTimeout = Convert.ToInt16(action.Timeout);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.HasRows && reader.Read())
                        firstMessageTime = reader.GetDateTime(0);

                    reader.Close();

                    if(firstMessageTime > DateTime.Now.Date.AddDays(1))
                    {
                        ClearInvalidMessages(action, conn);
                        firstMessageTime = GetFirstMessageTime(action, conn);
                    }

                    ConsoleLogger.Write(LogStatus.Info, "First time in " + conn.Database + "= " + firstMessageTime);
                    return firstMessageTime;
                }
            }
        }

        public void OptimizeTable(TableAction action, MySqlConnection conn)
        {
            using(MySqlCommand command = new MySqlCommand(String.Format(@"OPTIMIZE TABLE {0}", action.TableName), conn))
            {
                command.CommandTimeout = int.MaxValue;
                command.ExecuteNonQuery();
            }
        }

        public void ClearInvalidMessages(TableAction action, MySqlConnection conn)
        {
            using (MySqlCommand command = new MySqlCommand(String.Format(@"DELETE FROM {0} WHERE timeUtc > DATE(NOW()) + INTERVAL 1 DAY - INTERVAL 1 SECOND",
                 action.TableName), conn))
            {
                command.CommandTimeout = Convert.ToInt16(action.Timeout);
                command.ExecuteNonQuery();
            }
        }
    }
}