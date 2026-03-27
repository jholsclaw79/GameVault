using GameVault.Data;
using GameVault.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Components.Pages;

public partial class SystemPage
{
    [Parameter]
    public long PlatformId { get; set; }

    private GVPlatform? Platform { get; set; }
    private bool IsLoading { get; set; } = true;

    private string? LogoUrl
    {
        
        get
        {
            string? rawUrl = Platform?.PlatformLogo?.Url;
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return null;
            }

            string normalizedUrl = rawUrl.StartsWith("//") ? $"https:{rawUrl}" : rawUrl;
            return normalizedUrl.Replace("/t_thumb/", "/t_logo_med/").Replace(".jpg",".png");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;
        using AppDbContext context = await DbContextFactory.CreateDbContextAsync();
        Platform = await context.Platforms
            .Include(p => p.PlatformLogo)
            .Include(p => p.PlatformType)
            .Include(p => p.PlatformFamily)
            .FirstOrDefaultAsync(p => p.Id == PlatformId && p.IsTracked);
        IsLoading = false;
    }
}