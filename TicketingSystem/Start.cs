using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MySqlConnector;

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
                return;
            }

            // 4) Construir connection string de forma segura
            var csb = new MySqlConnectionStringBuilder
            {
                Server = host,
                Database = database,
                UserID = user,
                Password = password,
                Pooling = true,
                ConnectionTimeout = 15
            };

            csb.SslMode = sslModeEnv.Equals("Required", StringComparison.OrdinalIgnoreCase)
                ? MySqlSslMode.Required
                : MySqlSslMode.None;

            string connectionString = csb.ConnectionString;

            // 5) Intentar conectar
            using var connection = new MySqlConnection(connectionString);

            try
            {
                await connection.OpenAsync();
                MessageBox.Show("Conexión exitosa a MySQL.", "Conexión", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper: carga un .env simple (KEY=VALUE) en Environment si existe
        private void TryLoadDotEnv()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string envPath = Path.Combine(exeDir, ".env");

                if (!File.Exists(envPath))
                {
                    // También buscar en la carpeta del proyecto (útil en desarrollo)
                    string projectEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                    if (File.Exists(projectEnv)) envPath = projectEnv;
                    else return;
                }

                foreach (var rawLine in File.ReadAllLines(envPath))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("#")) continue; // comentario

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    // Quitar comillas si las hay
                    if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                    {
                        val = val.Substring(1, val.Length - 2);
                    }

                    // Solo establecer si no existe ya en el entorno (evita sobrescribir variables del sistema)
                    if (Environment.GetEnvironmentVariable(key) == null)
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
            }
            catch
            {
                // No hacemos nada si falla la carga; la app seguirá y mostrará mensaje si faltan variables.
            }
        }
    }
}

