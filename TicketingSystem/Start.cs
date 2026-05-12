using BCrypt.Net;
using MySqlConnector;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TicketingSystem;

namespace Start
{
    public class Start
    {
        public string GetConnectionString()
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

            return csb.ConnectionString;
        }
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
                    await ExecuteProcedure(conn, "create_tb_tickets");

                    // 8) Crear admin si no existe
                    await ExecuteProcedure(conn, "create_admin");
                }

                //MessageBox.Show("Base de datos lista y conexión exitosa.", "Conexión", MessageBoxButton.OK, MessageBoxImage.Information);
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

        public async Task UpdateTicketInfo(int ticketId, string title, string description)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL update_ticket_info(@id, @title, @desc)";
            cmd.Parameters.AddWithValue("@id", ticketId);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@desc", description);

            await cmd.ExecuteNonQueryAsync();
        }


        public async Task<bool> CreateTicket(string titulo, string descripcion, int openedBy)
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
                cmd.CommandText = "create_tickets";

                cmd.Parameters.AddWithValue("@p_title", titulo);
                cmd.Parameters.AddWithValue("@p_description", descripcion);
                cmd.Parameters.AddWithValue("@p_opened_by", openedBy);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    int ticketId = reader.GetInt32("ticket_id");
                    Console.WriteLine($"Ticket creado con ID: {ticketId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al crear ticket: " + ex.Message);
                return false;
            }
        }



        public async Task<int?> GetUserIdByDisplayName(string displayName)
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
                cmd.CommandText = "get_user_id_by_displayname";
                cmd.Parameters.AddWithValue("@p_display_name", displayName);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    return reader.GetInt32(0);

                return null;
            }
            catch
            {
                return null;
            }
        }



        public async Task<(bool ok, int userId, string displayName, int roleId)> ValidateLogin(string username, string password)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL validate_login(@username);";
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // ❌ Usuario no existe o está inactivo
                return (false, 0, null, 0);
            }

            string storedHash = reader.GetString("password_hash");

            // ✔ Validar contraseña con BCrypt
            bool passwordOk = BCrypt.Net.BCrypt.Verify(password, storedHash);

            if (!passwordOk)
                return (false, 0, null, 0);

            int userId = reader.GetInt32("id");
            string displayName = reader.GetString("display_name");
            int roleId = reader.GetInt32("role_id");

            return (true, userId, displayName, roleId);
        }


        public async Task<int?> GetUserRole(string username)
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
                cmd.CommandText = "get_user_role";
                cmd.Parameters.AddWithValue("@p_username", username);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return reader.GetInt32(0);
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error obteniendo rol: " + ex.Message);
                return null;
            }
        }

        public async Task<string> GetDisplayName(string username)
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
                cmd.CommandText = "get_display_name";
                cmd.Parameters.AddWithValue("@p_username", username);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    return reader.GetString(0);

                return "Usuario";
            }
            catch
            {
                return "Usuario";
            }
        }

        public async Task<List<Ticket>> GetAllTickets()
        {
            var list = new List<Ticket>();

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("CALL get_all_tickets()", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Ticket
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader.GetString("description"),
                    Status = reader.GetString("status"),
                    Priority = reader.GetInt32("priority"),
                    CreatedAt = reader.GetDateTime("created_at").ToString("yyyy-MM-dd HH:mm"),
                    OpenedBy = reader.GetString("opened_by")
                });
            }

            return list;
        }

        public async Task UpdateTicketStatus(int ticketId, string newStatusName)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("CALL update_ticket_status_by_name(@p_ticket_id, @p_status_name)", conn);
            cmd.Parameters.AddWithValue("@p_ticket_id", ticketId);
            cmd.Parameters.AddWithValue("@p_status_name", newStatusName);

            await cmd.ExecuteNonQueryAsync();
        }


        private async Task ExecuteProcedure(MySqlConnection conn, string procedureName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CALL {procedureName}();";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateLastLogin(string username)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL update_last_login(@user);";
                cmd.Parameters.AddWithValue("@user", username);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error actualizando último login: " + ex.Message);
            }
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

        public async Task<List<dynamic>> GetAllUsers()
        {
            var lista = new List<dynamic>();

            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL get_all_users();";

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    lista.Add(new
                    {
                        username = reader["username"].ToString(),
                        display_name = reader["display_name"].ToString(),
                        role_name = reader["role_name"].ToString(),
                        last_login_at = reader["last_login_at"]?.ToString(),
                        created_at = reader["created_at"]?.ToString(),
                        updated_at = reader["updated_at"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando usuarios: " + ex.Message);
            }

            return lista;
        }
        public async Task CreateUser(string username, string displayName, string roleName)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL create_user(@username, @display_name, @role_name);";
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@display_name", displayName);
                cmd.Parameters.AddWithValue("@role_name", roleName);

                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show(
                    "La contrasenya por defecto sera: hola123",
                    "Usuario creado satisfactoriamente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creando usuario: " + ex.Message);
            }
        }


        public async Task UpdateUser(string originalUsername, string newUsername, string displayName, string roleName)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL update_user(@original_username, @new_username, @display_name, @role_name);";

                cmd.Parameters.AddWithValue("@original_username", originalUsername);
                cmd.Parameters.AddWithValue("@new_username", newUsername);
                cmd.Parameters.AddWithValue("@display_name", displayName);
                cmd.Parameters.AddWithValue("@role_name", roleName);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error actualizando usuario: " + ex.Message);
            }
        }



        public async Task DeleteUser(string username)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CALL delete_user(@username);";
                cmd.Parameters.AddWithValue("@username", username);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error eliminando usuario: " + ex.Message);
            }
        }

        public async Task UpdatePassword(string username, string newHash)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL update_password(@username, @new_hash);";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@new_hash", newHash);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DateTime?> GetLastLogin(string username)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT last_login_at FROM users WHERE username = @username;";
            cmd.Parameters.AddWithValue("@username", username);

            var result = await cmd.ExecuteScalarAsync();

            if (result == DBNull.Value || result == null)
                return null;

            return Convert.ToDateTime(result);
        }

        public async Task UpdateTicketPriority(int ticketId, int priority)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL update_ticket_priority(@id, @priority)";
            cmd.Parameters.AddWithValue("@id", ticketId);
            cmd.Parameters.AddWithValue("@priority", priority);

            await cmd.ExecuteNonQueryAsync();
        }



    }


}

