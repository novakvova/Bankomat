using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;
using VisualClient.Models;

public class LoginCardModel : PageModel
{
    private readonly AtmClientService _atm;

    public LoginCardModel(AtmClientService atm) => _atm = atm;

    [BindProperty]
    public long CardNumber { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var response = await _atm.SendAsync(new RequestType1 { NumberCard = CardNumber });

        if (response == null || response.PassCode != 1945)
        {

            return RedirectToPage("/CardNotFound");
        }
        HttpContext.Session.SetString("CardNumber", CardNumber.ToString());
        return RedirectToPage("/LoginAuth");

    }

}
