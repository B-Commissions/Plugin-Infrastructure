using BlueBeard.Core.Configs;

namespace BlueBeard.Database;

public class DatabaseConfig : IConfig
{
    public string Host { get; set; }
    public ushort Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public void LoadDefaults()
    {
        Host = "localhost";
        Port = 3306;
        Database = "unturned";
        Username = "root";
        Password = "";
    }
}
