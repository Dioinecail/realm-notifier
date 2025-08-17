A very simple notification overlay made for a game called Realm of the Mad God  

## How it works  

- Takes a screenshot of a portion of your screen  
- Processes it via TesseractEngine from image to text
- Checks if the text contains any of the target dungeons
- If a target dungeon is found it plays a sound


You can customize the notification sound if you put a `.wav` file right next to the `.exe`  

The project uses [TesseractEngine](https://github.com/tesseract-ocr/tesseract) for image to text conversion  
No data is stored or sent from the app anywhere, portion of the screen is getting processed as-is inside the memory  

If you want to autorun the overlay when the game starts you can do the following:  
- Copy the `run.bat` file from the zip right next to your realm launcher (usually at `Steam/steamapps/common/Realm of the Mad God`)  
- add this to the launch properties of the game on steam `"run.bat" %COMMAND%`
- open the `run.bat` file with a notepad and put the paths to the Notifier app and the realm launcher where it shows  
<img width="1082" height="585" alt="image" src="https://github.com/user-attachments/assets/d195698c-4805-4d20-bca1-52f9fd24caaf" />  
<img width="1241" height="215" alt="image" src="https://github.com/user-attachments/assets/56432d8b-0237-45c6-a3cf-9f5d02c1c7ee" />
