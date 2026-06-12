# Make Bold Spark Brand Guide

Version: 1.0  
Updated: 2026-06-12

## Brand Foundation

Make Bold Spark is the project and product identity for practical AI, API, automation, and software experiments that are meant to become real working systems.

The brand should feel:

- Bold, but not loud.
- Technical, but not cold.
- Fast-moving, but still trustworthy.
- Builder-focused, not corporate-generic.
- Clear enough for production documentation and expressive enough for demos.

## Name Usage

Use `Make Bold Spark` as the public brand name.

Use `MakeBoldSpark` for:

- Repository names.
- Package names.
- Namespaces.
- Domains and URLs.
- Environment prefixes.
- Compact labels where spaces are not practical.

Avoid:

- `Makeboldspark`
- `Make BoldSpark`
- `MBSpark` as a public brand name
- Adding hyphens unless required by a technical system.

## Positioning

Make Bold Spark helps turn working ideas into durable web, API, and AI-enabled systems.

Short positioning line:

> Practical software sparks, built boldly.

Longer positioning line:

> Make Bold Spark is a builder-first identity for projects that combine clear architecture, useful automation, and practical AI into production-ready software.

## Voice

Make Bold Spark sounds confident, direct, and useful.

Write like this:

- "Ship the smallest reliable version first."
- "Make the system observable before it becomes mysterious."
- "Use AI where it improves the workflow, not where it decorates it."

Avoid:

- Hype-heavy claims.
- Vague transformation language.
- Overly cute technical metaphors.
- Long paragraphs when a checklist would be clearer.

## Logo System

The logo is built from three ideas:

- A forward wedge for momentum.
- A compact spark for ignition.
- A strong wordmark for practical credibility.

Primary logo:

- Use `assets/logo/makeboldspark-logo-horizontal.svg` in headers, website footers, documents, and product shells.

Stacked logo:

- Use `assets/logo/makeboldspark-logo-stacked.svg` when the format is square, centered, or presentation-like.

Mark only:

- Use `assets/logo/makeboldspark-mark.svg` for favicons, avatars, app icons, profile photos, and small UI.

One-color mark:

- Use `assets/logo/makeboldspark-mark-one-color.svg` when color output is limited.

## Clear Space

Keep clear space around the logo equal to at least one half of the spark mark width.

Do not place the logo directly against a container edge, image edge, dense pattern, or unrelated icon.

## Minimum Sizes

- Horizontal logo: 160 px wide minimum on screen.
- Stacked logo: 120 px wide minimum on screen.
- Mark only: 24 px minimum for UI; 16 px only for favicon use.
- Print: keep the horizontal logo at least 1.5 in wide.

## Logo Misuse

Do not:

- Stretch or compress the logo.
- Rotate the wordmark.
- Add drop shadows, glows, outlines, or bevels.
- Change the spark color outside the approved palette.
- Put the full logo over low-contrast photography.
- Recreate the wordmark in another font.
- Use the mark inside a rounded rectangle unless it is an app icon or favicon.

## Color Palette

### Primary

| Token | Hex | RGB | Use |
| --- | --- | --- | --- |
| `--mbs-ink` | `#040605` | `4, 6, 5` | Primary text, dark logo areas |
| `--mbs-spark` | `#E94B1B` | `233, 75, 27` | Primary action, spark, highlights |
| `--mbs-ember` | `#982407` | `152, 36, 7` | Deep accent, active states |
| `--mbs-paper` | `#F7F4EF` | `247, 244, 239` | Warm background |

### Supporting

| Token | Hex | RGB | Use |
| --- | --- | --- | --- |
| `--mbs-smoke` | `#787878` | `120, 120, 120` | Secondary text |
| `--mbs-steel` | `#2F3A3D` | `47, 58, 61` | Technical surfaces, charts |
| `--mbs-mint` | `#3FBFA8` | `63, 191, 168` | Success, freshness, positive status |
| `--mbs-gold` | `#F2B84B` | `242, 184, 75` | Warnings, attention, diagrams |
| `--mbs-white` | `#FFFFFF` | `255, 255, 255` | Reversed text, clean surfaces |

## Color Usage

Use warm paper and white for most backgrounds.

Use ink for text and structure.

Use spark red sparingly for:

- Primary buttons.
- Active navigation.
- Logo spark.
- Key chart highlights.
- Important labels.

Use mint and gold as functional colors, not decoration.

## Accessibility

Approved text combinations:

- Ink text on paper.
- Ink text on white.
- White text on ink.
- White text on ember.
- Ink text on gold for warning surfaces.

Avoid setting small white text on spark red unless the text is bold and at least 16 px.

## Typography

Primary typeface:

- Inter Tight

Fallback stack:

```css
font-family: "Inter Tight", Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
```

Use weights:

- 900 for strong display headings.
- 750-800 for section headings and navigation.
- 500-650 for body emphasis.
- 400-500 for normal body copy.

Do not use negative letter spacing. Keep letter spacing at `0` for headings and body copy. Use uppercase labels sparingly with positive tracking around `0.08em`.

## Type Scale

Recommended web scale:

- Display: `clamp(3rem, 7vw, 6.25rem)`, line-height `0.95`.
- H1: `clamp(2.5rem, 5vw, 4.75rem)`, line-height `1`.
- H2: `clamp(1.8rem, 3vw, 2.8rem)`, line-height `1.08`.
- H3: `1.25rem` to `1.6rem`, line-height `1.18`.
- Body: `1rem`, line-height `1.65`.
- Small: `0.875rem`, line-height `1.45`.

## Layout Principles

Make Bold Spark layouts should feel structured and direct.

Use:

- Strong left alignment.
- Clear section rhythm.
- Thin dividers.
- Compact cards with 8 px radius or less.
- Tool-like controls and practical navigation.
- Angled accents only when they support hierarchy.

Avoid:

- Decorative gradient blobs.
- Excessive card nesting.
- Overly soft rounded corners.
- Generic SaaS hero compositions that hide the product.
- One-note red-only pages.

## UI Components

Buttons:

- Primary buttons use spark red or ink.
- Secondary buttons use transparent or paper backgrounds with ink borders.
- Keep border radius at `8px`.
- Use icon buttons for common actions when an icon is clearer than text.

Cards:

- Use cards for repeated items, examples, resource tiles, or bounded tools.
- Do not place cards inside cards.
- Use subtle borders instead of heavy shadows.

Badges:

- Use uppercase, short labels.
- Use functional colors for status.

Forms:

- Labels should be short and direct.
- Inputs should have clear focus rings using spark red.
- Error text should be specific and actionable.

## Iconography

Icon style should be:

- Simple line icons.
- 1.75 px to 2 px stroke.
- Square viewboxes.
- Rounded joins only when the icon set uses them.

Use icons to clarify tools and actions, not as decoration.

## Imagery

Preferred imagery:

- Real product screens.
- Code, architecture, API, and workflow visuals.
- Builder environments.
- Abstract geometry derived from the spark mark.

Avoid:

- Generic stock business handshakes.
- Dark, blurry, atmospheric photos.
- Images where the real product or project cannot be understood.

## Motion

Motion should feel responsive and purposeful.

Use:

- Short transitions between 120 ms and 220 ms.
- Subtle hover movement of 1-2 px.
- Progress and loading states that clarify system status.

Avoid:

- Infinite decorative motion on documentation pages.
- Large bounce effects.
- Motion that delays interaction.

## Website Guidance

Header:

- Use the horizontal logo on desktop.
- Use the mark plus `Make Bold Spark` text on tight mobile headers.
- Keep navigation plain and scannable.

Hero:

- The first viewport should identify Make Bold Spark immediately.
- Use a real product visual, architecture visual, or strong typographic composition.
- Keep the next section slightly visible on normal desktop and mobile viewports.

Documentation:

- Favor short sections, checklists, examples, and code snippets.
- Use callouts only for decisions, warnings, and reusable patterns.

## Social and App Usage

Use the mark-only asset for:

- GitHub organization/project avatar.
- Open Graph fallback image detail.
- App launcher icon.
- Favicon.

Use the share image for:

- Open Graph image.
- LinkedIn project posts.
- Documentation previews.

## File Naming

Use lowercase kebab-case for brand assets:

- `makeboldspark-logo-horizontal.svg`
- `makeboldspark-mark.svg`
- `makeboldspark-favicon.ico`
- `makeboldspark-brand.css`

Use `makeboldspark` as the folder name for portable asset packages.

## Implementation Checklist

For every Make Bold Spark website or project:

- Add favicon links from `templates/html-head.html`.
- Include the CSS variables from `css/makeboldspark-brand.css`.
- Use the public name `Make Bold Spark` in page titles.
- Use `MakeBoldSpark` only where spaces are impractical.
- Keep primary CTAs visually consistent.
- Verify color contrast for small text.
- Include Open Graph metadata and the social share image.
- Keep logo clear space intact.

