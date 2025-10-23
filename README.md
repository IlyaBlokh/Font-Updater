# Font Updater

A handy Editor tool for bulk updating a font in prefabs

## Installation

Add via Package Manager git URL:
```
https://github.com/ilyablokh/Font-Updater.git#1.0.1
```

Or add to `Packages/manifest.json`:
```
com.echotitan.fontupdater": "https://github.com/ilyablokh/Font-Updater.git#1.0.1
```


## Usage
- Open the tool under *Tools/Font Updater* menu
- Select the new font
- Select the root folder where to search for prefabs with texts
- Press the *Search Texts* button to find all GameObjects with `TMP_Text` components in all prefabs in the chosen project folder

<img width="379" height="627" alt="font_updater_1" src="https://github.com/user-attachments/assets/5e5a01b9-6e93-4645-9ed7-f2d25962e5cf" />

- Select / Deselect GameObjects
- Press the *Replace Font* button at the bottom of the list
  
## License
MIT
