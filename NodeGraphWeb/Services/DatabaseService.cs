using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace NodeGraphWeb.Services
{
    public class DatabaseService
    {
        private readonly string? connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("Database");
        }

        public IEnumerable<object> FetchNodes()
        {
            var nodes = new List<object>();
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT Id, LabelText, Color, Size FROM Nodes", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        nodes.Add(new
                        {
                            Id = reader["Id"],
                            Label = reader["LabelText"],
                            Color = reader["Color"],
                            Size = reader["Size"]
                        });
                    }
                }
            }
            return nodes;
        }

        public IEnumerable<object> FetchEdges()
        {
            var edges = new List<object>();
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT SourceId, TargetId FROM Edges", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        edges.Add(new
                        {
                            source = reader["SourceId"],
                            target = reader["TargetId"]
                        });
                    }
                }
            }
            return edges;
        }
    }
}
