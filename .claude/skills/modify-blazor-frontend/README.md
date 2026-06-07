# modify-blazor-frontend Skill

This skill enables Claude to generate and modify Blazor WASM frontend components for the MobilityCenter project efficiently, without wasting context on the entire solution.

## Skill Structure

```
modify-blazor-frontend/
├── SKILL.md                          # Main skill instructions (loaded on trigger)
├── README.md                         # This file
├── references/
│   ├── frontend-patterns.md          # Component library, service template, page patterns
│   ├── api-contract.md               # Full API endpoint specification
│   └── (other docs as needed)
├── scripts/
│   └── extract-api-endpoints.ps1     # Helper: Extract endpoints from API controllers
└── evals/
    └── evals.json                    # Test cases for evaluating skill quality
```

## How the Skill Works

1. **Trigger:** User asks Claude to create/modify frontend components or integrate APIs
2. **Load:** Claude loads `SKILL.md` + references (frontend-patterns, api-contract)
3. **Generate:** Claude generates `.razor` and `.cs` files following the patterns
4. **Output:** User copies generated files into their project

The skill avoids loading the entire codebase — only the frontend folder is referenced when needed for implementation patterns.

## Maintaining This Skill

### When to Update References

**Update `frontend-patterns.md` if:**
- New reusable components are added to Components/
- Service patterns change (e.g., new DI setup)
- Page structure conventions change
- CSS variables are added/removed

**Update `api-contract.md` if:**
- New endpoints are added to any controller
- Response DTO structures change
- Authentication/authorization rules change
- Error response format changes

**Update `SKILL.md` if:**
- Common tasks or troubleshooting change
- Code generation patterns need to be clarified
- New component types are available

### Keeping References Accurate

Before making skill improvements, verify the references match current code:

```bash
# Check if frontend-patterns.md is still accurate
# Review actual components in: src/MobilityCenter.Frontend/Components/
# Review actual services in: src/MobilityCenter.Frontend/Services/

# Check if api-contract.md is still accurate
# Review actual endpoints in: src/MobilityCenter.API/Controllers/
```

### Testing the Skill

Use the test cases in `evals/evals.json` to evaluate skill quality:

1. Run each test prompt through Claude **with** the skill loaded
2. Verify generated code:
   - Compiles without errors
   - Follows project conventions
   - Matches API endpoints correctly
   - Uses existing components and services
3. Run a baseline **without** the skill to compare
4. Gather user feedback on generated code quality

### Updating Test Cases

Add new test cases to `evals/evals.json` when:
- New component types should be covered
- New API integration patterns emerge
- Common user requests aren't represented
- Edge cases are discovered

## Common Issues and Fixes

### Issue: Generated code has wrong API endpoint
**Cause:** api-contract.md is outdated
**Fix:** Verify endpoint in actual controller, update api-contract.md

### Issue: Generated component doesn't match style
**Cause:** frontend-patterns.md missing a component or pattern
**Fix:** Review actual components in Components/, add pattern to reference

### Issue: Service doesn't inject correctly
**Cause:** Skill didn't mention Program.cs registration
**Fix:** Ensure SKILL.md "Code Generation Rules" section mentions DI registration

## Reference File Format

### api-contract.md

Structure:
```markdown
# API Endpoint Name
```
GET /api/endpoint
Body: { ... }
Response 200: { ... }
Response 400: { ... }
```
```

Keep it:
- Accurate (matches actual controller code)
- Complete (all endpoints listed)
- Specific (exact parameter names, types, status codes)

### frontend-patterns.md

Structure:
```markdown
## Component Name

Description

Example usage:
```razor
<ComponentName Param="value" />
```

Parameters:
- `ParamName: type` — description
```

Keep it:
- Up-to-date (new components added)
- Concise (focus on usage, not implementation)
- Realistic (examples that actually work)

## Versioning

This skill doesn't use semantic versioning. Instead, track changes inline:
- Update references whenever underlying codebase changes
- Run evals periodically to ensure quality
- Note major improvements in SKILL.md header comment

## Future Enhancements

Potential improvements:
- Add bundled SVG icons library reference
- Add example of state management (cascading parameters, service-based state)
- Add dark mode CSS variable reference
- Create helper script to auto-generate frontend-patterns.md from Components/
- Add example of using EditForm for validation
- Document PWA/offline patterns if implemented
