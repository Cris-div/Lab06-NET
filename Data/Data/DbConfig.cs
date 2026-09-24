using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace WPF_SP.Data;

/// <summary>Builds the Neptuno connection string from local settings or environment variables.</summary>
public static class DbConfig
{
    private const string SettingsFileName = "dbsettings.local.json";

    public static string ConnectionString => BuildConnectionString();

    private static string BuildConnectionString()
    {
        var settings = LoadSettings();
        var server = Environment.GetEnvironmentVariable("NEPTUNO_DB_SERVER") ?? settings.Server;
        var database = Environment.GetEnvironmentVariable("NEPTUNO_DB_DATABASE") ?? settings.Database;
        var integratedText = Environment.GetEnvironmentVariable("NEPTUNO_DB_INTEGRATED_SECURITY");
        var integratedSecurity = integratedText is not null
            ? bool.Parse(integratedText)
            : settings.IntegratedSecurity;
        var user = Environment.GetEnvironmentVariable("NEPTUNO_DB_USER") ?? settings.User;
        var password = Environment.GetEnvironmentVariable("NEPTUNO_DB_PASSWORD") ?? settings.Password;

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                $"Faltan Server o Database en {SettingsFileName}. Copia dbsettings.example.json " +
                $"como {SettingsFileName} y completa la instancia SQL y la base de datos.");
        }

        if (!integratedSecurity && (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password)))
        {
            throw new InvalidOperationException(
                $"Faltan User o Password en {SettingsFileName}. Completa esos valores o activa " +
                "IntegratedSecurity si vas a iniciar sesión con Windows.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = integratedSecurity,
            TrustServerCertificate = settings.TrustServerCertificate,
            Encrypt = false,
            ConnectTimeout = 5
        };

        if (!integratedSecurity)
        {
            builder.UserID = user;
            builder.Password = password;
        }

        return builder.ConnectionString;
    }

    private static DbSettings LoadSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException(
                $"No se encontró {SettingsFileName} junto a la aplicación. " +
                "Copia dbsettings.example.json como dbsettings.local.json y agrega tu configuración local.",
                settingsPath);
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<DbSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("El archivo de configuración está vacío.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"El formato de {SettingsFileName} no es JSON válido.", ex);
        }
    }

    private sealed class DbSettings
    {
        public string? Server { get; init; }
        public string? Database { get; init; }
        public bool IntegratedSecurity { get; init; }
        public string? User { get; init; }
        public string? Password { get; init; }
        public bool TrustServerCertificate { get; init; } = true;
    }
}
