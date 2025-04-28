Frontend (vrchat prefab) is made by Rinvo (discord: rinvo), backend API (api.u2b.cx) is made by Lamp (https://lamp.wtf/)
For rules read Licence.txt

Google Docs version of the readme(with pics and stuff):
- https://docs.google.com/document/d/1TGqVZNlJU5p1fVgcZEtNOFm3rvciB2fJE-sx3sBoH3U/edit?usp=sharing


How to use:

- Drag and drop prefab into the scene
- Select "SearchManager" inside the prefab to open the settings 
- Setup the prefab by assigning the reference to your videoplayer's UI Handler, VRCUrlInputField and videoNameInputField
  if your videoplayer supports it, then typing in your personal pool name and size
- Click Apply settings and upload the world:3

Assigning references is slightly differenty depending on videoplayer you use, here is the differences:
(Be aware that names/paths could be different depending on your setup)
More detailed How to use:

  USharpVideo specific stuff:
- Video Player UI Handler is an object with name ControlsUI (USharpVideo -> ControlsUI)
- URL Input Field is an object called InputField (USharpVideo -> ControlUI -> InputField)
- Make sure USharpVideo selected as the videoplayer type

  ProTV 2 sdeecific stuff:
- Video Playuer UI Handler is an object with name ProTV (yeah, literally first one, that has TVManagerV2 script on it)
- URL Input Field is an object called MediaInput (Root -> Controls -> MediaInput)
  You should be able to just click on little circle next to object reference and select it from there
- Make sure ProTV 2 is selected as a the videoplayer type

  ProTV 3 sdeecific stuff:
- Video Playuer UI Handler is an object with name MediaControls V2 (that has MediaControls script on it)
- URL Input Field is an object called MainUrl (MediaControls V2 -> MainUrl)
  You should be able to just click on little circle next to object reference and select it from there
  Or you can find it in the list of media inputs in the MediaControls V2 object
- videoNameInputField is an object called Title (MediaControls V2 -> Extra Options -> Title)
- Make sure ProTV3 is selected as videoplayer type (some old versions still need ProTV2 to be selected)

  IwaSync3 specific stuff:
- Video Player UI Handler is an object with the name Udon (iwaSync3)
- URL Input Field is an object called Address (iwaSync3 -> iwaSync3-iwaSync3 -> Udon (iwaSync3) -> Canvas (1) -> Panel -> Address)



Troubleshooting:

1. Url shows up in the URL Field of the videoplayer, but doesn't play automatically:
- Make sure you set the videoplaeyr type;
- Make sure Video Player UI Handler points to the correct UI Handler object
  (you can make sure by looking if the UdonBehaviour on the object you selected has a field for VRCUrlInputField,
  and they generally called something like ControlUI(for USV), MediaInput(for ProTV2), MediaControls V2 (for ProTV 3))
- Some older versions of ProTV 3 use same interface as ProTV 2, so try setting it to ProTV 2 instead

2. Invalid URL when searching, no "Type YouTube Search Query Here    ->" when clicking the input field:
- Apply the settings in the editor script (its on object called "SearchManager" inside the prefab)
  (you can doublecheck if they are applied by opening "debug information" dropdown, if Current Pool size
  is not 0 - everything is okay)

3. Search works, but videoplayer doesn't react:
- Make sure its not issue 1 by checking URL Input field of the videoplayer after trying to play the video
- Make sure you selected URL Input Field that videoplayer is using in its UI
  (You can doublecheck that by looking for object you assgned as Video Player UI handler in the 
  list of VRCUrlInputField's events, it should be there)

4. If you can see list of videos through the objects somehow - make sure you use shaders with ZWrite enabled around the search
  disabling it causes unity's UI masking to break


Small FAQ for search specific stuff:

1. What is "pool name"?
- Pool name is just identifier of your world in the backend of the search
- Don't use same pool names on different worlds, it may lead to results getting mixed up

2. What is "pool size"?
- Pool size is the amount of storage on the backend allocated to your world, but selecting more
  than you need might lead to performance problems

3. What does "pool size" affect?
- The larger the pool size, the longer search results stay valid, but at the cost of performance/world size.
  Usually you never need it to stay valid for days so below you can find a recomendation for pool size, that will
  keep results valid for several hours before they become invalid (you definitely will already search something else
  by then)

3. What size should i select then?
- Select anywhere between 100 to 500 for small private worlds, 
  2_000 to 5_000 for small public worlds, 
  10_000 for fairly popular publics, 
  20_000 to 50_000 for huge publics,
  50_000 to 100_000 if something has gone wrong and you made the most popular world in the game:3