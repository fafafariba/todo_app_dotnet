# Explanations

A running log of concepts, decisions, and deep dives from building this project.

---

## Soft deletes

Todos and users are never hard-deleted. Instead a `DeletedAt` timestamp is set. Benefits:
- Enables undo/restore
- Preserves audit history
- EF Core's global query filters automatically exclude soft-deleted rows (`HasQueryFilter(t => t.DeletedAt == null)`), so every query is safe by default without manual filtering

---

## CompletedAt instead of a boolean

Rather than a `completed: boolean` flag, todos track a `CompletedAt` timestamp. This gives completion time for free and is more useful for sorting, filtering, and displaying history.

---

## Service returning null vs throwing on not-found

When a todo isn't found, the service returns `null` instead of throwing. This keeps the service layer clean — callers get a consistent `null | result` interface and decide how to handle it (the controller returns a 404). Exceptions are reserved for truly exceptional situations, not expected business cases like "record doesn't exist."

---

## Service layer (thin pass-through)

The service layer currently handles data access directly via EF Core with minimal added logic. It's kept intentionally — as the app grows, business logic belongs here: sending notifications on completion, enforcing ownership rules, triggering side effects. Adding the layer now avoids a painful refactor later.

---

## Global error-handling middleware vs per-controller try/catch

Initially we added `try/catch` blocks with `Console.Error` to every controller method. This was repetitive — every catch block did the same thing: log the error and return a 500. Instead, we moved error handling into a global `IExceptionHandler` implementation.

ASP.NET Core's `IExceptionHandler` catches any unhandled exception from the request pipeline. It logs the error with its stack trace and returns a generic `{ error: "Internal server error" }` response.

This keeps the controllers focused on business logic and gives us one place to change logging behaviour later (e.g. swapping `Console.Error` for Serilog).

---

## DELETE endpoint: 204 No Content vs 200 with JSON

Initially the delete endpoint returned `204 No Content` (no response body), which is the standard REST convention for a successful delete. However, the frontend's generic `request()` helper calls `res.json()` on every response — and `res.json()` throws on a 204 because there's no body to parse.

We could have added a special case in the frontend (`if (status === 204) return`) but that means every API endpoint no longer follows the same contract. Instead, we changed the delete endpoint to return `200` with `{ data: { id } }`. Now every endpoint returns JSON, the frontend helper stays simple, and the client gets the deleted ID back — which it can use to filter the item out of state without a refetch.

---

## Partial updates and nullable fields

PATCH requests need to distinguish between "field was not sent" (don't change it) and "field was sent as null" (clear it). C# record deserialization loses this distinction — both cases result in `null`.

To solve this, the controller accepts a raw `JsonElement` and builds a set of explicitly sent field names by iterating over the JSON properties. The service uses this set to decide whether to update nullable fields like `CompletedAt` and `DueDate`.

Alternatives considered:
- **JSON Patch** (`Microsoft.AspNetCore.JsonPatch`) — formal REST standard but adds client-side complexity and an unintuitive request format
- **Separate endpoints** per action (e.g. `PATCH /todos/:id/complete`, `DELETE /todos/:id/complete`) — clean for one field but doesn't scale when multiple nullable fields need clearing

The `JsonElement` approach keeps the client simple (plain JSON body) while correctly handling all nullable fields.

---

## Vite

Vite is a build tool and dev server for frontend projects. It replaces the older Create React App (CRA), which is now essentially deprecated (the React docs recommend Vite for SPAs).

Key advantages over CRA:
- **Fast dev server** — uses native ES modules, so it doesn't bundle everything upfront on each change. Hot Module Replacement (HMR) updates only the changed module instantly.
- **Faster builds** — uses Rollup under the hood for production bundling
- Env vars prefixed with `VITE_` are exposed to browser code via `import.meta.env`

---

## React Router

React Router is a library for client-side routing in React. "Routing" means: when the URL is `/login`, show the login page; when it's `/todos`, show the todos page — without a full browser reload. This is what makes it a Single Page App (SPA).

Options considered:
- **React Router v6** — industry standard, huge ecosystem, well documented
- **TanStack Router** — fully type-safe routes, newer API, smaller community
- **No router (useState)** — zero deps but the URL never changes, so back button and bookmarks don't work

We went with React Router v6 as the standard recognisable choice.

---

## Token storage: localStorage vs httpOnly cookies

The frontend stores the JWT in localStorage and attaches it as an `Authorization: Bearer <token>` header.

**localStorage risks:**
- Theoretically vulnerable to XSS — if malicious JavaScript runs on the page, it can read localStorage
- React mitigates this by escaping output by default. You'd need `dangerouslySetInnerHTML` or untrusted third-party scripts (analytics, ad trackers) to create an opening. For apps without third-party scripts, the risk is low.

**httpOnly cookie alternative:**
- The browser never exposes httpOnly cookies to JavaScript — immune to XSS entirely
- Paired with a `SameSite` cookie flag to prevent CSRF (where an attacker's site tricks the browser into sending cookies on the user's behalf to your API)
- More complex to implement: `res.cookie()` on the backend, `credentials: "include"` on every fetch, CORS can no longer use `origin: "*"`, logout requires a server-side endpoint to clear the cookie

**Conclusion:** localStorage + Authorization header is widely used in production SPAs. httpOnly cookies are the theoretically more secure choice and more common in server-rendered apps or high-risk domains (fintech, banking).

---

## CSRF (Cross-Site Request Forgery)

An attacker tricks your browser into making a request to your site from a different site. Example: you're logged into your bank, you visit a malicious page that submits a hidden form to `bank.com/transfer`. Because the browser automatically sends cookies with every request to `bank.com`, the bank thinks it's you.

The fix is a `SameSite` cookie flag — tells the browser to only send the cookie when the request originates from your own domain.

localStorage is immune to CSRF because an attacker's site can't read your localStorage (scoped to origin), and the JavaScript that attaches the token as a header can't run cross-origin.

---

## Frontend usability decisions

Several small UX choices were made to keep the interface intuitive and reduce accidental actions:

- **"+ Add due date" button** — since due date is optional, showing an empty date input would suggest it's required. Instead, a dashed "+ Add due date" button visually communicates optionality. Clicking it reveals the date picker; an "x" clears it back to the button state.

- **One card editable at a time** — editing state is lifted to the parent (`editingId`). Clicking a different card automatically closes the previous one. This avoids a cluttered UI with multiple open edit forms.

- **Click outside to cancel** — a `mousedown` listener on the document checks if the click target is outside the card ref. If so, editing is cancelled. Escape key also works. This follows the convention users expect from dropdown menus and modals.

- **Delete lives inside edit mode only** — originally each card had an "x" button for deletion, but it was too easy to accidentally click. Moving delete into the edit view ensures the user has intentional focus on the card before they can destroy it. The delete button is styled quietly (muted text, red only on hover) to further reduce accidental clicks.

- **Time-based greeting** — the header shows "Good morning/afternoon/evening" based on the browser's local time, using the user's name when available and falling back to email.

- **"In Progress" and "Completed" sections** — todos are split into two labeled groups rather than a flat list. Any todo without a `completedAt` timestamp is shown under "In Progress"; completed todos are grouped below with a dimmed style.

- **Multiline descriptions visible on cards** — descriptions preserve newlines (`white-space: pre-line`) so the card view matches what the user typed. However, this could make the list feel busy when many todos have long descriptions. Worth revisiting later — a possible improvement would be showing only the title and a truncated single-line preview of the description, expanding to full content on click or hover.
