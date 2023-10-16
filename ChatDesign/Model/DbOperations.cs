using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ChatDesign.Model
{
    internal class DbOperations
    {
        internal enum TableType
        {
            Traditional,
            FileStream
        }

        public static string GetConnectionString()
        {
            System.Data.SqlClient.SqlConnectionStringBuilder connectionStringBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder();

            connectionStringBuilder.DataSource = "DESKTOP-IFNI7QS";
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.InitialCatalog = "ChatAppDataBase4";

            return connectionStringBuilder.ToString();
        }

        // DataBase Operations That Used In Login Window
        #region Login
        // User Authentication at Login Button Click in Login Window  
        internal static bool AuthenticateUser(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM [User] WHERE [Name] = @Username AND [Password] = @Password";
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int matchingUserCount = (int)command.ExecuteScalar();

                    return matchingUserCount > 0; // If count is greater than 0, user is authenticated
                }
            }
        }

        // Fetch User Image 
        internal static byte[] GetUserImage(string username)
        {
            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ImageInformation].Data FROM [User] INNER JOIN [ImageInformation] ON [User].ImageId = [ImageInformation].Id WHERE [User].[Name] = @Username";
                    command.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (byte[])reader["Data"];
                        }
                    }
                }
            }

            return null; // Return null if no matching record is found
        }
        #endregion

        // DataBase Operations That Used In Registration Window
        #region Registration
        // Store Image
        internal static System.TimeSpan StoreFileUsingSqlParameter(string file, TableType tableType)
        {
            System.Data.SqlClient.SqlParameter parameter;
            int rowsInserted;
            System.DateTime startTime;
            System.DateTime endTime;

            using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (System.Data.SqlClient.SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO " + (tableType == TableType.Traditional ? "ImageInformation" : "FileStreamTest") + " ([Name], [Data]) VALUES (@Name, @Data)";
                    command.CommandType = System.Data.CommandType.Text;

                    parameter = new System.Data.SqlClient.SqlParameter("@Name", System.Data.SqlDbType.NVarChar, 100);
                    parameter.Value = file.Substring(file.LastIndexOf('\\') + 1);
                    command.Parameters.Add(parameter);

                    parameter = new System.Data.SqlClient.SqlParameter("@Data", System.Data.SqlDbType.VarBinary);
                    parameter.Value = System.IO.File.ReadAllBytes(file);
                    command.Parameters.Add(parameter);

                    command.Transaction = connection.BeginTransaction();
                    startTime = System.DateTime.Now;

                    rowsInserted = command.ExecuteNonQuery();

                    endTime = System.DateTime.Now;
                    command.Transaction.Commit();
                }

                connection.Close();
            }

            return endTime.Subtract(startTime);
        }

        // Store User
        internal static TimeSpan StoreUserUsingSqlParameter(string name, string password, string imageId, bool isAdmin, int likesId, int listeningHistoryId)
        {
            SqlParameter parameter;
            int rowsInserted;
            DateTime startTime;
            DateTime endTime;

            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [User] ([Name], [Password], [ImageId]) VALUES (@Name, @Password, @ImageId)";
                    command.CommandType = CommandType.Text;

                    parameter = new SqlParameter("@Name", SqlDbType.NVarChar, 100);
                    parameter.Value = name;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("@Password", SqlDbType.NVarChar, 20);
                    parameter.Value = password;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("@ImageId", SqlDbType.UniqueIdentifier);
                    parameter.Value = Guid.Parse(imageId); // Convert string to Guid
                    command.Parameters.Add(parameter);

                    command.Transaction = connection.BeginTransaction();
                    startTime = DateTime.Now;

                    rowsInserted = command.ExecuteNonQuery();

                    endTime = DateTime.Now;
                    command.Transaction.Commit();
                }

                connection.Close();
            }

            return endTime.Subtract(startTime);
        }

        // Select User Image Id by Name
        internal static string GetImageId(string imagename)
        {
            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ImageInformation].Id FROM [ImageInformation] WHERE [ImageInformation].Name = @Imagename";
                    command.CommandType = CommandType.Text;

                    command.Parameters.Add(new SqlParameter("@Imagename", SqlDbType.NVarChar, 100));
                    command.Parameters["@Imagename"].Value = imagename;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["Id"].ToString();
                        }
                    }
                }
            }

            return null; // Return null if no matching record is found
        }

        #endregion

        // // DataBase Operations That Used In Main Window
        #region MainWindow
        internal static byte[] GetImages()
        {
            System.Data.SqlClient.SqlDataReader reader;

            using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (System.Data.SqlClient.SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Data FROM ImageInformation";
                    command.CommandType = System.Data.CommandType.Text;



                    reader = command.ExecuteReader();
                    reader.Read();

                    return (byte[])reader["Data"];
                }
            }
        }

        internal static int GetUserId(string username)
        {
            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT [User].ID FROM [User] WHERE [User].Name = @username";
                    command.CommandType = CommandType.Text;

                    command.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar, 100));
                    command.Parameters["@username"].Value = username;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse(reader["ID"].ToString());
                        }
                    }
                }
            }
            return -1; // You can use a different sentinel value if -1 doesn't fit your needs
        }
        public static List<Chat> GetUserChats(int userId)
        {
            List<Chat> chats = new List<Chat>();

            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT c.* FROM [Chat] c " +
                                          "INNER JOIN [UserChat] uc ON c.ID = uc.ChatID " +
                                          "WHERE uc.UserID = @UserId";
                    command.CommandType = CommandType.Text;

                    SqlParameter parameter = new SqlParameter("@UserId", SqlDbType.Int);
                    parameter.Value = userId;
                    command.Parameters.Add(parameter);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Chat chat = new Chat
                            {
                                ID = (int)reader["ID"],
                                ChatName = reader["ChatName"].ToString(),
                                ChatType = (bool)reader["ChatType"],
                                Avatar = (byte[])reader["Avatar"]
                            };
                            chats.Add(chat);
                        }
                    }
                }

                connection.Close();
            }

            return chats;
        }

        public static ObservableCollection<ChatItem> LoadChatMessagesFromDatabase(int chatID)
        {
            ObservableCollection<ChatItem> chatMessages = new ObservableCollection<ChatItem>();

            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    // Update the SQL query to select the required columns and aliases, and filter by ChatID
                    command.CommandText = "SELECT MessageText AS Content, SenderID AS Sender, Timestamp AS Date, 0 AS IsSender " +
                                          "FROM [Message] WHERE ChatID = @chatID";
                    command.CommandType = CommandType.Text;

                    command.Parameters.AddWithValue("@chatID", chatID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ChatItem chatMessage = new ChatItem
                            {
                                Content = reader["Content"].ToString(),
                                Sender = reader["Sender"].ToString(),
                                Date = reader["Date"].ToString(),
                                IsSender = (int)reader["IsSender"] == 1 // Convert 1 to true, 0 to false
                            };

                            chatMessages.Add(chatMessage);
                        }
                    }
                }
            }

            return chatMessages;
        }


        public static List<User> SearchUsers(string searchText)
        {
            List<User> searchResults = new List<User>();

            using (SqlConnection connection = new SqlConnection(DbOperations.GetConnectionString()))
            {
                connection.Open();

                // Replace [YourQuery] with a SQL query that searches for users based on the search text.
                string query = "SELECT u.[ID], u.[Name], i.[Data] AS [ImageData] " +
                               "FROM [User] u " +
                               "LEFT JOIN [ImageInformation] i ON u.[ImageId] = i.[ID] " +
                               "WHERE u.[Name] LIKE @searchText";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@searchText", "%" + searchText + "%"); // You can use the LIKE operator for partial matches

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            User user = new User
                            {
                                ID = (int)reader["ID"],
                                Name = reader["Name"].ToString(),
                                ImagePath = (byte[])reader["ImageData"]
                            };

                            searchResults.Add(user);
                        }
                    }
                }
            }

            return searchResults;
        }

        #endregion
    }
}
