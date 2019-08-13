using EmailRendering.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using RazorLight;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EmailRendering
{
    public class Program
    {
        private const int NumberOfPeople = 1000;
        private const int NumberOfEmails = 1000;
        private const int Batches = 10;

        public async static Task Main(string[] args)
        {
            string view = "Views.EmbedEmail.cshtml";
            EmailModel model = GetModel();
            RazorViewToStringRenderer razor = GetRazorRenderer();
            RazorLightEngine razorLight = GetRazorLightRenderer();
            Stopwatch razorStopwatch = new Stopwatch();
            Stopwatch razorLightStopwatch = new Stopwatch();
            IEnumerable<Task<string>> tasks;

            for (int i = 0; i < Batches; i++)
            {
                razorStopwatch.Start();
                tasks = Enumerable.Range(0, NumberOfEmails / Batches)
                    .Select(_ => razor.Render(view, model));
                await Task.WhenAll(tasks);
                razorStopwatch.Stop();


                razorLightStopwatch.Start();
                tasks = Enumerable.Range(0, NumberOfEmails / Batches)
                    .Select(_ => razorLight.CompileRenderAsync(view, model));
                await Task.WhenAll(tasks);
                razorLightStopwatch.Stop();
            }
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
            IFileProvider fileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            HostingEnvironment hostingEnvironment = new HostingEnvironment { ApplicationName = Assembly.GetEntryAssembly().GetName().Name };
            ServiceProvider serviceProvider = new ServiceCollection()
                .Configure<RazorViewEngineOptions>(options =>
                {
                    options.FileProviders.Clear();
                    options.FileProviders.Add(fileProvider);
                })
                .AddSingleton<IHostingEnvironment>(hostingEnvironment)
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddSingleton<DiagnosticSource>(new DiagnosticListener(nameof(Microsoft.AspNetCore)))
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
                .UseEmbeddedResourcesProject(Assembly.GetExecutingAssembly())
                .UseMemoryCachingProvider()
                .Build();
        }
    }
}
