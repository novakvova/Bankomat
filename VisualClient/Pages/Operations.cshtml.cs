using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyClient.JSON_Converter;
using MyPrivate.JSON_Converter;

public class OperationsModel : PageModel
{
    private readonly AtmClientService _atm;

    public OperationsModel(AtmClientService atm)
    {
        _atm = atm;
    }

    public string? ResponseComment { get; set; }

    public async Task OnGetAsync()
    {
        var request = new RequestType5();
        var response = await _atm.SendAsync(request);

        ResponseComment = response?.Comment ?? "Банкомат не надіслав відповідь.";
    }
}