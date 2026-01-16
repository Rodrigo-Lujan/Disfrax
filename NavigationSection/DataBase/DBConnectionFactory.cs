using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.DataBase
{
    public static class DBConnectionFactory
    {
        private static readonly string _connection = "....";

        public static MySqlConnection obtenerConexionBd()
        {
            return new MySqlConnection(_connection);
        }
    }
}

