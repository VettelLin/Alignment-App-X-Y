using System.Collections.Generic;

namespace General_PCR18.DB
{
    public class SampleDAL : BaseDAL
    {
        public List<Sample> GetList()
        {
            return Execute((conn) =>
            {
                var list = conn.Table<Sample>().ToList();
                return list;
            });
        }

    }
}
