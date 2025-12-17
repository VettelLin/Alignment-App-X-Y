using SQLite;
using System;

namespace General_PCR18.DB
{
    public class BaseDAL
    {
        public static string DbFile
        {
            get { return Environment.CurrentDirectory + "\\Database\\pcr18.db"; }
        }
        protected string ConnectionString { get; set; } = DbFile;

        public void SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public T Execute<T>(Func<SQLiteConnection, T> func)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                return func(connection);
            }
        }

        /// <summary>
        /// 支持事务
        /// </summary>
        /// <typeparam name="T"></typeparam> 
        /// <param name="func"></param>
        /// <returns></returns>
        //public T Execute<T>(Func<SQLiteConnection, SQLiteTransaction, T> func)
        //{
        //    using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
        //    {
        //        connection.Open();
        //        using (var transaction = connection.BeginTransaction())
        //        {
        //            return func(connection, transaction);
        //        }
        //    }
        //}
    }
}
