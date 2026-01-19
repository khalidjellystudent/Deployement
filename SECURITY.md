
# Security Measures  Document (TicketSystem)

Last updated: 2026-01-16

This document summarizes the security controls that are implemented in this project and the key risks/next hardening steps.

## Threat Model (What we defend against)

Typical web application threats relevant to this project:

- **Unauthorized access** to protected dashboards and ticket data.
- **IDOR / Insecure Direct Object Reference** (e.g., guessing `ticketId` to view or modify another user’s ticket).
- **Credential attacks** (brute-force login attempts).
- **CSRF** (cross-site requests that change state when a user is logged in).
- **Open redirect** (tricking users into logging in then being redirected to a malicious site).
- **Basic browser attack surface** (missing security headers).

## Implemented Controls

### 1) Authentication (Cookie-based)

- Cookie authentication is configured with explicit paths for login and access denied.
- Users are signed in using claims that include:
  - `Email`
  - `NameIdentifier`
  - `Role`

Files:
- [Program.cs](Program.cs)
- [Controllers/HomeController.cs](Controllers/HomeController.cs)

### 2) Authorization (Role-Based Access Control)

- Controllers are protected with role-based authorization:
  - Officer area: `[Authorize(Roles = "Officer")]`
  - Office area: `[Authorize(Roles = "Office")]`
  - User area: `[Authorize(Roles = "User")]`

Files:
- [Controllers/OfficerController.cs](Controllers/OfficerController.cs)
- [Controllers/OfficeController.cs](Controllers/OfficeController.cs)
- [Controllers/UserController.cs](Controllers/UserController.cs)

### 3) Object-Level Authorization (Ticket Ownership)

- User ticket details enforce ownership checks (a user can only view their own tickets).
- QR scanning uses a single authenticated entrypoint that routes by role and validates ownership for `User`.

Files:
- [Controllers/UserController.cs](Controllers/UserController.cs)
- [Controllers/TicketController.cs](Controllers/TicketController.cs)

### 4) Password Security

- Passwords are **hashed with BCrypt** before storing.
- Login verifies password hashes with BCrypt.

Files:
- [Controllers/HomeController.cs](Controllers/HomeController.cs)
- [Controllers/OfficeController.cs](Controllers/OfficeController.cs)

### 5) Brute-Force Mitigation (Login Lockout)

- Failed login attempts are tracked.
- Accounts are temporarily locked after repeated failures.

File:
- [Controllers/HomeController.cs](Controllers/HomeController.cs)

### 6) CSRF Protection (Anti-Forgery)

- State-changing actions use `[ValidateAntiForgeryToken]`.
- Views that post forms use ASP.NET Core form tag helpers and/or explicit `@Html.AntiForgeryToken()`.

Files (examples):
- [Controllers/HomeController.cs](Controllers/HomeController.cs)
- [Controllers/OfficerController.cs](Controllers/OfficerController.cs)
- [Controllers/UserController.cs](Controllers/UserController.cs)
- [Controllers/OfficeController.cs](Controllers/OfficeController.cs)
- [Controllers/LprController.cs](Controllers/LprController.cs)
- [Controllers/AdminController.cs](Controllers/AdminController.cs)

### 7) Safe Redirect Handling (ReturnUrl)

- After login, ReturnUrl is only honored when it is a **local URL**.
- Prevents open-redirect attacks.

Files:
- [Controllers/HomeController.cs](Controllers/HomeController.cs)
- [Models/LoginViewModel.cs](Models/LoginViewModel.cs)
- [Views/Home/Login.cshtml](Views/Home/Login.cshtml)

### 8) Cookie Hardening (Defense-in-Depth)

Cookie settings include:
- `HttpOnly = true`
- `SecurePolicy = Always`
- `SameSite = Lax`
- `SlidingExpiration = true`

File:
- [Program.cs](Program.cs)

### 9) Basic Security Headers (Defense-in-Depth)

Added headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

File:
- [Program.cs](Program.cs)

## QR Code Security Notes

- QR codes encode a URL and **do not bypass authentication**.
- The QR target is an authenticated route that performs role routing and ownership checks:
  - `/Ticket/Open/{id}`

File:
- [Controllers/TicketController.cs](Controllers/TicketController.cs)

## Recommendations / Next Hardening Steps

These are good improvements for a graduation-level “production-minded” project:

1. **Audit logging** for ticket state changes (Paid / UnderProcess / Delete): log who changed what, when, and from where.
2. **Rate limiting** (beyond login lockout) for sensitive endpoints.
3. **CSP (Content Security Policy)** to reduce XSS impact. This project currently uses inline scripts/styles in views, so CSP should be introduced carefully (start with Report-Only).
4. **Consistent ownership checks** on all ticket state-changing actions (already improved in User POST actions; review Office/Officer flows as needed).
5. **Centralized error handling** and structured logging, including security-relevant events.

## Graduation Evaluation (Summary)

- Strong foundation: RBAC, BCrypt, lockout, safe ReturnUrl redirect, and object-level authorization for user tickets.
- With audit logs + CSP/report-only + broader rate limiting, this becomes a strong “real-world ready” security story for an IT graduation project.
