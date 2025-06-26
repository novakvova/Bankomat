using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;
using VisualClient.Models;

public class RegisterModel : PageModel
{
    private readonly AtmClientService _atm;
    public RegisterModel(AtmClientService atm) => _atm = atm;
    [BindProperty(SupportsGet = true)] public long CardNumber { get; set; }
    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public string FatherName { get; set; } = "";
    [BindProperty] public long PinCode { get; set; }

    public string? ErrorMessage { get; set; }
    public async Task OnGetAsync()
    {
        var rnd = new Random();
        int a = 10000000, b = 99999999;
        long number;
        ServerResponse? response;

        do
        {
            number = long.Parse($"{rnd.Next(a, b)}{rnd.Next(a, b)}");
            response = await _atm.SendAsync(new RequestType1 { NumberCard = number });
        }
        while (response?.PassCode == 1945); // 1945 - код, що означає, що картка вже існує
        var check = await _atm.SendAsync(new RequestType1 { NumberCard = number });

        CardNumber = number;
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var req = new RequestType2
        {
            NumberCard = CardNumber,
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
            HttpContext.Session.SetString("CardNumber", CardNumber.ToString());
            return RedirectToPage("LoginCard");
        }

        ErrorMessage = "Не вдалося зареєструвати.";
        return Page();
    }
}
