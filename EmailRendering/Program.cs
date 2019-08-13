using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using RazorLight;
using RazorViewLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EmailRendering
{
    public class Program
    {
        private const int NumberOfPeople = 10;
        private const int NumberOfEmails = 10;

        public static void Main(string[] args)
        {
            string view = "Views/Email.cshtml";
            EmailModel model = GetModel();
            RazorViewToStringRenderer razor = GetRazorRenderer();
            RazorLightEngine razorLight = GetRazorLightRenderer();

            int razorMilliseconds = Enumerable.Range(0, NumberOfEmails)
                .Sum(i =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    string html = razor.Render(view, model).Result;
                    return stopwatch.Elapsed.Milliseconds;
                });

            int razorLightMilliseconds = Enumerable.Range(0, NumberOfEmails)
                .Sum(i =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    string html = razorLight.CompileRenderAsync(view, model).Result;
                    return stopwatch.Elapsed.Milliseconds;
                });

            TimeSpan razorTime = TimeSpan.FromMilliseconds(razorMilliseconds);
            TimeSpan razorLightTime = TimeSpan.FromMilliseconds(razorLightMilliseconds);
        }

        private static EmailModel GetModel()
        {
            List<PersonModel> people = Enumerable.Range(0, NumberOfPeople)
                .Select(i => new PersonModel() { FirstName = "First" + i, LastName = "Last" + i })
                .ToList();

            return new EmailModel() { People = people };
        }

        private static RazorViewToStringRenderer GetRazorRenderer()
        {
            PhysicalFileProvider fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            HostingEnvironment hostingEnvironment = new HostingEnvironment { ApplicationName = Assembly.GetEntryAssembly().GetName().Name };
            ServiceProvider serviceProvider = new ServiceCollection()
                .Configure<RazorViewEngineOptions>(options =>
                {
                    options.FileProviders.Clear();
                    options.FileProviders.Add(fileProvider);
                })
                .AddSingleton<IHostingEnvironment>(hostingEnvironment)
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.AspNetCore"))
                .AddSingleton<RazorViewToStringRenderer>()
                .AddLogging()
                .AddMvc().Services
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<RazorViewToStringRenderer>();
        }

        private static RazorLightEngine GetRazorLightRenderer()
        {
            return new RazorLightEngineBuilder()
                .SetOperatingAssembly(Assembly.GetExecutingAssembly())
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();
        }
    }
}
