using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Threading.Tasks;

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
                    SslMode = MySqlSslMode.None
                };

                MySqlConnection conn = CreateConnection(connectionStringBuilder);

                // можно было в action указывать имя БД, но для одной БД может быть несколько action, что удобно при настройке
                // так же можно доработать конфиг - указать кодировку
                foreach(var db in connection.Databases)
                {
                    conn.ChangeDatabase(db.Name);

                    foreach(var action in db.Actions)
                    {
                        Type thisType = this.GetType();
                        MethodInfo theMethod = thisType.GetMethod(action.Name);
                        Task task = Task.Factory.StartNew(() =>
                            theMethod.Invoke(this, new object[] {db, action, conn})
                        );
                        tasks.Add(task);
                    }
                }
                Task.WaitAll(tasks.ToArray());

                conn.Close();
            }
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

        public void Delete(Database db, TableAction action, MySqlConnection conn)
        {
            const int ONEFULLDAY = 24;

            try
            {
                DateTime firstMessageTime = GetFirstMessageTime(db, action, conn);

                //получение разницы в часах между текущим временем и полученым из БД
                TimeSpan ts = DateTime.Now - firstMessageTime;
                double intervalTime = Math.Round(ts.TotalHours);

                int limitHours = Convert.ToInt32(action.HistoryDay) * ONEFULLDAY;

                //удаление с шагом в IntervalHours часов
                while (intervalTime > limitHours)
                {
                    intervalTime -= Convert.ToDouble(action.IntervalHours);
                    var querystring = String.Format(@"DELETE t1 FROM {0}.{1} t1 INNER JOIN (SELECT `key` FROM {0}.{1} WHERE timeUtc < DATE_SUB(NOW(), INTERVAL {2} HOUR) ORDER BY `key`) t2 USING(`key`)",
                        db.Name, action.TableName, intervalTime);

                    using (MySqlCommand command = new MySqlCommand(querystring, conn))
                    {
                        command.CommandTimeout = Convert.ToInt32(action.Timeout);
                        command.ExecuteNonQuery();
                    }
                }
                ConsoleLogger.Write(LogStatus.Info, "Deleted from " + db.Name + " gps_mes_archive in " + DateTime.Now.ToString());

                OptimizeTable(db, action, conn);
                ConsoleLogger.Write(LogStatus.Info, "Optimized table " + action.TableName);

            }
            catch(Exception ex)
            {
                ConsoleLogger.Write(LogStatus.Error, ex.Message);
            }
        }

        public void OptimizeTable(Database db, TableAction action, MySqlConnection conn)
        {
            using(MySqlCommand command = new MySqlCommand(String.Format(@"OPTIMIZE TABLE {0}.{1}",db.Name, action.TableName), conn))
            {
                command.CommandTimeout = action.Timeout;
                command.ExecuteNonQuery();
            }
        }

        public DateTime GetFirstMessageTime(Database db, TableAction action, MySqlConnection conn)
        {
            DateTime firstMessageTime = DateTime.MinValue;

            using (MySqlCommand command = new MySqlCommand(String.Format(@"SELECT timeUtc FROM {0}.{1} ORDER BY `key` LIMIT 1",
                db.Name, action.TableName), conn))
            {
                command.CommandTimeout = Convert.ToInt16(action.Timeout);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.HasRows && reader.Read())
                        firstMessageTime = reader.GetDateTime(0);

                    reader.Close();

                    if(firstMessageTime > DateTime.Now.Date.AddDays(1))
                    {
                        ClearInvalidMessages(db, action, conn);
                        firstMessageTime = GetFirstMessageTime(db, action, conn);
                    }

                    ConsoleLogger.Write(LogStatus.Info, "First time in " + db.Name + "= " + firstMessageTime);
                    return firstMessageTime;
                }
            }
        }

        void ClearInvalidMessages(Database db, TableAction action, MySqlConnection conn)
        {
            using (MySqlCommand command = new MySqlCommand(String.Format(@"DELETE FROM {0}.{1} WHERE timeUtc > DATE(NOW()) + INTERVAL 1 DAY - INTERVAL 1 SECOND",
                db.Name, action.TableName), conn))
            {
                command.CommandTimeout = Convert.ToInt16(action.Timeout);
                command.ExecuteNonQuery();
            }
        }
    }
}