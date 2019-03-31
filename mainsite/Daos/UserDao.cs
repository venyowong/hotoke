using System;
using Dapper;
using Hotoke.MainSite;
using Hotoke.MainSite.Entities;
using MySql.Data.MySqlClient;

namespace Hotoke.MainSite.Daos
{
    public class UserDao : IDisposable
    {
        private MySqlConnection connection;
        
        public UserDao(string connectionString)
        {
            if(string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            this.connection = new MySqlConnection(connectionString);
        }

        public User GetUserByEMail(string email)
        {
            if(string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return connection.QueryFirstOrDefault<User>("select * from user where email=@EMail;", new{EMail = email});
        }

        public int CreateUser(string email, string password)
        {
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return 0;
            }

            var salt = Guid.NewGuid().ToString("N");
            return connection.Execute("insert into user(email, password, salt) values(@EMail, @Password, @Salt)", new
            {
                EMail = email,
                Salt = salt,
                Password = $"{password}{salt}".GetMd5Hash()
            });
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}