# FMV Game Translations

[![Discord][discord-discussion-logo]][discord-fmv-heaven]

A community-driven for custom translation patches and subtitle modifications for Full Motion Video (FMV) games.

---

## 🎮 Supported Games

### [Love Chase](https://store.steampowered.com/app/2812850/Love_Chase/)

- **Target Language:** English
- **Status:** Done
- **Latest Release**: [![Love Chase Latest][lc-release-badge]][lc-release-url]

#### Credits

- **[BBNO$]** - Original Translation

#### Installation Instructions

Please refer to the **[Love Chase Installation Guide](./games/LoveChase/README.md)** for detailed, step-by-step instructions.

---

## 🛠️ Tools & Workflow (For Modders)

If you are interested in how these patches are created or want to contribute by translating new files, the underlying games run on Unity and use the Addressables system. We utilize the following open-source community tools to modify the game assets:

- **[UABEA](https://github.com/nesrak1/UABEA) (Unity Asset Bundle Extractor Avalon):** Used to unpack the original Unity `.bundle` files, extract the subtitle/text assets, and repackage the bundles once the localized text has been injected.
- **[AddressablesTools](https://github.com/nesrak1/AddressablesTools):** Crucial for patching the `catalog.json` CRC. Because modifying the bundles changes their checksums, the catalog must be updated so the game's Addressables system recognizes and loads our modified assets.

---

## 🤝 Contributing

Contributions are welcome! If you spot any typos, mistranslations, or want to help translate additional game segments, please open an Issue or submit a Pull Request.

> **Technical Note on Repository Structure:**
> Any files prefixed with an underscore `_` (e.g., `_catalog.json`) are the unmodified original files from the game's installation. They are included in this repository strictly to serve as a baseline reference during the translation and quality control processes. Please do not edit these files.

## ⚠️ Disclaimer

This is a non-commercial, community-led fan translation project. This repository is not affiliated with, endorsed by, or connected to the original developers or publishers of the games. You must own a legitimate copy of the original game to use these patches.

## 📄 License

- **Translation Text & Subtitles:** Licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/).
- **Scripts & Tools:** Licensed under the [MIT License](LICENSE).

[BBNO$]: https://discord.com/users/840349143246438450
[discord-discussion-logo]: https://img.shields.io/badge/Discord-Join%20the%20Discussion-7289da?logo=discord&logoColor=white
[discord-fmv-heaven]: https://discord.gg/d48czN5Htp
[lc-release-badge]: https://img.shields.io/github/v/tag/chaerun/fmv-game-translations?filter=love-chase/*&label=version
[lc-release-url]: https://github.com/chaerun/fmv-game-translations/releases?q=love-chase
