# Microsoft Store listing artwork

These are the submission images for the Partner Center listing:

- `Store-Listing-Logo-300x300.png` — application tile icon, 1:1.
- `Store-Listing-Logo-150x150.png` — store logo, 1:1.
- `Store-Listing-Logo-71x71.png` — compact store logo, 1:1.
- `Store-Poster-9x16.png` — 1440 × 2160.
- `Store-Hero-1x1.png` — 2160 × 2160.
- `Store-Screenshot-Source.png` — real markdim interface capture used inside both compositions.

The artwork uses the current markdim logo and a locally authored sample document, with no personal document content. The three logos are generated from the same icon using `Create-StoreListingLogos.ps1`. To regenerate the promotional images after updating the application screenshot, replace `Store-Screenshot-Source.png` and run `Create-StoreListingArtwork.ps1`; ffmpeg and the existing Store icon assets are required.
