# What is the Dataverse Browser?

It's a browser which allows a Dataverse developer to:
* Debug plugins in live (while navigating on a Model Driven App)
* Test plugins without having to deploy them. This speed ups developments and enable coauthoring on plugin assemblies.
* Analyze in real time what plugins are triggerred. This is particularly useful when you have plugins triggerring other plugins.


# How does it work?

The application embeds a web browser based on Chromium (thanks to [CEFSharp](https://cefsharp.github.io/)). It intercepts all web api requests and translates them to SDK requests. Then it analyzes if some plugin steps are registered on the message. If yes, it loads them and make them run locally. All other requests are sent to the Dataverse, so that the plugins are interacting with the real database.

For now, only create, retrieve, update and delete requests are translated (sometimes partially). Other requests (custom api, batch, ...) should be implemented in not too distant future.
Please note that the chosen architecture has some limits which will probably never fixed by this project:
* There is no support for transactions
* When plugins are triggered because of a server side operation, they will not be run locally
* For many reasons, behavior will never be perfectly similar to the one when plugins are executed on server side.

# How to start?

## Warning
Keep in mind that it is a very first version. They are a lot of known bugs and probably more waiting to be discovered. There is little to no error management. If the application crashes, you will have to check the event viewer to know why. 
Moreover the UI is very minimalist and not very beautiful. 
That said, it still runs quite well on my laptop and it's already a real time saving application. I hope it will be the same for you.

## Prerequistes
You need:
* A PowerPlatform environment with dataverse
* A .NET debugger (preferably Visual Studio) running on Windows 10 or Windows 11 
* A plugin project. It must have been deployed on Dataverse and you must have its source code.

## Let's go!
The simplest way to test the Dataverse.Browser is to:
* Download [the latest release](https://github.com/NicolasPrats/DvBrowser/releases)
* Unzip the file in the folder of your choice
* Run the "Dataverse.Browser.exe" file. (For early adopters: the start.cmd file is not needed anymore)
* Enter the settings of your environment:
  * A name meaningful for you
  * The host name of your instance (without the _https://_)
  * The path to the plugins assembly file (the dll). For a better experience, it should be compiled in debug mode with the pdb file generated.
* A popup asking you to authenticate is displayed. You need to use a system administrator. If you have a big assembly file and/or a lot of steps, the UI may freeze at this step. Don't worry and please wait few seconds.
* Then the browser is started. You need to authenticate again. This time, you can choose any user and should take the one that is relevant for your tests.
* Enjoy! 

At the top right of the window, you have 2 icons and a checkbox:
* First icon is to open the Developer tools
* Second icon is to automatically attach a debugger to the browser. Before to use it, it's recommended to open your plugin solution in a Visual Studio instance. When a popup asks you to select a Visual Studio instance, choose this one. 
* If the checkbox is selected, then plugins are run locally and can be debugged in Visual Studio. If not they are run on server side, as with any other browser.

At the right bottom of the list, the trash icon allows you to clear the history of requests.


