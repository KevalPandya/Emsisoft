using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Security.Cryptography;
using System.Text;

namespace APIHandler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HashesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HashesController(DbContextOptions options)
        {
            _context = new ApplicationDbContext(options);
        }

        [HttpGet]
        public IActionResult Get()
        {
            var hashSummary = _context.Hashes
                .GroupBy(h => h.Date.Date)
                .Select(h => new
                {
                    date = h.Key.ToString("yyyy-MM-dd"),
                    count = h.Count()
                }).ToList();

            return Ok(hashSummary);
        }

        [HttpPost]
        public IActionResult Post()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var config = builder.Build();

            var rabbitConfig = config.GetSection("RabbitMQ");
            var connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitConfig["HostName"],
                UserName = rabbitConfig["UserName"],
                Password = rabbitConfig["Password"],
                Port = AmqpTcpEndpoint.UseDefaultPort
            };

            var sharedConnection = new SharedConnection(connectionFactory);
            var publisher = new Publisher(sharedConnection, rabbitConfig["Exchange"]);

            var hashConfig = config.GetSection("Hash");
            var hashBatches = new List<List<string>>();

            var totalHashes = Convert.ToDecimal(Convert.ToInt32(hashConfig["TotalHashes"]));
            var hashesInBatch = Convert.ToDecimal(Convert.ToInt32(hashConfig["HashesInBatch"]));
            var remainingHashes = totalHashes;

            var numberOfBatches = Convert.ToInt32(Math.Ceiling(totalHashes / hashesInBatch));

            for (int i = 0; i < numberOfBatches; i++)
            {
                if (remainingHashes >= hashesInBatch)
                {
                    hashBatches.Add(GenerateRandomHashes(Convert.ToInt32(hashesInBatch)));
                    remainingHashes -= hashesInBatch;
                }
                else
                {
                    hashBatches.Add(GenerateRandomHashes(Convert.ToInt32(remainingHashes)));
                    remainingHashes -= remainingHashes;
                }
            }

            try
            {
                Parallel.ForEach(hashBatches, hashesBatch =>
                {
                    Parallel.ForEach(hashesBatch, hash =>
                    {
                        var hashes = new Hashes()
                        {
                            Date = DateTime.UtcNow,
                            SHA1 = hash
                        };

                        publisher.Publish(hashes);
                    });
                });
            }
            catch (Exception)
            {
                return Problem("Something went wrong. Please try again later.");
            }

            return Ok("Request has been submitted successfully.");
        }

        private List<string> GenerateRandomHashes(int count)
        {
            List<string> hashes = new List<string>();

            using (SHA1 sha1Hash = SHA1.Create())
            {
                for (int i = 0; i < count; i++)
                {
                    byte[] sourceBytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
                    byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
                    hashes.Add(BitConverter.ToString(hashBytes).Replace("-", string.Empty));
                }
            }

            return hashes;
        }
    }
}
