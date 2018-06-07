using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Reflection;

namespace ADM.ECO.API.Models
{
    public class db
    {
        private String server;
        private String database;
        private Int32 commandTimeout;

        public db(String server, String database, Int32? commandTimeout)
        {
            this.server = server;
            this.database = database;
            this.commandTimeout = commandTimeout == null ? 0 : (Int32)commandTimeout;
        }

        private SqlConnection SetConnectionString()
        {
            return new SqlConnection(
                //"Server = tcp:installe.database.windows.net, 1433; Initial Catalog = Installe; Persist Security Info = False; User ID = { your_username }; Password ={ your_password}; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30;""
            "Data Source=installe.database.windows.net; Initial Catalog=installe; Persist Security Info=True; User ID=inst;  Password=Painel)2015; Pooling=False;Connection Timeout=" + this.commandTimeout + ";");
        }

        private static List<T> BindList<T>(DataTable dt)
        {
            var fields = typeof(T).GetProperties();

            List<T> lst = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                // Create the object of T
                var ob = Activator.CreateInstance<T>();

                foreach (var fieldInfo in fields)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        // Matching the columns with fields
                        if (fieldInfo.Name == dc.ColumnName)
                        {
                            // Get the value from the datatable cell
                            object value = dr[dc.ColumnName];

                            if (value.ToString().Equals(String.Empty))
                            {
                                value = null;
                            }

                            // Set the value into the object
                            fieldInfo.SetValue(ob, value);
                            break;
                        }
                    }
                }
                lst.Add(ob);
            }
            return lst;
        }

        private static String ProcBuilder(Procedure procedure)
        {
            String param = String.Empty;

            if (procedure.PARAMETERS != null)
            {
                foreach (Parameter p in procedure.PARAMETERS)
                {
                    if (p.VALUE.Equals(String.Empty))
                    {
                        param += (procedure.PARAMETERS.IndexOf(p) != 0 ? "," : "") + "@" + p.NAME + " = NULL";
                    }
                    else if (p.TYPE == typeof(String) || p.TYPE == typeof(DateTime))
                    {
                        param += (procedure.PARAMETERS.IndexOf(p) != 0 ? "," : "") + "@" + p.NAME + " = " + "'" + p.VALUE + "'";
                    }
                    else
                    {
                        param += (procedure.PARAMETERS.IndexOf(p) != 0 ? "," : "") + "@" + p.NAME + " = " + p.VALUE;
                    }
                }
            }
            return "EXEC " + procedure.NAME + " " + param;
        }

        private static String InsertBuilder<T>(T obj)
        {
            try
            {
                PropertyInfo[] fields = typeof(T).GetProperties();

                String header = String.Format("INSERT INTO {0} (", typeof(T).Name.ToString().Replace("_Bean", ""));
                String values = "VALUES (";

                List<PropertyInfo> fieldsNoVirtual = new List<PropertyInfo>();
                foreach (PropertyInfo fieldInfo in fields)
                {
                    if (!fieldInfo.GetGetMethod().IsVirtual)
                    {
                        fieldsNoVirtual.Add(fieldInfo);
                    }
                }

                foreach (PropertyInfo fieldInfo in fieldsNoVirtual)
                {
                    if (!fieldInfo.GetGetMethod().IsVirtual)
                    {
                        header = header + (fieldsNoVirtual.ToList().IndexOf(fieldInfo) != fieldsNoVirtual.ToList().Count - 1 ? fieldInfo.Name + "," : fieldInfo.Name + ")");

                        String value = "NULL";

                        if (fieldInfo.GetValue(obj) != null)
                        {
                            value = fieldInfo.GetValue(obj).ToString();

                            if (fieldInfo.GetValue(obj).GetType() == typeof(String))
                            {
                                value = "'" + value.TrimEnd() + "'";
                            }
                            else if (fieldInfo.GetValue(obj).GetType() == typeof(DateTime))
                            {
                                value = "'" + ((DateTime)fieldInfo.GetValue(obj)).ToString("yyyyMMdd HH:mm:ss") + "'";
                            }
                            else if (fieldInfo.GetValue(obj).GetType() == typeof(Decimal))
                            {
                                value = value.Replace(',', '.');
                            }
                        }

                        values = values + (fieldsNoVirtual.ToList().IndexOf(fieldInfo) != fieldsNoVirtual.ToList().Count - 1 ? value + "," : value + ")");
                    }
                }

                return header + values;
            }
            catch (Exception)
            {
                throw;
            }
        }

        ///<summary>
        ///<para>Método que executa um script de banco e espera retorno.</para>
        ///<para>Deve ser passado o tipo de dado do resultado esperado.</para>
        ///</summary>
        public List<T> Select<T>(String query)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.CommandTimeout = this.commandTimeout;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            return BindList<T>(dt);
                        }
                        else
                        {
                            return new List<T>();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        ///<summary>
        ///<para>Método que executa um script de banco e espera retorno.</para>
        ///<para>Deve ser passado o tipo de dado do resultado esperado.</para>
        ///</summary>
        public DataTable Select(String query)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.CommandTimeout = this.commandTimeout;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            return dt;
                        }
                        else
                        {
                            return new DataTable();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        ///<summary>
        ///<para>Método que executa uma procedure de banco e espera retorno.</para>
        ///<para>Deve ser passado o tipo de dado do resultado esperado.</para>
        ///</summary>
        public List<T> Select<T>(Procedure procedure)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = ProcBuilder(procedure);
                    cmd.CommandTimeout = this.commandTimeout;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            return BindList<T>(dt);
                        }
                        else
                        {
                            return new List<T>();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        ///<summary>
        ///<para>Método que executa um script de banco e não espera retorno.</para>
        ///<para>Resultado Booleano que exibe se o processo foi executado com sucesso ou não.</para>
        ///</summary>
        public Boolean Exec(String query)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.CommandTimeout = this.commandTimeout;
                    Int32 rowsAffected = cmd.ExecuteNonQuery();

                    return rowsAffected >= 0 ? true : false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        ///<summary>
        ///<para>Método que executa uma Procedure de banco e não espera retorno.</para>
        ///<para>Resultado Booleano que exibe se o processo foi executado com sucesso ou não.</para>
        ///</summary>
        public Boolean Exec(Procedure procedure)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = ProcBuilder(procedure);
                    cmd.CommandTimeout = this.commandTimeout;
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        ///<summary>
        ///<para>Método que insere no banco utilizando o Bean que seja idêntico à tabela de banco.</para>
        ///<para>Resultado Booleano que exibe se o processo foi executado com sucesso ou não.</para>
        ///</summary>
        public String Insert<T>(T obj)
        {
            try
            {
                using (var conn = SetConnectionString())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = InsertBuilder(obj);
                    cmd.CommandTimeout = this.commandTimeout;
                    cmd.ExecuteNonQuery();

                    return "OK";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //public Boolean Insert<T>(T obj)
        //{
        //    try
        //    {
        //        using (var conn = SetConnectionString())
        //        using (var cmd = conn.CreateCommand())
        //        {
        //            conn.Open();
        //            cmd.CommandText = InsertBuilder(obj);
        //            cmd.ExecuteNonQuery();

        //            return true;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}
    }

    public class Procedure
    {
        public String NAME { get; set; }
        public List<Parameter> PARAMETERS { get; set; }
    }

    public class Parameter
    {
        public String NAME { get; set; }
        public String VALUE { get; set; }
        public Type TYPE { get; set; }
    }
}