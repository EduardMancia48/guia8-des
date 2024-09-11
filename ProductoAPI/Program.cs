
using Microsoft.EntityFrameworkCore;
using ProductoAPI.Models;
using StackExchange.Redis;

namespace ProductoAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<ProductoContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddStackExchangeRedisCache(opciones =>
            {
                opciones.Configuration = builder.Configuration.GetConnectionString("redis");
            });

            //Registrar IconnectionMultiplexer
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("redis"), true);
                return ConnectionMultiplexer.Connect(configuration);
            });


            builder.Services.AddOutputCache();




            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseOutputCache();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
