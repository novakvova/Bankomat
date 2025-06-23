using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyClient.JSON_Converter;
using MyPrivate.JSON_Converter;

public class DashboardModel : PageModel
{
    private readonly AtmClientService _atm;
    public DashboardModel(AtmClientService atm) => _atm = atm;

    public string UserName { get; set; } = "Клієнт";
    public string? BalanceMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetString("Authorized") != "true")
            return RedirectToPage("LoginCard");

        UserName = HttpContext.Session.GetString("UserName") ?? "Клієнт";

        var resp = await _atm.SendAsync(new RequestType5());

        if (resp?.PassCode == 1945)
            BalanceMessage = resp.Comment;
        else
            BalanceMessage = "Помилка отримання балансу";

        return Page();
    }
}