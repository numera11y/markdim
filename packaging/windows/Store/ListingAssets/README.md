# Microsoft Store listing artwork

These are the submission images for the Partner Center listing:

- `Store-Listing-Logo-300x300.png` — application tile icon, 1:1.
- `Store-Listing-Logo-150x150.png` — store logo, 1:1.
- `Store-Listing-Logo-71x71.png` — compact store logo, 1:1.
- `Store-Poster-9x16.png` — French poster, 1440 × 2160.
- `Store-Hero-1x1.png` — French square hero, 2160 × 2160.
- `Store-Poster-9x16.en.png` — English poster, 1440 × 2160.
- `Store-Hero-1x1.en.png` — English square hero, 2160 × 2160.
- `Store-Screenshot-Source.png` — French-language markdim interface capture used in the existing French Store artwork.
- `Store-Screenshot-Source.en.png` — English-language app capture used by the English README and website.

The artwork uses the current markdim logo and a locally authored sample document, with no personal document content. The three logos are generated from the same icon using `Create-StoreListingLogos.ps1`. `Create-StoreListingArtwork.ps1` reads both screenshot sources and generates the French and English poster/hero pairs; pass `-ScreenshotPath` or `-EnglishScreenshotPath` to use alternate captures. ffmpeg and the existing Store icon assets are required.
