using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MySqlConnector;
using BCrypt.Net;

namespace Start
{
    public class Start
    {
        public async Task ConnectionAsync()
        {
            // 1) Intentar cargar .env desde el directorio de ejecución
            TryLoadDotEnv();

            // 2) Leer variables de entorno
            string host = Environment.GetEnvironmentVariable("DB_HOST");
            string database = Environment.GetEnvironmentVariable("DB_NAME");
            string user = Environment.GetEnvironmentVariable("DB_USER");
            string password = Environment.GetEnvironmentVariable("DB_PASSWORD");
            string sslModeEnv = Environment.GetEnvironmentVariable("DB_SSLMODE");

            // 3) Validar credenciales obligatorias
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Faltan variables de entorno necesarias: DB_USER y/o DB_PASSWORD.\n" +
                    "Asegúrate de tener un archivo .env en la carpeta de ejecución o de definir las variables en el entorno.",
                    "Variables de entorno faltantes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                Application.Current.Shutdown();
                return;
            }

            // 4) Construir connection string base (sin DB)
            var csb = new MySqlConnectionStringBuilder
            {
                Server = host,
                UserID = user,
                Password = password,
                Pooling = true,
                ConnectionTimeout = 15,
                SslMode = sslModeEnv?.Equals("Required", StringComparison.OrdinalIgnoreCase) == true
                    ? MySqlSslMode.Required
                    : MySqlSslMode.None
            };

            try
            {
                // 5) Conexión sin base de datos (para crearla si no existe)
                using (var conn = new MySqlConnection(csb.ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{database}`;";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // 6) Conexión ya con la base de datos seleccionada
                csb.Database = database;

                using (var conn = new MySqlConnection(csb.ConnectionString))
                {
                    await conn.OpenAsync();

                    // 7) Ejecutar procedimientos de creación de tablas
                    await ExecuteProcedure(conn, "create_tb_roles");
                    await ExecuteProcedure(conn, "create_tb_departments");
                    await ExecuteProcedure(conn, "create_tb_users");

                    // 8) Crear admin si no existe
                    await ExecuteProcedure(conn, "create_admin");
                }

                MessageBox.Show("Base de datos lista y conexión exitosa.", "Conexión", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al inicializar la base de datos:\n" + ex.Message,
                    "Error de conexión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }
        }

        public async Task<bool> ValidateLogin(string username, string password)
        {
            try
            {
                string host = Environment.GetEnvironmentVariable("DB_HOST");
                string database = Environment.GetEnvironmentVariable("DB_NAME");
                string user = Environment.GetEnvironmentVariable("DB_USER");
                string pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

                var csb = new MySqlConnectionStringBuilder
                {
                    Server = host,
                    Database = database,
                    UserID = user,
                    Password = pass,
                    SslMode = MySqlSslMode.None
                };

                using var conn = new MySqlConnection(csb.ConnectionString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "validate_admin_login";
                cmd.Parameters.AddWithValue("@p_username", username);

                using var reader = await cmd.ExecuteReaderAsync();

                string storedHash = null;
                if (await reader.ReadAsync())
                {
                    int ordinal = reader.GetOrdinal("password_hash");
                    storedHash = reader.GetString(ordinal);
                    
                }

                if (string.IsNullOrEmpty(storedHash))
                    return false; // usuario no existe

                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en login: " + ex.Message);
                return false;
            }
        }



        private async Task ExecuteProcedure(MySqlConnection conn, string procedureName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CALL {procedureName}();";
            await cmd.ExecuteNonQueryAsync();
        }

        private void TryLoadDotEnv()
{
    try
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string envPath = Path.Combine(exeDir, ".env");

        if (!File.Exists(envPath))
        {
            string projectEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(projectEnv)) envPath = projectEnv;
            else
            {
                MessageBox.Show("No se encontró el archivo .env en el directorio de ejecución ni en el proyecto.");
                return;
            }
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("#")) continue;

            int idx = line.IndexOf('=');
            if (idx <= 0) continue;

            string key = line.Substring(0, idx).Trim();
            string val = line.Substring(idx + 1).Trim();

            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
            {
                val = val.Substring(1, val.Length - 2);
            }

            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, val);
            }
        }

        // 🔍 Mostrar las variables cargadas para verificar
        string dbUser = Environment.GetEnvironmentVariable("DB_USER");
        string dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD");
        string dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        string dbName = Environment.GetEnvironmentVariable("DB_NAME");

    }
    catch (Exception ex)
    {
        MessageBox.Show(
            "Error al cargar .env:\n" + ex.Message,
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        Application.Current.Shutdown();
    }
}

    }
}

