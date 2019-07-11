using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Changes;

namespace ravendb_live_counter
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<IDocumentStore>(provider =>
      {
        var isAzure = System.Environment.GetEnvironmentVariable("AZURE") != null;

        X509Certificate2 certificate;

        if (!isAzure)
        {
          certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(System.Environment.GetEnvironmentVariable("RAVENDB_KAMRANICUS_CERT"));
        }
        else
        {
          X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
          certStore.Open(OpenFlags.ReadOnly);
          X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                     X509FindType.FindByThumbprint,
                                 // I know this will be a single thumbprint, not *
                                 // see: https://azure.microsoft.com/de-de/blog/using-certificates-in-azure-websites-applications/
                                 System.Environment.GetEnvironmentVariable("WEBSITES_LOAD_CERTIFICATES"),
                                     false);

          certificate = certCollection[0];
        }

        var store = new DocumentStore()
        {
          Urls = new[] { "https://a.free.kamranicus.ravendb.cloud" },
          Database = "livecounter",
          Certificate = certificate
        };

        store.Initialize();

        // Subscribe to views counter
        store.Changes().ForCounterOfDocument("SiteStats", "views").Subscribe(
          change =>
          {
            switch (change.Type)
            {
              case CounterChangeTypes.Increment:
                provider.GetService<IHubContext<CounterHub>>().Clients.All.SendAsync("UpdateCounter", change.Value);
                break;
            }
          }
        );

        return store;
      });

      services.Configure<CookiePolicyOptions>(options =>
      {
        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
      });


      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

      services.AddSignalR()
        .AddAzureSignalR();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();
      app.UseCookiePolicy();

      app.UseMvc();

      app.UseAzureSignalR(routes =>
{
  routes.MapHub<CounterHub>("/counterHub");
});
    }
  }
}
