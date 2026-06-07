---
name: modify-blazor-frontend
description: Build and modify Blazor WASM components for MobilityCenter. Use this whenever the user wants to create new pages, components, services, or integrate API endpoints into the frontend. This skill knows the component library, service patterns, and full API contract to avoid wasting context analyzing the entire codebase. Automatically identifies relevant API endpoints and reuses existing patterns from the frontend.
---

# Modify Blazor Frontend

Create, modify, and integrate Blazor WebAssembly components for MobilityCenter. This skill provides a focused context on the **frontend architecture only**, with the API contract and component library pre-loaded, so you don't need to read the entire codebase.

## When to Use This Skill

- **New page:** "Create a page showing my profile with photo upload"
- **New component:** "Add a filter sidebar for bike rack search"
- **Service integration:** "Create a service to fetch user ratings and display them"
- **API integration:** "Wire up the photo upload endpoint to a form"
- **Bug fix in frontend:** "Fix the login form validation"
- **Styling:** "Update the theme colors for the rating stars"

This skill **automatically:**
- Identifies which API endpoints you need (from the spec)
- Applies existing patterns from the codebase (components, services, error handling)
- Generates `.razor`, `.cs`, and `.cs` model files ready to copy into the project
- Suggests where to register new services in `Program.cs` if needed

## How It Works

### Step 1: Describe What You Want

Give a clear description of the feature:
- What should it do?
- Which page/route?
- Which API endpoints does it use?
- Any specific design or interactions?

**Example:** "Create a page at `/minha-avaliacao` that shows all my ratings. Let me see the bike rack name, my star rating, and my comment. Include a button to edit each rating and a button to delete it."

### Step 2: Skill Reads Context

The skill automatically reads:
1. **API Contract** (`references/api-contract.md`) — All endpoints, request/response formats
2. **Frontend Patterns** (`references/frontend-patterns.md`) — Component library, service template, page structure
3. **Existing codebase** — For reference implementations (only the Frontend folder, not the entire solution)

This keeps context efficient — no time reading unrelated backend layers.

### Step 3: Generate Code

The skill produces:
- **`.razor` files** — UI components ready to integrate
- **`.cs` service files** — API communication services (if needed)
- **`.cs` model files** — DTOs matching API responses (if needed)
- **Registration code** — Snippets for `Program.cs` DI if you added new services
- **Integration instructions** — Where to put files, what to import

### Step 4: Copy Into Project

All generated code follows the project's structure and patterns. You paste the files directly into:
```
src/MobilityCenter.Frontend/
├── Pages/              # .razor pages
├── Components/         # .razor reusable components
├── Services/           # .cs services
└── Models/             # .cs DTOs
```

No formatting or refactoring needed — it's ready to use.

## Code Generation Rules

### Component Files

**File:** `Pages/YourPage.razor`

```razor
@page "/route"
@inject NavigationManager Nav
@inject YourService YourSvc

<!-- Your HTML here -->

@code {
    // Your C# logic here
}
```

**Patterns followed:**
- Use existing components (`WfInput`, `BtnP`, `Stars`, etc.)
- Apply CSS variables for colors
- Handle errors by returning `string?` from services
- Show errors in a styled `<div>`
- Authenticate via `AuthService` or check for null `UserInfo`

### Service Files

**File:** `Services/YourService.cs`

```csharp
using System.Net.Http.Json;
using MobilityCenter.Frontend.Models;

namespace MobilityCenter.Frontend.Services;

public class YourService(HttpClient http)
{
    public async Task<YourDto?> GetYourAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<YourDto>("api/endpoint");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> CreateYourAsync(YourDto dto)
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

**Patterns followed:**
- Constructor injection of `HttpClient http` (already configured with auth token)
- Methods return `Task<T?>` for data or `Task<string?>` for error messages
- HTTP methods: `GetFromJsonAsync<T>`, `PostAsJsonAsync<T>`, etc.
- Error handling: try-catch, read error message from response, return to caller
- Private records for internal response structures

### Model Files

**File:** `Models/YourDto.cs`

```csharp
namespace MobilityCenter.Frontend.Models;

public class YourDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Patterns followed:**
- Match API response DTOs exactly (same property names, types)
- Auto-properties with public getters/setters
- Use `Guid` for IDs, `DateTime` for timestamps
- Nullable fields: use `string?` or `T?`

### Program.cs Registration

If you create a new service, register it in `Program.cs`:

```csharp
builder.Services.AddScoped<YourService>();
```

Add this line before `await builder.Build().RunAsync();`

## Reference Documents

- **API Contract:** See `references/api-contract.md` for all endpoints, request/response formats, error codes
- **Frontend Patterns:** See `references/frontend-patterns.md` for component library, service template, page structure, state management, CSS variables, and error handling conventions

Read these **before** asking for code changes. They're loaded in the skill context, so questions about "what component should I use?" or "does this endpoint exist?" can be answered from the references.

## Common Tasks

### Task: Create a New Page

**What you say:** "Create a page at `/my-ratings` that lists all my ratings with the bike rack name, star rating, and comment. Include an edit button."

**Skill does:**
1. Identifies endpoint: `GET /api/usuarios/me/avaliacoes`
2. Identifies nested endpoint for details: `GET /api/bicicletarios/{id}`
3. Creates `Pages/MeuRatings.razor`
4. Creates `Services/RatingService.cs` to fetch data
5. Creates `Models/RatingDto.cs`, `BicicletarioDto.cs`
6. Returns `.razor` and `.cs` files with full integration code

### Task: Add API Integration to Existing Page

**What you say:** "Wire up the login form in `Pages/Login.razor` to use the POST /api/auth/login endpoint. On success, redirect to home; on error, show the error message."

**Skill does:**
1. Reads the current `Pages/Login.razor`
2. Identifies `AuthService` is already available
3. Suggests how to call `await AuthSvc.LoginAsync(email, password)`
4. Shows error handling and navigation logic
5. Returns modified `.razor` with minimal changes

### Task: Create a Reusable Component

**What you say:** "Create a reusable `RatingCard.razor` component that displays a single rating (bike rack name, stars, comment). It should take parameters for the rating data."

**Skill does:**
1. Creates `Components/RatingCard.razor`
2. Defines `@Parameter` properties for rating data
3. Styles consistently with existing components
4. Returns `.razor` ready to use in pages

### Task: Fix a Bug

**What you say:** "The file upload form in the profile page isn't sending the file to the API. The input has type='file' but there's no service method to handle it. Fix it."

**Skill does:**
1. Reads the current page
2. Identifies the gap: missing service method for multipart form data
3. Creates/updates the service with file upload logic
4. Shows the corrected page code
5. Explains what changed

## Troubleshooting

### "I got a compile error after pasting the code"

Check:
1. **Namespace:** Make sure the file is in the right folder (`Pages/`, `Services/`, `Models/`)
2. **Imports:** Verify `@using` statements at the top match your namespace
3. **DI registration:** If it's a new service, register it in `Program.cs`
4. **Model names:** Ensure DTOs match API response property names exactly (case-sensitive)

### "The API call returns null"

Likely causes:
1. **Auth token missing:** The API requires `[Authorize]`. Check `AuthService.GetUserInfoAsync()` returns non-null
2. **Wrong endpoint:** Verify the URL in the service matches the API contract exactly
3. **Network error:** Check browser DevTools Network tab for actual error response
4. **CORS issue:** Ensure `api` HttpClient is configured (it should be in `Program.cs`)

### "The component doesn't look right"

Check:
1. **CSS variables:** Are `var(--bg)`, `var(--text)`, etc. defined in your `wwwroot/css`? They should be inherited from existing pages.
2. **Component imports:** Did you add `@using MobilityCenter.Frontend.Components` at the top of your page?
3. **Responsive design:** Test on mobile — the frontend uses a mobile-first layout

### "How do I test the code locally?"

```bash
# Terminal 1: Start the API (already running on port 5000)
dotnet run --project ./src/MobilityCenter.API

# Terminal 2: Start the Frontend dev server
dotnet run --project ./src/MobilityCenter.Frontend --urls http://localhost:5200

# Open browser to http://localhost:5200
```

The frontend dev server has hot reload — save a `.razor` file and the browser updates automatically.

## Important Notes

- **No TypeScript:** Blazor uses C# for all logic, not TypeScript or JavaScript
- **No Node/npm:** This is a pure .NET project — no build tools beyond `dotnet`
- **Component names:** Start with capital letter (PascalCase) — `MyComponent.razor`, not `my-component.razor`
- **Async/await:** All service methods are async; use `await` in components
- **CSS:** Inline styles only — no separate `.css` files per component (matches existing pattern)
- **State management:** Use local `@code` properties for component state; use `LocalStorageService` for persistence
- **Errors are strings:** Services return `string?` for errors, not exceptions. Display as simple `<div>`

## Getting Help

If the generated code doesn't compile or doesn't work:
1. **Copy the exact error message** from the compiler or browser console
2. **Show the file** where the error occurs
3. **Ask for a fix** — be specific about what you changed since generating the code

Example: *"I copied `RatingService.cs` to the Services folder. When I try to inject it in my page with `@inject RatingService RatingSvc`, I get 'The type or namespace name 'RatingService' could not be found.' How do I fix this?"*

---

**Note:** This skill focuses on the **Blazor WASM frontend only**. For backend changes (API endpoints, database models), consult the `/run-mobilityCenter` or API modification skills.
