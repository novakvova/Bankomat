using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;

public class LoginCardModel : PageModel
{
    private readonly AtmClientService _atm;
    public LoginCardModel(AtmClientService atm) => _atm = atm;

    [BindProperty]
    public long CardNumber { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var resp = await _atm.SendAsync(new RequestType1 { NumberCard = CardNumber });

        if (resp == null || resp.PassCode is 1914 or 1918)
        {
            ErrorMessage = "Сервер заблокував вас.";
            return Page();
        }

        TempData["CardNumber"] = CardNumber.ToString();

        if (resp.PassCode == 1789)
            return RedirectToPage("Register");

        if (resp.PassCode == 1945)
            return RedirectToPage("LoginAuth");

        ErrorMessage = "Невідома відповідь.";
        return Page();
    }
}