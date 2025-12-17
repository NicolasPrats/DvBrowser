# What is the Dataverse Browser?

It's a browser which allows a Dataverse developer to:
* Debug plugins in live (while navigating on a Model Driven App)
* Test plugins without having to deploy them. This speeds up developments and enable coauthoring on plugin assemblies.
* Analyze in real time what plugins are triggerred. This is particularly useful when you have plugins triggerring other plugins.

# How does it work?

The application embeds a web browser based on Chromium (using WebView2). It intercepts all web api requests and translates them to SDK requests. Then it analyzes if some plugin steps are registered on the message. If yes, it loads them and make them run locally. All other requests are sent to the Dataverse, so that the plugins are interacting with the real database.

The browser now supports:
* **CRUD operations**: Create, Retrieve, Update, Delete (including CreateMultiple, UpdateMultiple, UpsertMultiple)
* **Batch requests**: Including $batch with changeset support (transactional mode) and continue-on-error preference
* **Custom API**: Custom actions and bound functions (e.g., RetrievePrincipalAccess)
* **Lookup fields**: Full OData annotations support for formatted values and logical names via Prefer header forwarding

**Important Note on Retrieve Operations**: For retrieve operations (Retrieve, RetrieveMultiple), the emulator proxies requests directly to the Dataverse Web API instead of using SDK conversion. This ensures complete responses with null attributes and OData annotations, which are essential for proper form rendering in Model Driven Apps. As a consequence, **Retrieve and RetrieveMultiple plugins are executed server-side and cannot be debugged locally**. All other operations (Create, Update, Delete, Custom API) run plugins locally and can be debugged.

Please note that the chosen architecture has some limits which will probably never be fixed by this project:
* **Retrieve plugin debugging**: Retrieve and RetrieveMultiple plugins run server-side (not locally) to ensure proper form rendering with OData annotations
* There is no support for transactions (except within batch changesets)
* When plugins are triggered because of a server side operation, they will not be run locally
* For many reasons, behavior will never be perfectly similar to the one when plugins are executed on server side.

# How to start?

## Prerequistes
You need:
* A PowerPlatform environment with dataverse
* A .NET debugger (preferably Visual Studio) running on Windows 10 or Windows 11 
* A plugin project. It must have been deployed on Dataverse and you must have its source code.

## Let's go!
The simplest way to test the Dataverse.Browser is to:
* Download [the latest release]
* Unzip the file in the folder of your choice
* Run the "Dataverse.Browser.exe" file.
* Enter the settings of your environment:
  * A name meaningful for you
  * The host name of your instance (without the _https://_)
  * The path to the plugins assembly file (the dll). For a better experience, it should be compiled in debug mode with the pdb file generated.
* A popup asking you to authenticate is displayed. You need to use a system administrator. If you have a big assembly file and/or a lot of steps, the UI may freeze at this step. Don't worry and please wait a few seconds.
* Then the browser is started. You need to authenticate again. This time, you can choose any user and should take the one that is relevant for your tests.
* Enjoy! 

At the top right of the window, you have 2 icons and a checkbox:
* First icon is to open the Developer tools
* Second icon is to automatically attach a debugger to the browser. Before to use it, it's recommended to open your plugin solution in a Visual Studio instance. When a popup asks you to select a Visual Studio instance, choose this one. 
* If the checkbox is selected, then plugins are run locally and can be debugged in Visual Studio. If not they are run on server side, as with any other browser.

## Hot Reload

The Dataverse Browser supports **hot reload** of plugin assemblies, enabling rapid development iteration without restarting the application:

* Make changes to your plugin code in Visual Studio
* Build your solution (F6 or Ctrl+Shift+B)
* The browser automatically detects the change and reloads the assembly
* A notification appears confirming the reload was successful
* Continue debugging with your updated code immediately

**Workflow with Debugger:**

When you need to make code changes while debugging:
1. **Detach the debugger** (or close the debugging session in Visual Studio)
2. Make your code changes
3. Build the solution (F6) - hot reload happens automatically
4. **Re-attach the debugger** using the attach icon in the browser
5. Debug your new code

*Note: While .NET Framework supports basic "Edit and Continue" for small method changes, hot reload requires detaching and re-attaching the debugger for structural changes. You can build while the debugger is attached (no file locks!), but you'll need to re-attach to actually debug the new code.*

**Technical Details:**
* Uses FileSystemWatcher to monitor the plugin DLL for changes
* 2-second debounce timer handles multiple file system events during build
* Assembly.Load from bytes ensures no file locking - Visual Studio can rebuild while browser is running
* Plugin instance cache and step registrations are cleared and reloaded automatically
* Old assembly versions remain in memory (cannot be unloaded in .NET Framework) but are no longer used
* IOrganizationService proxy is recreated to use the new plugin instances

At the right bottom of the list, the trash icon allows you to clear the history of requests.

## Asynchronous Plugin Testing

The Dataverse Browser fully supports testing **asynchronous plugins** with special handling to prevent duplicate execution:

### How It Works

* **Async plugins execute locally**: Just like synchronous plugins, async plugins run in the browser emulator with `Mode = 1` context
* **Execution order preserved**: Async steps execute after sync steps in Post-Operation stage, maintaining proper plugin pipeline order
* **Immediate execution**: Unlike server-side async plugins (which queue), emulated async plugins run immediately in the same process

### "Disable Asynchronous Steps" Feature

The environment settings include a **"Disable asynchronous steps"** checkbox that prevents double execution of async plugins:

**⚠️ IMPORTANT BEHAVIOR:**
* When enabled, **ALL asynchronous plugin steps from your loaded plugin assembly are disabled on the server**
* This affects **every async step** in the assembly, not just specific plugins
* Synchronous steps from the same assembly remain active
* Async steps from other assemblies are NOT affected

**Example:** If your assembly contains:
- Plugin A: Create Account (Sync) - **Remains Active**
- Plugin B: Update Contact (Async) - **Disabled on Server**
- Plugin C: Delete Lead (Async) - **Disabled on Server**

All async steps (B and C) will be disabled when the checkbox is checked.

**Why Use This Feature:**
Without this option, async plugins would execute twice:
1. Once locally in the browser (for debugging)
2. Once on the server after the request completes

This can cause unwanted side effects like duplicate emails, duplicate records, or double processing.

### Crash Recovery

The browser includes robust recovery mechanisms for unexpected shutdowns:

**Lock File Tracking:**
* When async steps are disabled, a lock file (`disabled_async_steps.lock`) is created with the specific step IDs
* This file is automatically deleted on normal exit
* Tracks only the steps that were actually disabled by the browser

**Exit Handlers:**
* ProcessExit, ApplicationExit, and UnhandledException handlers attempt to re-enable steps even during crashes
* Multiple redundant handlers ensure cleanup happens in most failure scenarios

**Startup Recovery:**
* On next startup, if an orphaned lock file is detected, a dialog offers to re-enable the steps
* Shows exactly how many steps were left disabled
* Gives you the choice to recover or leave them disabled
* Only re-enables the specific steps that were disabled (not all disabled steps in the environment)

**Manual Recovery:**
If automatic recovery fails:
1. Open your Dataverse environment
2. Navigate to Settings → Customizations → Customize the System → SDK Message Processing Steps
3. Find the disabled steps (Status = Inactive) from your plugin assembly
4. Manually activate them

Or simply uncheck the "Disable asynchronous steps" option and run the browser normally once.

# Known Limitations and Future Work

The following features are not yet implemented and are tracked for future development:

## Not Yet Implemented
* **Plugin Configuration & Secure Configuration**: Plugin step configuration and secure configuration values are not passed to plugins during execution
* **Alternate Keys**: Entity retrieval and updates using alternate keys (instead of GUIDs) are not fully supported for plugin step identification
* **Web Resource File Overrides**: Ability to replace web resources (JavaScript, CSS, HTML) with local files during development, similar to Chrome DevTools Local Overrides, enabling live editing without deployment


