using System;

namespace General_PCR18.DB
{
    public class Patient
    {

        [SQLite.PrimaryKey, SQLite.AutoIncrement, SQLite.Column("id")]
        public long Id { get; set; }

        [SQLite.Column("name")]
        public string Name { get; set; }

        [SQLite.Column("patient_id")]
        public string PatientId { get; set; }

        /// <summary>
        /// 性别：1 男  2 女
        /// </summary>
        [SQLite.Column("gender")]
        public int Gender { get; set; }

        [SQLite.Column("birthday")]
        public string Birthday { get; set; }

        [SQLite.Column("address")]
        public string Address { get; set; }

        [SQLite.Column("phone")]
        public string Phone { get; set; }

        [SQLite.Column("create_time")]
        public DateTime CreateTime { get; set; }
    }
}
