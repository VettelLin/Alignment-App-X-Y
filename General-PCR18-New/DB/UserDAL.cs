using System.Collections.Generic;

namespace General_PCR18.DB
{
    public class UserDAL : BaseDAL
    {
        public List<User> GetList()
        {
            return Execute((conn) =>
            {
                var result = conn.Table<User>().ToList();
                return result;
            });
        }


        public User FindByUsername(string username)
        {
            return Execute((conn) =>
            {
                //var result = conn.Query<User>("select * from user where username = @username", new {username}).FirstOrDefault();
                //return result;
                var result = conn.Table<User>().Where(t => t.Username == username).FirstOrDefault();
                return result;
            });
        }


    }
}
