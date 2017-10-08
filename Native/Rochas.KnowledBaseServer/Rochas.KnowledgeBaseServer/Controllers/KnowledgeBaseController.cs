using System;
using System.Linq;
using System.Web.Http;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Rochas.KnowledgeBaseServer.Models;
using Rochas.KnowledgeBaseServer.Repositories;

namespace Rochas.KnowledgeBaseServer.Controllers
{
    public class KnowledgeBaseController : ApiController
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["InMemorySQLite"].ConnectionString;

        [HttpGet]
        public IHttpActionResult Get()
        {
            ICollection<KnowledgeGroup> result = null;

            try
            {
                var connection = KnowledgeDB.GetConnection(connectionString);
                using (var context = new KnowledgeContext(connection))
                {
                    result = context.Knowledgement.AsParallel().AsOrdered().ToList();
                }

                if (result != null)
                    return Ok(result);
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex.InnerException ?? ex);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> Get([FromUri]string groupName)
        {
            KnowledgeGroup result = null;

            try
            {
                var connection = KnowledgeDB.GetConnection(connectionString);
                using (var context = new KnowledgeContext(connection, true))
                {
                    result = await context.Knowledgement.FindAsync(groupName);

                    if (result != null)
                    {
                        var readFake = result.Hashes;
                    }
                }

                if (result != null)
                    return Ok(result);
                else
                    return NotFound();
            }
            catch(Exception ex)
            {
                return InternalServerError(ex.InnerException ?? ex);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Post([FromBody]KnowledgeGroup knowledgeGroup)
        {
            try
            {
                var connection = KnowledgeDB.GetConnection(connectionString);
                using (var context = new KnowledgeContext(connection))
                {
                    context.Knowledgement.Add(knowledgeGroup);

                    await context.SaveChangesAsync();
                }

                return Ok();
            }
            catch(Exception ex)
            {
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains("UNIQUE constraint failed"))
                    {
                        return await updateGroup(knowledgeGroup);
                    }
                    else
                        return InternalServerError(ex.InnerException ?? ex);
                }
                else
                    return InternalServerError(ex);
            }
        }

        private async Task<IHttpActionResult> updateGroup(KnowledgeGroup knowledgeGroup)
        {
            var connection = KnowledgeDB.GetConnection(connectionString);
            using (var context = new KnowledgeContext(connection))
            {
                var existGroup = await context.Knowledgement.FindAsync(knowledgeGroup.Name);

                if (existGroup != null)
                {
                    existGroup.Hashes = knowledgeGroup.Hashes;
                }

                await context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
