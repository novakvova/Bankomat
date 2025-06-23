using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPrivate.JSON_Converter;

public class WithdrawModel : PageModel
{
    private readonly AtmClientService _atm;
    public WithdrawModel(AtmClientService atm) => _atm = atm;

    [BindProperty] public decimal Sum { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var resp = await _atm.SendAsync(new RequestType3 { Sum = Sum });

        if (resp?.PassCode == 1945)
            return RedirectToPage("Success");

        ErrorMessage = "Не вдалося зняти кошти.";
        return RedirectToPage("Failure");
    }
}