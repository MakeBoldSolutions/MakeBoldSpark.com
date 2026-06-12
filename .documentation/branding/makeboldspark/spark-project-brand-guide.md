# Spark Project Brand Guide

Version: 1.0
Updated: 2026-06-12

## Purpose

This guide defines the visual and verbal identity for every project that belongs to the Make Bold Spark family. A Spark Project can be a NuGet package, an npm package, a hosted website, a web application, a mobile app, a methodology, or any other deliverable published under the Make Bold Spark umbrella.

Every Spark Project must feel like a recognizable member of the family while maintaining its own clear identity. Visitors who know makeboldspark.com should immediately recognize a Spark Project site as part of the same system.

This guide supplements, never replaces, the core [`brand-guide.md`](./brand-guide.md). All decisions in the core guide apply unless this document provides a more specific rule.

---

## What Makes a Spark Project

A Spark Project is any project that:

- Is published, hosted, or distributed under the `makeboldspark.com` domain family.
- Lives at a subdomain `*.makeboldspark.com` when hosted on the web.
- Uses `MakeBoldSpark` or an approved Spark Project name as its package namespace or repository namespace.
- Follows the color, type, and component rules in this guide.

Spark Projects are not forks of each other. They are independent tools, packages, or products that share a parent identity.

---

## Naming a Spark Project

### Project Name Structure

Every Spark Project name follows a two-part pattern:

```
[ProjectName]Spark
```

Examples:

- `ApiSpark` — API exploration and backend platform.
- `WebSpark` — Web component and template library.
- `DocSpark` — Documentation tooling.
- `DataSpark` — Data pipeline utilities.

Acceptable variations when the word Spark would be redundant in context:

- Use `[ProjectName].Core`, `[ProjectName].Api`, etc. as sub-package suffixes within a project family.

### Subdomain Pattern

Every hosted Spark Project uses a subdomain of `makeboldspark.com`:

```
[projectname].makeboldspark.com
```

Examples:

- `api.makeboldspark.com`
- `web.makeboldspark.com`
- `docs.makeboldspark.com`

Use lowercase, no hyphens preferred, no underscores.

### Package Namespace

#### NuGet packages

```
MakeBoldSpark.[ProjectName]
MakeBoldSpark.[ProjectName].[Module]
```

Examples:

- `MakeBoldSpark.WebSpark.Core`
- `MakeBoldSpark.ApiSpark`

#### npm packages

Use scoped packages under the `@makeboldspark` scope:

```
@makeboldspark/[project-name]
@makeboldspark/[project-name]-[module]
```

Examples:

- `@makeboldspark/webspark`
- `@makeboldspark/apispark-client`

---

## Project Identity Elements

Each Spark Project has:

1. A project name.
2. A project mark derived from the Make Bold Spark spark shape.
3. An accent color drawn from the approved palette.
4. A short positioning line (one sentence).

The project mark is always the Make Bold Spark spark shape plus the project name. It is not a separate custom icon.

### Project Accent Colors

Each Spark Project selects one accent from the supporting palette. The core spark red (`--mbs-spark`) is reserved for Make Bold Spark itself.

| Accent Token | Hex | Use |
|---|---|---|
| `--mbs-spark` | `#E94B1B` | Reserved for makeboldspark.com only |
| `--mbs-ember` | `#982407` | Deep red accent projects |
| `--mbs-mint` | `#3FBFA8` | Tool and developer-facing projects |
| `--mbs-gold` | `#F2B84B` | Data, analytics, and content projects |
| `--mbs-steel` | `#2F3A3D` | Infrastructure and systems projects |

A project that has not been assigned a unique accent uses `--mbs-ember` as its default.

Add a project-specific CSS variable that maps to the chosen accent:

```css
--project-accent: var(--mbs-mint); /* Example for a tool project */
```

### Project Short Line

Every Spark Project must have a one-sentence description that can appear in:

- The subdomain site header or hero.
- Package descriptions on NuGet.org and npmjs.com.
- Repository About fields.
- Open Graph descriptions.

Format: one sentence, present tense, builder-focused, no buzzwords.

Examples:

- "ApiSpark consolidates portfolio APIs into a single observable backend."
- "WebSpark provides reusable C# and Razor components for ASP.NET Core projects."

---

## Color Usage in Spark Projects

Spark Projects inherit the full Make Bold Spark color palette. The tokens are defined in `brand-tokens.json` and `css/makeboldspark-brand.css`.

### Base Token Reference

```
--mbs-ink:    #040605   Primary text, borders, dark surfaces
--mbs-spark:  #E94B1B   Reserved for parent brand only
--mbs-ember:  #982407   Deep accent, active/hover states
--mbs-paper:  #F7F4EF   Warm light background
--mbs-cream:  #EEE8DF   Secondary background, alternate surfaces
--mbs-smoke:  #787878   Secondary text, placeholders
--mbs-steel:  #2F3A3D   Technical surfaces, dark cards
--mbs-mint:   #3FBFA8   Success, positive status
--mbs-gold:   #F2B84B   Warning, attention, highlights
--mbs-white:  #FFFFFF   Reversed text, clean surfaces
```

### Project Accent Override Pattern

Define the project accent once and reference it throughout all components:

```css
:root {
  --project-accent: var(--mbs-mint);
  --project-accent-dark: #2e9e8a; /* darken by ~10% for hover */
}
```

Use `--project-accent` anywhere the parent brand would use `--mbs-spark`.

---

## CSS Framework Implementations

This guide provides three parallel implementation paths. Choose the one that matches the project's tech stack. The visual output must be consistent regardless of which path is used.

---

### Path A — Plain CSS (no framework)

Use the base `css/makeboldspark-brand.css` as the foundation. Include it before any project-specific styles.

**CSS variable setup:**

```css
@import url("path/to/makeboldspark-brand.css");

:root {
  --project-accent: var(--mbs-mint);
  --project-accent-dark: #2e9e8a;
}
```

**Primary button:**

```css
.btn-primary {
  align-items: center;
  background: var(--project-accent);
  border: 1px solid transparent;
  border-radius: var(--mbs-radius);
  color: var(--mbs-white);
  cursor: pointer;
  display: inline-flex;
  font: 800 0.95rem/1 var(--mbs-font);
  gap: 0.5rem;
  min-height: 2.75rem;
  padding: 0.85rem 1rem;
  text-decoration: none;
  transition: background-color 160ms ease, transform 160ms ease;
}

.btn-primary:hover {
  background: var(--project-accent-dark);
  transform: translateY(-1px);
}
```

**Card:**

```css
.card {
  background: var(--mbs-white);
  border: 1px solid var(--mbs-line);
  border-radius: var(--mbs-radius);
  padding: clamp(1rem, 2vw, 1.5rem);
}
```

**Badge:**

```css
.badge {
  background: rgba(63, 191, 168, 0.1); /* adjust alpha to project accent */
  border: 1px solid rgba(63, 191, 168, 0.22);
  border-radius: 999px;
  color: var(--project-accent-dark);
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  padding: 0.42rem 0.58rem;
  text-transform: uppercase;
}
```

**Navigation bar:**

```css
.site-nav {
  align-items: center;
  background: var(--mbs-white);
  border-bottom: 1px solid var(--mbs-line);
  display: flex;
  gap: 1.5rem;
  padding: 0.75rem 1.5rem;
}

.site-nav a {
  color: var(--mbs-smoke);
  font-size: 0.9rem;
  font-weight: 700;
  text-decoration: none;
}

.site-nav a[aria-current="page"],
.site-nav a:hover {
  color: var(--project-accent);
}

.site-nav a[aria-current="page"] {
  border-bottom: 2px solid var(--project-accent);
  padding-bottom: 2px;
}
```

---

### Path B — Bootstrap 5

Bootstrap projects do not load the plain MBS button or card classes. Instead, override Bootstrap's CSS custom properties and add thin project-specific utility classes.

**Bootstrap variable overrides (place before bootstrap import or in a custom `_variables.scss` file):**

```css
/* Place in a <style> block or project.css loaded AFTER bootstrap.min.css */
:root {
  /* Map Bootstrap semantic tokens to MBS values */
  --bs-body-bg: #F7F4EF;               /* --mbs-paper */
  --bs-body-color: #040605;            /* --mbs-ink */
  --bs-body-font-family: "Inter Tight", Inter, ui-sans-serif, system-ui, sans-serif;
  --bs-border-radius: 8px;             /* --mbs-radius */
  --bs-border-color: rgba(4, 6, 5, 0.14);

  /* Primary = project accent, not Bootstrap blue */
  --bs-primary: #3FBFA8;               /* replace with project accent */
  --bs-primary-rgb: 63, 191, 168;
  --bs-link-color: #3FBFA8;
  --bs-link-hover-color: #2e9e8a;

  /* Project accent local reference */
  --project-accent: #3FBFA8;
  --project-accent-dark: #2e9e8a;
}
```

**SCSS variable overrides (if using SCSS compilation):**

```scss
// Override before @import "bootstrap"
$font-family-sans-serif: "Inter Tight", Inter, ui-sans-serif, system-ui, sans-serif;
$body-bg: #F7F4EF;
$body-color: #040605;
$primary: #3FBFA8;      // replace with project accent
$border-radius: 8px;
$border-radius-sm: 6px;
$border-radius-lg: 10px;
$box-shadow: none;
$box-shadow-sm: none;
$card-border-color: rgba(4, 6, 5, 0.14);
$card-cap-bg: transparent;
$card-bg: #ffffff;
```

**Bootstrap usage patterns:**

Buttons: use `btn btn-primary` for the project accent primary action. Do not use `btn-secondary` with Bootstrap's default grey; override it:

```css
.btn-secondary {
  --bs-btn-color: var(--mbs-ink);
  --bs-btn-bg: transparent;
  --bs-btn-border-color: rgba(4, 6, 5, 0.14);
  --bs-btn-hover-color: var(--mbs-ink);
  --bs-btn-hover-bg: var(--mbs-cream);
  --bs-btn-hover-border-color: rgba(4, 6, 5, 0.22);
  --bs-btn-font-weight: 700;
}
```

Cards: use Bootstrap `.card` with the overridden variables above. Add MBS-specific heading weight:

```css
.card .card-title {
  font-size: 1.25rem;
  font-weight: 800;
  line-height: 1.18;
}
```

Badges: use `badge rounded-pill` and override color with project accent:

```css
.badge-project {
  background: rgba(63, 191, 168, 0.1);
  border: 1px solid rgba(63, 191, 168, 0.22);
  color: var(--project-accent-dark);
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
```

Navigation: use Bootstrap's `navbar` with these overrides:

```css
.navbar {
  --bs-navbar-color: var(--mbs-smoke);
  --bs-navbar-hover-color: var(--project-accent);
  --bs-navbar-active-color: var(--project-accent);
  --bs-navbar-brand-color: var(--mbs-ink);
  border-bottom: 1px solid var(--mbs-line);
  font-weight: 700;
}
```

**Do not** use Bootstrap's `text-primary`, `bg-primary`, or gradient utilities for decorative elements. Map them to the project accent explicitly.

---

### Path C — Tailwind CSS v3 / v4

Tailwind projects configure the Make Bold Spark palette and typography as design tokens in the Tailwind config, then use utility classes directly in markup.

**Tailwind v3 config (`tailwind.config.js` or `tailwind.config.ts`):**

```js
/** @type {import('tailwindcss').Config} */
module.exports = {
  theme: {
    extend: {
      colors: {
        mbs: {
          ink:    '#040605',
          spark:  '#E94B1B',
          ember:  '#982407',
          paper:  '#F7F4EF',
          cream:  '#EEE8DF',
          smoke:  '#787878',
          steel:  '#2F3A3D',
          mint:   '#3FBFA8',
          gold:   '#F2B84B',
        },
        // Project accent — change per project
        accent: {
          DEFAULT: '#3FBFA8',   // replace with project accent
          dark:    '#2e9e8a',
        },
      },
      fontFamily: {
        sans: ['"Inter Tight"', 'Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'sans-serif'],
      },
      borderRadius: {
        DEFAULT: '8px',
        control: '8px',
        icon: '28px',
        full: '9999px',
      },
      boxShadow: {
        raised: '0 18px 48px rgba(4,6,5,0.14)',
        none: 'none',
      },
      fontSize: {
        display: ['clamp(3rem,7vw,6.25rem)', { lineHeight: '0.95' }],
        h1:      ['clamp(2.5rem,5vw,4.75rem)', { lineHeight: '1' }],
        h2:      ['clamp(1.8rem,3vw,2.8rem)',  { lineHeight: '1.08' }],
      },
    },
  },
};
```

**Tailwind v4 config (`@theme` block in CSS):**

```css
@import "tailwindcss";

@theme {
  --color-mbs-ink:    #040605;
  --color-mbs-spark:  #E94B1B;
  --color-mbs-ember:  #982407;
  --color-mbs-paper:  #F7F4EF;
  --color-mbs-cream:  #EEE8DF;
  --color-mbs-smoke:  #787878;
  --color-mbs-steel:  #2F3A3D;
  --color-mbs-mint:   #3FBFA8;
  --color-mbs-gold:   #F2B84B;

  /* Project accent — change per project */
  --color-accent:      #3FBFA8;
  --color-accent-dark: #2e9e8a;

  --font-sans: "Inter Tight", Inter, ui-sans-serif, system-ui, sans-serif;
  --radius-control: 8px;
  --radius-icon: 28px;
  --shadow-raised: 0 18px 48px rgba(4,6,5,0.14);
}
```

**Tailwind component patterns:**

Primary button:

```html
<a href="#"
   class="inline-flex items-center gap-2 rounded-control bg-accent px-4 py-3
          text-sm font-black text-white no-underline
          transition-[background-color,transform] duration-150 ease-in-out
          hover:-translate-y-px hover:bg-accent-dark">
  Get started
</a>
```

Card:

```html
<div class="rounded-control border border-mbs-ink/[0.14] bg-white
            p-[clamp(1rem,2vw,1.5rem)]">
  <h3 class="mb-2 text-xl font-black leading-snug">Title</h3>
  <p class="text-mbs-steel">Description text.</p>
</div>
```

Badge:

```html
<span class="inline-flex items-center rounded-full border
             border-accent/20 bg-accent/10 px-2 py-1
             text-[0.75rem] font-black uppercase tracking-widest text-accent-dark">
  Label
</span>
```

Navigation:

```html
<nav class="flex items-center gap-6 border-b border-mbs-ink/[0.14]
            bg-white px-6 py-3">
  <a href="/" class="flex items-center gap-3 no-underline">
    <img src="/assets/logo/makeboldspark-mark.svg" class="h-8 w-auto" alt="Project Name">
  </a>
  <a href="/docs"
     class="text-sm font-bold text-mbs-smoke no-underline
            hover:text-accent aria-[current=page]:border-b-2
            aria-[current=page]:border-accent aria-[current=page]:text-accent">
    Docs
  </a>
</nav>
```

**Tailwind note:** Avoid `text-primary`, `bg-blue-*`, or any Tailwind default palette colors in project markup. Use only `mbs-*` and `accent` color utilities.

---

## Typography Rules (All Frameworks)

These rules apply regardless of the CSS framework in use.

- Typeface: Inter Tight. Load from `fonts/inter-tight.ttf` or from Google Fonts.
- Fallback: `Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif`.
- Do not use negative letter spacing.
- Display and heading levels use weight 800–900.
- Body copy uses weight 400–500.
- Uppercase labels use weight 900 and `letter-spacing: 0.08em`.
- Line height: 0.95 for display, 1.0–1.18 for headings, 1.65 for body.

### Framework-Specific Typography Setup

**Plain CSS:**

```css
h1, h2, h3, h4, h5, h6 {
  font-family: var(--mbs-font);
  font-weight: 800;
  letter-spacing: 0;
  line-height: 1.1;
}
```

**Bootstrap 5 SCSS:**

```scss
$headings-font-family: "Inter Tight", Inter, ui-sans-serif, system-ui, sans-serif;
$headings-font-weight: 800;
$headings-line-height: 1.1;
$lead-font-weight: 500;
```

**Tailwind:**

Apply heading weights with utility classes `font-black` (900) or `font-extrabold` (800) directly on heading elements in templates. Configure default heading styles with a Tailwind plugin or `@layer base` block:

```css
@layer base {
  h1, h2, h3, h4, h5, h6 {
    @apply font-black leading-tight tracking-normal;
  }
}
```

---

## Logo and Mark Usage in Spark Projects

Every Spark Project site displays the Make Bold Spark mark. It links back to `makeboldspark.com`.

### Navigation Header

- Place `makeboldspark-mark.svg` at the left of the navigation bar.
- Minimum height: 32 px on desktop, 28 px on mobile.
- Follow it with the project name in weight 900.
- Link the entire lockup to the project's own homepage, not back to makeboldspark.com.

### Footer Attribution

Every Spark Project site footer must include an attribution line:

```html
<p>Part of the <a href="https://makeboldspark.com">Make Bold Spark</a> family.</p>
```

Style with `--mbs-smoke` text. Link uses `--project-accent` color.

### Back-Link Policy

- The navigation header mark + project name links to the project's own root URL.
- The footer attribution line links to `https://makeboldspark.com`.
- Do not place two separate links to makeboldspark.com in the header.

### Project Mark Files

Project-specific assets follow the same naming pattern as the parent brand:

```
[projectname]-logo-horizontal.svg
[projectname]-logo-stacked.svg
[projectname]-mark.svg
[projectname]-favicon.ico
```

If a project does not have its own custom mark, use `makeboldspark-mark.svg` directly.

---

## Page Structure

Every Spark Project web page follows this structural hierarchy:

```
<header>     Site nav: mark + project name, main links, optional CTA
<main>
  <section>  Hero or page title area
  <section>  Primary content
  <section>  Supporting content or calls to action
</main>
<footer>     Links, attribution to makeboldspark.com, copyright
```

### Hero Section Rules

- The first viewport must identify the project by name and one-sentence description.
- Use a real product visual, code sample, or architecture diagram — never a generic stock hero.
- The hero background is `--mbs-paper` or `--mbs-ink` (dark). Not a gradient.
- Keep the next section partially visible on standard desktop and mobile viewports.

### Documentation Pages

- Short sections with clear headings.
- Use code blocks for all technical examples.
- Use callouts (bordered `<aside>` elements) for decisions, warnings, and reusable patterns.
- Avoid deep nesting: no more than two levels of section nesting.

---

## Iconography

All Spark Projects use the same icon style:

- Simple line icons, 1.75–2 px stroke weight.
- Square viewboxes.
- Rounded joins when the icon set uses them.
- Icons clarify actions and content; they do not replace labels in navigation.

Recommended icon libraries (pick one per project and do not mix):

- Lucide (MIT, consistent 2 px stroke, well-maintained).
- Phosphor Icons (flexible weight).
- Heroicons (Tailwind-native, good for Tailwind projects).

Do not use icon libraries that default to filled / solid style for most icons unless overriding to outline/line consistently.

---

## Imagery

Preferred imagery for all Spark Projects:

- Real product screenshots or screencasts.
- Code, API, architecture, and workflow diagrams.
- Abstract geometry derived from the spark mark or wedge shape.
- Terminal or IDE screenshots where relevant to the project.

Avoid:

- Generic handshake or teamwork stock photography.
- Dark atmospheric blur photographs.
- AI-generated images of people or faces.
- Images that obscure what the project actually does.

Social and Open Graph images must include:

- The project name in the image.
- The Make Bold Spark mark or the project mark.
- A short description or a visual of the product.

---

## Package Presentation

### NuGet Package Requirements

Every `MakeBoldSpark.*` NuGet package must include:

- `<PackageIcon>` pointing to the project mark PNG (512×512).
- `<Description>` using the project's approved one-sentence short line.
- `<PackageTags>` starting with `makeboldspark spark`.
- `<PackageProjectUrl>` pointing to the subdomain: `https://[project].makeboldspark.com`.
- `<RepositoryUrl>` pointing to the GitHub repository.
- `<Authors>` set to `Make Bold Spark`.
- Semantic versioning with a consistent `CHANGELOG.md` in the repository root.

Example `.csproj` excerpt:

```xml
<PropertyGroup>
  <PackageId>MakeBoldSpark.WebSpark.Core</PackageId>
  <Description>Core components and service abstractions for WebSpark ASP.NET Core projects.</Description>
  <PackageTags>makeboldspark spark webspark aspnetcore</PackageTags>
  <PackageProjectUrl>https://webspark.makeboldspark.com</PackageProjectUrl>
  <Authors>Make Bold Spark</Authors>
  <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>
```

### npm Package Requirements

Every `@makeboldspark/*` npm package must include:

- `"description"` using the project's approved one-sentence short line.
- `"homepage"` pointing to the subdomain.
- `"repository"` pointing to the GitHub repository.
- `"keywords"` array starting with `"makeboldspark"` and `"spark"`.
- `"author"` set to `"Make Bold Spark"`.
- A `README.md` that includes the Make Bold Spark footer attribution.

Example `package.json` excerpt:

```json
{
  "name": "@makeboldspark/webspark",
  "description": "Reusable components for WebSpark web projects.",
  "homepage": "https://webspark.makeboldspark.com",
  "keywords": ["makeboldspark", "spark", "webspark"],
  "author": "Make Bold Spark"
}
```

---

## Motion

Motion rules match the parent brand and apply in all framework contexts:

- Transition duration: 120–220 ms.
- Easing: `ease` or `ease-in-out`.
- Hover lift: `translateY(-1px)` or `translateY(-2px)`.
- No infinite decorative animations on documentation pages.
- No large bounce or spring effects that delay interaction.

**Tailwind utility mapping:**

```
duration-[160ms] ease-in-out hover:-translate-y-px
```

**Bootstrap utility mapping:**

```html
class="transition" style="--bs-transition-duration: 160ms;"
```

---

## Dark Mode

Spark Projects may offer a dark mode. When dark mode is implemented:

- Background: `--mbs-ink` (`#040605`).
- Text: `--mbs-white` (`#FFFFFF`) at full opacity for headings; `rgba(255,255,255,0.78)` for body.
- Borders: `--mbs-line-dark` (`rgba(247,244,239,0.18)`).
- Accent remains the project accent color unchanged.
- Paper and cream are not used in dark mode.

**Plain CSS:**

```css
@media (prefers-color-scheme: dark) {
  :root {
    --mbs-paper: #040605;
    --mbs-white: #040605;
    --mbs-ink: #F7F4EF;
    --mbs-line: rgba(247, 244, 239, 0.18);
  }
}
```

**Tailwind:**

Use the `dark:` variant with `mbs-ink` and `mbs-white` utilities.

**Bootstrap:**

Set `data-bs-theme="dark"` on `<html>` and override `--bs-body-bg` and `--bs-body-color` in a dark-theme stylesheet.

Dark mode is optional for package documentation sites. It is not required but must follow these rules if implemented.

---

## Accessibility

Contrast requirements for Spark Project UIs:

- Body text: minimum 4.5:1 against its background.
- Large text (≥24 px regular or ≥18.67 px bold): minimum 3:1.
- UI focus rings: `2px solid var(--project-accent)` with `2px offset`.
- Do not rely on color alone to convey state or meaning.

Approved high-contrast text combinations:

- Ink on paper — passes 4.5:1.
- Ink on white — passes 4.5:1.
- White on ink — passes 4.5:1.
- White on ember — passes 4.5:1 for bold text ≥16 px.
- Ink on gold — passes for warning surfaces.
- White on mint (project accent) — verify at `3FBFA8`; passes for large or bold text only. Use ink text on mint for small body text.

When using a custom project accent that is not in this list, verify contrast before shipping.

---

## Subdomain Implementation Checklist

Complete this checklist for every Spark Project web deployment:

### Identity

- [ ] Project name follows `[ProjectName]Spark` or an approved variation.
- [ ] Site is hosted at `[projectname].makeboldspark.com`.
- [ ] Page `<title>` format: `Page Name — ProjectName | Make Bold Spark`.
- [ ] `<meta name="description">` uses the project's approved one-sentence short line.

### Branding Assets

- [ ] `makeboldspark-mark.svg` in the site navigation, minimum 32 px height.
- [ ] Project mark or Make Bold Spark mark used as favicon.
- [ ] Open Graph image includes project name and a product visual.
- [ ] Footer attribution links to `https://makeboldspark.com`.

### Styles

- [ ] Make Bold Spark color tokens loaded (`makeboldspark-brand.css` or equivalent theme config).
- [ ] Project accent variable defined and used consistently.
- [ ] Inter Tight font loaded and applied to all headings.
- [ ] No default Bootstrap blue, Tailwind blue, or other framework colors visible in the UI.
- [ ] `border-radius: 8px` applied to all interactive controls and cards.

### Framework-Specific

- [ ] **Plain CSS:** `makeboldspark-brand.css` imported before project styles.
- [ ] **Bootstrap:** Bootstrap color and variable overrides applied. Default `btn-secondary` overridden.
- [ ] **Tailwind:** MBS palette registered in theme config. Default color palette not used in markup.

### Accessibility

- [ ] All body text meets 4.5:1 contrast.
- [ ] All focus states visible with `2px solid` accent ring.
- [ ] No color-only state indicators.

### Packages (if applicable)

- [ ] NuGet: package icon, description, tags, project URL, and authors fields set.
- [ ] npm: description, homepage, keywords, and author fields set.
- [ ] Both: repository URL present and accurate.

---

## File Naming for Spark Projects

Follow the same kebab-case convention as the parent brand:

```
[projectname]-logo-horizontal.svg
[projectname]-logo-stacked.svg
[projectname]-mark.svg
[projectname]-mark.png           (512×512 for NuGet package icon)
[projectname]-favicon.ico
[projectname]-brand.css          (project-specific extension of makeboldspark-brand.css)
[projectname]-tailwind.config.js (project Tailwind config with MBS tokens)
```

Project CSS files are extensions, not replacements. Always import `makeboldspark-brand.css` first, then apply project-specific overrides.
