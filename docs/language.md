# Languages
FoxIDs support translating the user interfaces elements into the configured languages. English is default (FoxIDs Control Client only support English). It is possible to add text translations to all text elements.  

By default, the text translations are read from the embedded resource file. The [EmbeddedResource.json](https://github.com/ITfoxtec/FoxIDs/blob/master/src/FoxIDs.Shared/Models/Master/Resources/EmbeddedResource.json) file can be found in the git repository.  

## Contributions

Text translations added to the [EmbeddedResource.json](https://github.com/ITfoxtec/FoxIDs/blob/master/src/FoxIDs.Shared/Models/Master/Resources/EmbeddedResource.json) file will become generally available. 
Each time the FoxIDs app service are updated and restarted the new text resources is loaded and new translations will become available.

> Text translation contributions are greatly appreciated.

It is possible to contribute either by creating a pull request in the FoxIDs [GitHub repository](https://github.com/ITfoxtec/FoxIDs) or by sending an updated [EmbeddedResource.json](https://github.com/ITfoxtec/FoxIDs/blob/master/src/FoxIDs.Shared/Models/Master/Resources/EmbeddedResource.json) file to [support@itfoxtec.com](mailto:support@itfoxtec.com?subject=FoxIDs-embedded-resource).

## Translation in track

It is possible to add track specific translations for each text element in multiple languages in [FoxIDs Control Client](control.md#foxids-control-client).

Add translation to a track:

1. Open the track
2. Select the Texts tab
3. Select a text element
4. Specify language and add the text
5. Click Add text to add a translation in another language
6. Click Create

This is an example of a text element translated into two languages (da and es).

![Configure text](images/configure-tenant-text.png)
