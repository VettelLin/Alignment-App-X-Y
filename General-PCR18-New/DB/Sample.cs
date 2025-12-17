using System;

namespace General_PCR18.DB
{
    public class Sample
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement, SQLite.Column("id")]
        public long Id { get; set; }

        [SQLite.Column("create_time")]
        public DateTime CreateTime { get; set; }
    }
}
