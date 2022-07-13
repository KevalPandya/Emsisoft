# Emsisoft - Test Application

### Technology Stack
- C# and .NET 6.0
- SQL Server
- RabbitMQ

### Implemented Features
- REST API for GET & POST:
    - GET '/hashes' : This endpoint will return number of hashes genereted by day.
    - POST '/hashes' : This endpoint will generate 40 random SHA1 hashes and sends them to RabbitMQ queue for further processing.
        - To modify the number of hashes generated, update **TotalHashes** in **appsettings.json** (Project: **APIHandler**).
- Created background worker application to run 4 instances of consumer to process the hashes and store them in database.
    - To update the number of instances, update **Instances** in **appsettings.json** (Project: **Processor**).
- Database is created with **Code First** approach of **Entity Framework Core**.
    - Update the connection string in **appsettings.json** in both projects (**APIHandler** & **Processor**)
    - To create the same database on your end, kindly run the migration and it will create a table **Hashes** (Id, Date, SHA1).

### Optionally Implemented Features
- Split generated hashes into batches and send them into RabbitMQ in parallel during API request.
    - You can also update the number of hashes that are in one batch. Update **HashesInBatch** in **appsettings.json** (Project: **APIHandler**).
