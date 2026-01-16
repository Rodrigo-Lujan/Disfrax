using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.DataBase
{
    //Obtener como generico al hacer consultas
    public static class DataBase
    {
        //Hacer una consulta SELECT
        //params tiene una longitud no definida --> en Java es como ...args
        public static List<T> DoQuery<T>(string sqlQuery, 
            Func<MySqlDataReader, T> map, 
            params MySqlParameter[] parametros)
        {
            var resultado = new List<T>();
            //Crear la conexion
            using var conexion = DBConnectionFactory.obtenerConexionBd();
            using var comand = new MySqlCommand(sqlQuery, conexion);

            if (parametros != null)
            {
                //Tiene parametros
                comand.Parameters.AddRange(parametros);
            }
            conexion.Open();

            //Tengo la data
            using var reader = comand.ExecuteReader();

            while (reader.Read())
            {
                //Paso una funcion que mapea ese objeto
                resultado.Add(map(reader));
            }
            return resultado;
        }

        //Ejecutar y obtener el Id
        public static int Execute(string sql,params MySqlParameter[] parametros)
        {
            using var conn = DBConnectionFactory.obtenerConexionBd();
            using var comand = new MySqlCommand(sql, conn);

            if(parametros != null)
            {
                comand.Parameters.AddRange(parametros);
            }

            conn.Open();
            return comand.ExecuteNonQuery(); //Es un insert, update 
        }

        public static void ExecuteProcedure(
            string procedureName,
            params MySqlParameter[] parametros)
        {
            try
            {
                using var conn = DBConnectionFactory.obtenerConexionBd();
                using var command = new MySqlCommand(procedureName, conn);

                command.CommandType = CommandType.StoredProcedure;

                if (parametros != null && parametros.Length > 0)
                {
                    command.Parameters.AddRange(parametros);
                }

                conn.Open();
                command.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                // Captura el SIGNAL SQLSTATE '45000'
                throw new Exception($"Error en el procedimiento almacenado: {ex.Message}");
            }
        }

    }
}
