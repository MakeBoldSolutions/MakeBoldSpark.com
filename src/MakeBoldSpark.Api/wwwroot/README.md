# wwwroot ownership

## Hand-maintained

- `index.html`
- `vision.html`
- `ecosystem.html`
- `subsites.json`
- `assets/makebold/brand.css`
- `assets/makebold/logos/`
- `assets/makebold/fonts/`

## Build-generated

Do not edit these paths directly. Run `npm run build` from `src/MakeBoldSpark.Web` or build `MakeBoldSpark.Api`.

- `insights/`
- `systems/`
- `assets/makebold/catalog.json`

## Dynamic at request time

- `ecosystem.html` reads `subsites.json` in the browser to render the broader ecosystem inventory.

The static-site build intentionally does not modify any hand-maintained or dynamic path.
