using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;

public class RegisterModel : PageModel
{
    private readonly AtmClientService _atm;
    public RegisterModel(AtmClientService atm) => _atm = atm;

    [BindProperty] public long CardNumber { get; set; }
    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public string FatherName { get; set; } = "";
    [BindProperty] public long PinCode { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (TempData["CardNumber"] is not string cardNumber || !long.TryParse(cardNumber, out var numberCard))
        {
            return RedirectToPage("LoginCard");
        }
        var check = await _atm.SendAsync(new RequestType1 { NumberCard = numberCard });

        if (check?.PassCode == 1945)
        {
            ErrorMessage = "Картка вже існує.";
            return Page();
        }

        var req = new RequestType2
        {
            NumberCard = numberCard,
            FirstName = FirstName,
            LastName = LastName,
            FatherName = FatherName,
            PinCode = PinCode
        };

        var resp = await _atm.SendAsync(req);

        if (resp?.PassCode == 1945)
        {
            HttpContext.Session.SetString("Authorized", "true");
            HttpContext.Session.SetString("UserName", FirstName);
            HttpContext.Session.SetString("CardNumber", numberCard.ToString());
            return RedirectToPage("Dashboard");
        }

        ErrorMessage = "Не вдалося зареєструвати.";
        return Page();
    }
}