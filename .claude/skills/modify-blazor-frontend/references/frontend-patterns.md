# Paraki Frontend Patterns

This document captures the architectural patterns, component library, and service conventions used in the Blazor WASM frontend.

## Project Structure

```
src/Paraki.Frontend/
├── Components/         # Reusable UI components (.razor)
├── Pages/             # Full page components (@page directive)
├── Services/          # HTTP and business logic services (.cs)
├── Models/            # Data transfer objects and DTOs (.cs)
├── Layout/            # Layout components (MainLayout.razor)
├── wwwroot/           # Static assets, CSS variables
└── Program.cs         # DI setup
```

## Component Library

### Input Components

**WfInput.razor** — Unified text/password input with optional icon
```razor
<WfInput 
    Placeholder="Enter email"
    IconClass="fa-envelope"
    Type="email"
    Value="@email"
    ValueChanged="@((v) => email = v)" />
```

**Parameters:**
- `Placeholder: string` — placeholder text
- `IconClass: string` — FontAwesome class (e.g., "fa-envelope"), optional
- `Type: string` — HTML input type (default: "text")
- `Value: string` — two-way binding
- `ValueChanged: EventCallback<string>` — binding callback

### Button Components

**BtnP.razor** — Primary button (full-width, brand color)
```razor
<BtnP @onclick="HandleSubmit">Send</BtnP>
```

**BtnO.razor** — Outline button (secondary)
```razor
<BtnO @onclick="Cancel">Cancel</BtnO>
```

### Layout Components

**StatusBar.razor** — Fixed header bar with status indicators

**BottomNav.razor** — Mobile-style bottom navigation

**MainLayout.razor** — Root layout wrapper

### Utility Components

**Chip.razor** — Small badge/tag for filters or labels
```razor
<Chip>Free Access</Chip>
```

**Stars.razor** — 5-star rating display
```razor
<Stars Rating="4" />
```

**CheckItem.razor** — Checkbox with label, used in filter lists
```razor
<CheckItem Label="Power Outlet" Checked="@hasPower" />
```

**RadioItem.razor** — Radio button with label

**ImgPH.razor** — Image with placeholder/loading state

## Service Patterns

### HTTP Client Setup

DI in `Program.cs`:
```csharp
builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
```

Inject as: `HttpClient http` (already configured with auth token header)

### Service Template

```csharp
using System.Net.Http.Json;
using Paraki.Frontend.Models;

namespace Paraki.Frontend.Services;

public class XyzService(HttpClient http)
{
    public async Task<XyzDto?> GetXyzAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<XyzDto>("api/endpoint");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> CreateXyzAsync(XyzDto dto)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/endpoint", dto);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiError>();
                return err?.Message ?? "Unknown error";
            }
            return null; // null = success
        }
        catch
        {
            return "Connection error";
        }
    }

    private record ApiError(string Message);
}
```

### Authentication Pattern

Inject `AuthService`:
```csharp
@inject AuthService AuthSvc

@code {
    protected override async Task OnInitializedAsync()
    {
        var userInfo = await AuthSvc.GetUserInfoAsync();
        if (userInfo == null) 
            Nav.NavigateTo("/login");
    }
}
```

The `AuthService` handles:
- Token persistence via `LocalStorageService`
- JWT state updates via `JwtAuthStateProvider`
- Automatic token injection on all HTTP requests via `AuthTokenHandler`

## Page Structure Pattern

```razor
@page "/page-route"
@inject NavigationManager Nav
@inject XyzService XyzSvc

<!-- Header -->
<div style="background:var(--bg); border-bottom:1px solid var(--border);">
    <StatusBar />
    <div style="display:flex; align-items:center; gap:14px; padding:4px 20px 14px;">
        <button @onclick="@(() => Nav.NavigateTo("/"))" 
                style="width:36px; height:36px; border-radius:var(--r-full); 
                        background:var(--bg-page); border:none; cursor:pointer; 
                        display:flex; align-items:center; justify-content:center;">
            <i class="fa-solid fa-chevron-left"></i>
        </button>
        <span style="font-size:17px; font-weight:700;">Page Title</span>
    </div>
</div>

<!-- Content -->
<div style="padding:20px;">
    @if (_isLoading)
    {
        <p>Loading...</p>
    }
    else
    {
        <!-- Your content here -->
    }
</div>

@code {
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        // Load data
        await Task.Delay(100); // simulate load
        _isLoading = false;
    }
}
```

## CSS Variables

Theme variables available globally (in `wwwroot/css`):

```css
/* Colors */
--bg              /* Page background */
--bg-page         /* Secondary background */
--border          /* Border color */
--border-2        /* Secondary border */
--text            /* Primary text */
--text-2          /* Secondary text (emphasized) */
--text-3          /* Tertiary text */
--text-4          /* Quaternary text (subtle) */
--brand           /* Brand color (primary) */
--brand-soft      /* Brand light background */
--gold            /* Accent color (ratings) */

/* Spacing */
--r               /* Standard border-radius */
--r-lg            /* Large border-radius */
--r-full          /* Full border-radius (circles) */

/* Effects */
--shadow-sm       /* Small shadow */
--font            /* Font family */
```

## State Management Pattern

**Local State:** Use `@code` with properties and `StateHasChanged()` for manual updates

**Persistent State:** Use `LocalStorageService` for auth tokens, user info

**Session State:** Use `CascadingParameters` for auth state from `JwtAuthStateProvider`

## Data Models Pattern

Models live in `Models/` namespace. Match API response DTO names:

```csharp
namespace Paraki.Frontend.Models;

public class XyzDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateXyzRequest
{
    public string Name { get; set; }
}
```

## Navigation Pattern

Use `NavigationManager.NavigateTo()`:
```csharp
Nav.NavigateTo("/details/123");
Nav.NavigateTo("/", forceLoad: true); // Full page reload
```

## Forms Pattern

No form component library — build inline with `WfInput`, `BtnP`, validation state:

```razor
<div>
    <WfInput Placeholder="Name" Value="@_name" ValueChanged="@((v) => _name = v)" />
    @if (!string.IsNullOrEmpty(_error))
    {
        <div style="color:red; font-size:12px; margin-top:4px;">@_error</div>
    }
    <BtnP @onclick="Submit">Send</BtnP>
</div>

@code {
    private string _name = "";
    private string _error = "";

    private async Task Submit()
    {
        _error = await _service.CreateAsync(_name) ?? "";
    }
}
```

## Error Handling Convention

Services return `string?`:
- `null` = success
- Non-null string = error message to display to user

Pages show errors in simple `<div>` with red color.

## API Request/Response Pattern

**Request:** `PostAsJsonAsync<T>(url, dto)` or `GetFromJsonAsync<T>(url)`

**Response:** 
- Success: JSON-deserialized to DTO
- Error: Read as `ApiError` record with `Message` property

Example error response from API:
```json
{ "error": true, "message": "User not found" }
```

Service handles and returns message to UI.
