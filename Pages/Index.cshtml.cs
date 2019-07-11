using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents;

namespace ravendb_live_counter.Pages
{
  public class IndexModel : PageModel
  {
    private readonly IDocumentStore store;
    public IndexModel(IDocumentStore store)
    {
      this.store = store;
    }
    public long Counter { get; set; }

    public async Task OnGet()
    {
      using (var session = store.OpenAsyncSession())
      {
        var counter = session.CountersFor("SiteStats");

        counter.Increment("views");

        await session.SaveChangesAsync();

        var views = await counter.GetAsync("views");

        if (views != null) {
          Counter = views.Value;
        } else {
          Counter = 0;
        }
      }
    }
  }
}
