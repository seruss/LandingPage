# 🚀 Paweł Seweryn — Landing Page

Personal portfolio & landing page built with **Blazor Web App (Static SSR)** — the most minimal Blazor hosting model with zero WebSocket or WebAssembly overhead.

## ✨ Features

- **Dark & light themes** with custom design system (Space Grotesk font, CSS variables, no frameworks)
- **Scroll-reveal animations** via IntersectionObserver + CSS transitions
- **Expandable project details** — clean at first glance, rich on demand (`<details>/<summary>`)
- **Floating terminal card** with animated activity bars
- **Career timeline** with visual markers and expandable achievements
- **Counter animations** for stats (3TB+, 300+, 15x, 99%)
- **Responsive** — mobile-first grid layouts
- **SEO-ready** — proper meta tags, semantic HTML, heading hierarchy

## 📋 Sections

| Section | Description |
|---|---|
| **Hero** | Headline, stats terminal, GitHub/LinkedIn links |
| **What I Do** | 3 expertise cards (Distributed Systems, Performance, SLA) |
| **Flagship Project** | Distributed Logging Platform with architecture diagram |
| **Selected Works** | Media Delivery, Microservices Migration, NationsCities |
| **Experience** | Career timeline with expandable details |
| **Why I'm Different** | JSON-styled USP block + Tech Stack badges |
| **Education** | M.Sc., B.Sc., Music education cards |
| **Side Projects & About** | GitHub links, bio, languages |
| **Footer** | Contact info, social links |

## 🛠 Tech Stack

- **Framework:** Blazor Web App (.NET 9) — Static SSR
- **Styling:** Vanilla CSS (no Tailwind, no Bootstrap)
- **Fonts:** Space Grotesk + Material Icons (Google Fonts)
- **Animations:** Vanilla JS (IntersectionObserver, requestAnimationFrame)
- **Hosting model:** Static Server-Side Rendering — pure HTML, no JS runtime

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Run locally

```bash
git clone https://github.com/seruss/LandingPage.git
cd LandingPage/LandingPage
dotnet run
```

Open [http://localhost:5028](http://localhost:5028) in your browser.

### Build for production

```bash
dotnet publish -c Release
```

## 📁 Project Structure

```
LandingPage/
├── .github/
│   └── workflows/                       # CI/CD pipeline
├── Analytics/
│   ├── AnalyticsEndpoints.cs            # API endpoints for stats dashboard
│   ├── AnalyticsMiddleware.cs           # Request tracking middleware
│   ├── EventBuffer.cs                   # Batched event buffer for SurrealDB
│   ├── TrackingEvent.cs                 # Event model
│   └── VisitRecord.cs                   # Visit record model
├── Components/
│   ├── App.razor                        # HTML root, fonts, meta
│   ├── Routes.razor                     # Blazor routing
│   ├── _Imports.razor                   # Global using directives
│   ├── Layout/
│   │   └── MainLayout.razor             # Fixed navigation bar + theme toggle
│   ├── Pages/
│   │   └── Home.razor                   # Main page composing all sections
│   └── Sections/                        # Individual section components
│       ├── HeroSection.razor
│       ├── WhatIDoSection.razor
│       ├── FlagshipProjectSection.razor
│       ├── SelectedWorksSection.razor
│       ├── ExperienceSection.razor
│       ├── WhyDifferentSection.razor
│       ├── TechStackSection.razor
│       ├── EducationSection.razor
│       ├── SideProjectsAboutSection.razor
│       └── FooterSection.razor
├── Properties/
│   └── launchSettings.json              # Dev server config
├── wwwroot/
│   ├── app.css                          # Complete design system + light/dark themes
│   ├── favicon.png
│   └── js/
│       ├── animations.js                # Scroll reveals, counters, parallax
│       ├── theme.js                     # Dark/light theme toggle persistence
│       └── tracker.js                   # Analytics tracking client
├── Program.cs                           # Blazor startup + middleware
├── appsettings.json                     # Base configuration
├── appsettings.Development.json         # Development overrides
└── appsettings.Production.json          # Production overrides (SurrealDB, etc.)
```

## 📄 License

MIT

## 👤 Author

**Paweł Seweryn** — Senior Backend Developer

- Website: [pawel-seweryn.pl](https://www.pawel-seweryn.pl)
- LinkedIn: [Paweł Seweryn](https://www.linkedin.com/in/pawe%C5%82-seweryn-4677b7106/)
- Email: serus1604@gmail.com
