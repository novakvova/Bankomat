using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;


public class LoginAuthModel : PageModel
{
    private readonly AtmClientService _atm;
    public LoginAuthModel(AtmClientService atm) => _atm = atm;

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
        var request = new RequestType2
        {
            NumberCard = numberCard,
            FirstName = FirstName,
            LastName = LastName,
            FatherName = FatherName,
            PinCode = PinCode
        };

        var resp = await _atm.SendAsync(request);

        if (resp?.PassCode == 1945)
        {
            HttpContext.Session.SetString("Authorized", "true");
            HttpContext.Session.SetString("UserName", FirstName);
            HttpContext.Session.SetString("CardNumber", numberCard.ToString());

            ModelState.Clear();
            return RedirectToPage("Dashboard");
        }

        if (resp?.PassCode is 1914 or 1918)
        {
            ErrorMessage = "Доступ заблоковано сервером.";
            return Page();
        }

        ErrorMessage = "Невірні дані. Спробуйте знову.";
        return Page();
    }
}