# Program Viewer 3.0

I created this project for exploring a WPF technology. The main idea of this project is to keep a desktop free from a not very frequently used folders and files(items in one word). 
I realise that I need to occupy some Windows Desktop space to provide an opportunity for users of using the most important items without wasting a lot of time looking for these items.
So the user interface is divided into two areas: "hot" and "desktop". The desktop area can be shown by clicking one vertical button and is designed for containing not very frequently used items.
The hot area is used to contain the most frequently used items(preffered executable files), it is always visible(if not overlayed by another program).

# VirtualizingGridPanel Control

Improved performance with asynchronous items loading. This control uses a RenderTargetBitmap class to render a whole row of items into a single ImageSource object.
It helps application to render and update much less ui objects and this improvement is especially noticable during the smooth scrolling animation. The rendering process is fully asynchronous 
in order not to freeze the main thread while the row rendering process is in progress. The control has an ItemSource property and updates the rows every time source collection is changed.
The VirtualizingGridPanel is not very universal, I designed and developed it to be the best fit for my application, but it will be improved and I hope, it can be usefull not only for my project.

# Customization screenshots:
![Alt text](https://i.postimg.cc/bwqHFPVH/Screenshot-1.png "")
![Alt text](https://i.postimg.cc/xCNKb1qY/Screenshot-2.png "")
![Alt text](https://i.postimg.cc/L6KVLLzW/Screenshot-3.png "")
![Alt text](https://i.postimg.cc/Njb83nWh/Screenshot-4.png "")
![Alt text](https://i.postimg.cc/FKByRLrD/Screenshot-5.png "")
![Alt text](https://i.postimg.cc/Gp2PdQC4/Screenshot-6.png "")
![Alt text](https://i.postimg.cc/mZFwP8FM/Screenshot-7.png "")
![Alt text](https://i.postimg.cc/3JXFBN1x/Screenshot-8.png "")
![Alt text](https://i.postimg.cc/Fzt0bXJS/Screenshot-9.png "")
![Alt text](https://i.postimg.cc/XqwdQ1wz/Screenshot-10.png "")
![Alt text](https://i.postimg.cc/pV8fWrgJ/Screenshot-11.png "")
