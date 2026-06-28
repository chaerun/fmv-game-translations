# LubanUnpacker

A powerful, extensible modding framework designed to unpack, modify, and repack Luban `.bytes` data files. 

This tool uses a dynamic, reflection-based architecture to read binary game data, convert it to human-readable JSON for easy editing, and safely repack it back into the exact binary format required by the game engine.

For more information about `Luban`, please visit the official GitHub repository at [focus-creative-games/luban][luban-url]."

---

## ⚙️ Prerequisites & Setup

Before you can build or run this tool, you need to set up your environment.

1. **Install .NET SDK 8.0 or higher:**

   * Download and install it from the [official Microsoft website](https://dotnet.microsoft.com/download).
   * Verify your installation by opening a terminal and running: `dotnet --version`

2. **Required DLL Files:**

   Copy the following required DLL files from `steamapps\common\<game_folder>\<game_folder>_Data\Managed` to the `lib` folder of this project:
   * `Assembly-CSharp.dll`: The Generator engine relies on this to read the original game data structures.
   * `Luban.Runtime.dll`: This is the main library for unpacking and repacking the Luban `.bytes` data files.

3. **Restore Dependencies:**

   * Open your terminal in the root folder of this project (where the `.csproj` file is located) and run the following command to download all required NuGet packages (like Newtonsoft.Json and Roslyn Analyzers):

   ```bash
   dotnet restore
   ```

4. **dnSpyEx:**

   * Download and install [dnSpyEx][dnspyex-url]

---

## 📂 Folder Structure

Before using the tool, ensure your workspace is set up correctly:

* **`Input/`** - Drop your original, unmodified `.bytes` files here.
* **`Output/`** - Your unpacked `.json` files and your repacked `_NEW.bytes` files will appear here.
* **`Models/Generated`** - Contains the generated C# class definitions (blueprints) for each game file.
* **`Utils/`** - Contains the Generator engine and other helper scripts.

---

## 🚀 How to Use the Tool

### Get the Class Name of the `.bytes` Data File

1. Run `dnSpy` and open `Assembly-CSharp.dll`
2. Click `Edit` > `Search Assemblies`
3. In the `Search` bar, type the file name without extention (e.g. `tboptionscontent`).
4. Open the class (indicated by a yellow icon) and locate the line containing `.Deserialize`.

   ```csharp
   optionContent optionContent = optionContent.DeserializeoptionContent(_buf);
   ```

   In this example, `optionContent` is the class name.

### Run The Application

Run the application using the command below. You will be presented with the main menu. 

```bash
dotnet run
```

#### **Option 1:** Generate Model (Adding a new file type)

Before you can unpack a new `.bytes` file, the tool needs to know how to read it. You only need to do this once per file type.

1. Find the target class in the game's `Assembly-CSharp.dll` (e.g., `cfg.optionContent`).
2. Run the tool and select **Option 1 (Generate)**.
3. The tool will use Reflection to read the class from the DLL and automatically generate a clean, mod-ready `optionContent.cs` file in your `Models/Generated` folder.
4. **Important:** Open `Program.cs` and add your new file to the `FileClassRegistry`:

   ```csharp
   { "tboptionscontent", typeof(cfg.optionContent) }
   ```

   **NOTE**: 
   * The key must match the exact name of the `.bytes` file, without the extension.
   * Include the `namespace` in the class name.

#### **Option 2:** Unpack All (.bytes -> .json)

Use this option to extract the binary data into an editable format.

1. Place your target `.bytes` files (e.g., `tboptionscontent.bytes`) into the `Input/` folder.
2. Run the tool and select **Option 2 (Unpack)**.
3. The tool will read all registered files in the `Input/` folder and generate editable JSON files (e.g., `tboptionscontent_extracted.json`) in the `Output/` folder.

#### **Option 3:** Repack All (.json -> .bytes)

Use this option to inject your modifications back into the game.

1. Open the `_extracted.json` files in the `Output/` folder and make your modded changes.
2. Run the tool and select **Option 3 (Repack)**.
3. The tool will read your modified JSON data and generate a game-ready binary file (e.g., `tboptionscontent_NEW.bytes`) in the `Output/` folder.
4. Rename the file (remove the `_NEW` tag) and place it in your game's data directory to test your mod!

---

## 🛠️ The Modding Loop (Quick Start)

Whenever you discover a new file in the game you want to mod, just follow this loop:

1. Extract the `cfg` class name.
2. Generate the model using **Option 1**.
3. Register the model in **Program.cs**.
4. Unpack the `.bytes` file (**Option 2**).
5. Edit the JSON.
6. Repack the `.bytes` file (**Option 3**).

[dnspyex-url]: https://github.com/dnSpyEx/dnSpy/releases
[luban-url]: https://github.com/focus-creative-games/luban