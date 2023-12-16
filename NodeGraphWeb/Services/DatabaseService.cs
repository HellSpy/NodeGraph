using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace NodeGraphWeb.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "server=193.203.166.22;user=u278723081_Avilin;database=u278723081_NodeGraph;port=3306;password=Csvma!l122mA";
        }

        public IEnumerable<object> FetchNodes()
        {
            var nodes = new List<object>();
            using (var connection = new MySqlConnection(_connectionString))
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
            using (var connection = new MySqlConnection(_connectionString))
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
