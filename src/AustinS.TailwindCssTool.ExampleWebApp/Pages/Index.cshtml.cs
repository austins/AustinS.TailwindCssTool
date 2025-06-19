using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AustinS.TailwindCssTool.ExampleWebApp.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}
