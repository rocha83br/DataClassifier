using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Rochas.DataClassifier.Models;
using Rochas.DataClassifier.DBContext;

namespace Rochas.DataClassifier.Repositories
{
    public static class KnowledgeRepository
    {
        private static string connStr = string.Empty;

        public static void Init(string connectionString)
        {
            connStr = connectionString;

            KnowledgeDB.GetConnection(connectionString);
        }

        public static ICollection<KnowledgeGroup> List()
        {
            ICollection<KnowledgeGroup> result = null;

            try
            {
                var connection = KnowledgeDB.GetConnection(connStr);
                using (var context = new KnowledgeContext(connection))
                {
                    result = context.Knowledgement.AsParallel().AsOrdered().ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KnowledgeGroup Get(int id)
        {
            KnowledgeGroup result = null;

            try
            {
                var connection = KnowledgeDB.GetConnection(connStr);
                using (var context = new KnowledgeContext(connection, true))
                {
                    result = context.Knowledgement.Find(id);

                    if (result != null)
                    {
                        var readFake = result.Hashes;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static bool Save(KnowledgeGroup knowledgeGroup)
        {
            var preResult = 0;

            try
            {
                var connection = KnowledgeDB.GetConnection(connStr);
                using (var context = new KnowledgeContext(connection))
                {
                    context.Knowledgement.Add(knowledgeGroup);

                    preResult = context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("UNIQUE constraint failed"))
                    {
                        preResult = updateGroup(knowledgeGroup);
                    }
                    else
                        throw ex.InnerException ?? ex;
                }
                else
                    throw ex;
            }

            return (preResult > 0);
        }

        private static int updateGroup(KnowledgeGroup knowledgeGroup)
        {
            var result = 0;

            try
            {
                var connection = KnowledgeDB.GetConnection(connStr);
                using (var context = new KnowledgeContext(connection))
                {
                    var existGroup = context.Knowledgement.Find(knowledgeGroup.Name);

                    if (existGroup != null)
                    {
                        existGroup.Hashes = knowledgeGroup.Hashes;
                    }

                    result = context.SaveChanges();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }

            return result;
        }
    }
}
