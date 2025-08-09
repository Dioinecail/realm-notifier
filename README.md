A very simple notification overlay made for a game called Realm of the Mad God  

## How it works  

- Takes a screenshot of a portion of your screen  
- Processes it via TesseractEngine from image to text
- Checks if the text contains any of the target dungeons
- If a target dungeon is found it plays a sound


You can customize the notification sound if you put a `.wav` file right next to the `.exe`  

The project uses [TesseractEngine](https://github.com/tesseract-ocr/tesseract) for image to text conversion  
No data is stored or sent from the app anywhere, portion of the screen is getting processed as-is inside the memory  
