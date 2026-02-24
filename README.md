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

## 📄 License

MIT

## 👤 Author

**Paweł Seweryn** — Senior Backend Developer

- Website: [pawel-seweryn.pl](https://www.pawel-seweryn.pl)
- LinkedIn: [Paweł Seweryn](https://www.linkedin.com/in/pawe%C5%82-seweryn-4677b7106/)