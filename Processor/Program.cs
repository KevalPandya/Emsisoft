using Common;
using Common.Interfaces;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Processor;
using Processor.Interfaces;
using RabbitMQ.Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var rabbitConfig = builder.Build().GetSection("RabbitMQ");

        var exchangeName = rabbitConfig["Exchange"];
        var queueName = rabbitConfig["Queue"];
        var deadLetterExchange = rabbitConfig["DeadLetterExchange"];
        var deadLetterQueue = rabbitConfig["DeadLetterQueue"];
        var subscriberOptions = new SubscriberOptions(exchangeName, queueName, deadLetterExchange, deadLetterQueue);

        var connectionFactory = new ConnectionFactory()
        {
            HostName = rabbitConfig["HostName"],
            UserName = rabbitConfig["UserName"],
            Password = rabbitConfig["Password"],
            Port = AmqpTcpEndpoint.UseDefaultPort,
            DispatchConsumersAsync = true
        };

        var consumerConfig = builder.Build().GetSection("Consumer");
        int consumerInstances = Convert.ToInt32(consumerConfig["Instances"]);

        var channel = System.Threading.Channels.Channel.CreateBounded<Hashes>(100);

        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(builder.Build().GetConnectionString("DefaultConnection")))
            .AddSingleton<IMessagesRepository, MessagesRepository>()
            .AddSingleton(subscriberOptions)
            .AddSingleton<IConnectionFactory>(connectionFactory)
            .AddSingleton<ISharedConnection, SharedConnection>()
            .AddSingleton<ISubscriber, Subscriber>()
            .AddSingleton(channel)
            .AddSingleton<IProducer>(ctx =>
            {
                var channel = ctx.GetRequiredService<System.Threading.Channels.Channel<Hashes>>();
                var logger = ctx.GetRequiredService<ILogger<Producer>>();
                
                return new Producer(channel.Writer, logger);
            })
            .AddSingleton<IEnumerable<IConsumer>>(ctx =>
            {
                var channel = ctx.GetRequiredService<System.Threading.Channels.Channel<Hashes>>();
                var logger = ctx.GetRequiredService<ILogger<Consumer>>();
                var repo = ctx.GetRequiredService<IMessagesRepository>();
                
                var consumers = Enumerable.Range(1, consumerInstances)
                                          .Select(i => new Consumer(channel.Reader, logger, i, repo))
                                          .ToArray();
                
                return consumers;
            })
            .AddHostedService<BackgroundWorker>();
    })
    .Build();

await host.RunAsync();
