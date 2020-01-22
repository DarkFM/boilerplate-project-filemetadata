using System;
using System.Net.Mime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text;
using System.IO;

namespace FileMetaData
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = MediaTypeNames.Text.Html;
                    await context.Response.WriteAsync(File.ReadAllText(env.ContentRootPath + "/Views/index.html"));
                });

                endpoints.MapPost("/", async context =>
                {
                    bool headerSet = context.Request.Headers.TryGetValue("Content-Type", out var header);
                    bool hasRightType = header.ToString().Contains("multipart/form-data");
                    if (!(headerSet && hasRightType))
                    {
                        await WriteResponseAsync(
                            context,
                            MediaTypeNames.Application.Json,
                            new { Error = "Please upload a file" },
                            StatusCodes.Status400BadRequest
                        );
                        return;
                    }

                    var files = context.Request.Form.Files;
                    if (files.Count == 0)
                    {
                        await WriteResponseAsync(
                            context,
                            MediaTypeNames.Application.Json,
                            new { Error = "Empty file/No file uploaded" },
                            StatusCodes.Status400BadRequest
                            );
                        return;
                    }

                    var file = files[0];
                    var response = new
                    {
                        Name = file.FileName,
                        Type = file.ContentType,
                        Size = file.Length
                    };
                    await WriteResponseAsync(context, MediaTypeNames.Application.Json, response, StatusCodes.Status200OK);
                });
            });
        }

        private static async Task WriteResponseAsync(HttpContext context, string contentType, object response, int httpCode)
        {
            context.Response.ContentType = contentType;
            context.Response.StatusCode = httpCode;
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string content = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(content, Encoding.UTF8);
        }
    }
}
