using System;
using System.Data.Common;
using System.Data.SQLite;

namespace Rochas.DataClassifier.DBContext
{
    public static class KnowledgeDB
    {
        static DbConnection internalConnection = null;

        public static DbConnection GetConnection(string connectionString)
        {
            if (internalConnection == null)
            {
                var newConnection = new SQLiteConnection(connectionString);

                if (newConnection != null)
                {
                    newConnection.Open();

                    // To use without the SQLite.CodeFirst library

                    //if (newConnection.State == System.Data.ConnectionState.Open)
                    //{
                    //    using (var groupTableCmd = newConnection.CreateCommand())
                    //    {
                    //        groupTableCmd.CommandText = @"CREATE TABLE KnowledgeGroups
                    //                                    (
                    //                                        Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
                    //                                        Name VARCHAR(2000) NOT NULL
                    //                                    );";

                    //        groupTableCmd.ExecuteNonQuery();
                    //    }

                    //    using (var hashTableCmd = newConnection.CreateCommand())
                    //    {
                    //        hashTableCmd.CommandText = @"CREATE TABLE KnowledgeHashs 
                    //                                   (GroupId INTEGER NOT NULL PRIMARY KEY, 
                    //                                    Value INTEGER NOT NULL,
                    //                                    FOREIGN KEY (GroupId) 
                    //                                    REFERENCES KnowledgeGroup (Id)
                    //                                   );";

                    //        hashTableCmd.ExecuteNonQuery();
                    //    }
                    //}
                }

                internalConnection = newConnection;

                return newConnection;
            }
            else
            {
                return internalConnection;
            }
        }
    }
}