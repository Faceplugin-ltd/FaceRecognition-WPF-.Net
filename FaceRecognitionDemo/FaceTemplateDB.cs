using System;
using System.Data.SQLite;
using System.IO;

class FaceTemplateDB
{
    private static string connectionString = "Data Source=FaceTemplates.db;Version=3;";

    // Create table if not exists
    public static void CreateTable()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS FaceTemplates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    Template BLOB NOT NULL
                )";

            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    // Convert float[] to byte[] using BitConverter
    private static byte[] FloatArrayToByteArray(float[] array)
    {
        return array.SelectMany(BitConverter.GetBytes).ToArray();
    }

    // Convert byte[] back to float[]
    private static float[] ByteArrayToFloatArray(byte[] byteArray)
    {
        return Enumerable.Range(0, byteArray.Length / sizeof(float))
                         .Select(i => BitConverter.ToSingle(byteArray, i * sizeof(float)))
                         .ToArray();
    }

    public static int GetHighestId()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT MAX(Id) FROM FaceTemplates";

            using (var command = new SQLiteCommand(query, connection))
            {
                object result = command.ExecuteScalar(); // Executes the query and gets the result

                // If no rows, return 0 (or handle as appropriate)
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
    }

    // Store face template in database
    public static void StoreFaceTemplate(string userName, float[] faceTemplate)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string insertQuery = "INSERT INTO FaceTemplates (UserName, Template) VALUES (@UserName, @Template)";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@UserName", userName);
                command.Parameters.AddWithValue("@Template", FloatArrayToByteArray(faceTemplate)); // Convert float[] to byte[]
                command.ExecuteNonQuery();
            }
        }
    }

    public static float[] GetFaceTemplateById(int id)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Modified query to retrieve template by ID (No)
            string selectQuery = "SELECT Template FROM FaceTemplates WHERE Id = @Id";

            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", id);  // Use the ID (or No)

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())  // Move to the first row
                    {
                        byte[] byteArray = reader["Template"] as byte[]; // Ensure it's a byte array

                        if (byteArray != null && byteArray.Length > 0)
                        {
                            return ByteArrayToFloatArray(byteArray);
                        }
                    }
                }
            }
        }

        return null;  // Return null if not found
    }

    public static string GetUsernameById(int id)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Modified query to retrieve template by ID (No)
            string selectQuery = "SELECT UserName FROM FaceTemplates WHERE Id = @Id";

            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                command.Parameters.AddWithValue("@Id", id);  // Use the ID (or No)

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())  // Move to the first row
                    {
                        return reader["UserName"].ToString(); // Cast it to string
                    }
                }
            }
        }

        return null;  // Return null if not found
    }

    // Method to delete all data in the database
    public static void DeleteAllData()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Get the list of tables in the database
            string getTableNamesQuery = "SELECT name FROM sqlite_master WHERE type='table';";
            using (var command = new SQLiteCommand(getTableNamesQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tableName = reader["name"].ToString();
                        // Skip system tables (sqlite_sequence is used for auto-increment management)
                        if (tableName != "sqlite_sequence")
                        {
                            // Delete all rows from the table
                            string deleteQuery = $"DELETE FROM {tableName};";
                            using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                            {
                                deleteCommand.ExecuteNonQuery();
                            }

                            // Reset the auto-increment counter for the table
                            string resetAutoIncrementQuery = $"DELETE FROM sqlite_sequence WHERE name='{tableName}';";
                            using (var resetCommand = new SQLiteCommand(resetAutoIncrementQuery, connection))
                            {
                                resetCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
    }
}