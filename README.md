<br />
<div align="center">
  <a href="https://github.com/Mysterken/DLsiteMetadata">
    <img src="documentation/icon.png" alt="Logo" width="256" height="256">
  </a>

<h3 align="center">DLsiteMetadata</h3>

  <p align="center">
    Playnite metadata plugin that fetch from DLsite 
  </p>

[![CodeFactor](https://www.codefactor.io/repository/github/mysterken/dlsitemetadata/badge)](https://www.codefactor.io/repository/github/mysterken/dlsitemetadata)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Mysterken/DLsiteMetadata)](https://github.com/Mysterken/DLsiteMetadata/releases/latest)
![Language](https://img.shields.io/github/languages/top/Mysterken/DLsiteMetadata)
[![GitHub](https://img.shields.io/github/license/Mysterken/DLsiteMetadata)](https://github.com/Mysterken/DLsiteMetadata/blob/master/LICENSE)  
![GitHub downloads](https://img.shields.io/github/downloads/Mysterken/DLsiteMetadata/total)
![GitHub stars](https://img.shields.io/github/stars/Mysterken/DLsiteMetadata?style=social)
</div>

### Installation

1. Download and install the latest release from the [releases page](https://github.com/Mysterken/DLsiteMetadata/releases/latest).

## Usage

Supported [fields](https://api.playnite.link/docs/api/Playnite.SDK.Plugins.MetadataField.html):
- Age Rating
- Background Image
- Community Score
- Cover Image
- Description
- Developers
- Features
- Genres
- Icon
- Links
- Name
- Publishers
- Release Date
- Series
- Tags

### Getting metadata directly from the game page

If you can't find the game you're looking for in the search results, you can get the metadata directly from the game page, there are two ways to do so.   


1. Set the game name as either the full URL or the work ID (RJ150726) of the game you want to fetch metadata from.
2. Set a new link to the game with the name `dlsite` and the link's URL or directly the work ID (RJ150726).

#### Example

<img src="documentation/addLinkGuide.jpg" alt="Logo">

In both cases, the plugin will fetch the metadata from the game page and set it to the game.

### Configuration

You can configure the plugin by going to the plugin settings and setting the following values:

| Name                                   | Default value  | Description                                                                 |
|----------------------------------------|----------------|-----------------------------------------------------------------------------|
| Game category                          | All categories | What DLsite category should the plugin fetch from                           |
| Page Language                          | English        | The page locale language                                                    |
| Assign DLsite genres to                | Genres         | Which Playnite field should the plugin assign DLsite Genres to              |
| Assign Supported languages to          | None           | Which Playnite field should the plugin assign DLsite Supported languages to |
| Include Illustrators as Developers     | No             | This will include Illustrators in the Developers field in Playnite          |
| Include Scenario Writers as Developers | No             | This will include Scenario Writers in the Developers field in Playnite      |
| Include Music Creators as Developers   | No             | This will include Music Creators in the Developers field in Playnite        |
| Include Voice Actors as Developers     | No             | This will include Voice Actors in the Developers field in Playnite          |
| Include Product Format as Features     | Yes            | This will include the Product Format in the Features field in Playnite      |
| Include File Format as Features        | No             | This will include the File Format in the Features field in Playnite         |
| Max Search Results                     | 30             | Maximum amount of search results that should appear                         |
| Assign Game Product Format to Genres   | Yes            | This will assign work_type Product Format to the Genres field in Playnite   |

## Roadmap

- [x] Add the possibility to fetch metadata from every category at once
- [x] Fetch a game by its work ID instead of the full URL
- [ ] Automatic .pext packaging at every release
- [ ] Add tests

## Acknowledgments

* [erri120's Playnite.Extensions](https://github.com/erri120/Playnite.Extensions)

