using System;

namespace General_PCR18.DB
{
    public class User
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement, SQLite.Column("id")]
        public long Id { get; set; }

        [SQLite.Column("username")]
        public string Username { get; set; }

        [SQLite.Column("password")]
        public string Password { get; set; }

        /// <summary>
        /// 角色：1 管理员  2 医生
        /// </summary>
        [SQLite.Column("role")]
        public int Role { get; set; }

        [SQLite.Column("name")]
        public string Name { get; set; }

        [SQLite.Column("create_time")]
        public DateTime CreateTime { get; set; }
    }
}
