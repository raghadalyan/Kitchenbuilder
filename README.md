# 🚀 Smart Kitchen Builder – SolidWorks Automation Project

This project automates the creation of kitchen layouts using the **SolidWorks API** and **.NET 8**.  
It enables users to enter kitchen dimensions via a dashboard UI and generates up to three optimized kitchen designs. Each layout includes suggested base arrangements, cabinet options, and appliance placements. The system programmatically modifies SolidWorks `.SLDPRT` files based on user input.

---

## ✨ Features

- **Parametric Design** – Adjusts base and cabinet sizes according to kitchen dimensions.  
- **Three Layout Options** – Automatically generates up to 3 kitchen designs per input.  
- **Interactive UI** – Built with Blazor/Maui to collect user data and control generation flow.  
- **SolidWorks Integration** – Connects to SolidWorks using its API to modify and save part files.  
- **Customizable Cabinets** – Allows setting drawer count and cabinet height for each base.  
- **Appliance Placement** – Logic-based sink and cooktop positioning under windows, in center, or side-by-side.  

---

## ⚙️ Getting Started

### ✅ Prerequisites

- **SolidWorks**  
  Must be installed locally with API access enabled.

- **.NET SDK (8.0 or higher)**  
  Download from [Microsoft .NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

- **SolidWorks API DLLs**  
  Required files:  
  - `SolidWorks.Interop.sldworks.dll`  
  - `SolidWorks.Interop.swconst.dll`  
  > 📌 **TODO:** Place these DLLs inside a `libs/` folder.

---

## 🧭 Workflow

1. **Launch Dashboard** – The user opens the Smart Kitchen Builder dashboard UI.  
2. **Input Floor Dimensions** – User enters kitchen width and length.  
3. **Wall & Window Setup** – User defines number of walls, their lengths, and window positions.  
4. **Room Preview** – The system visualizes the kitchen room based on the inputs.  
5. **Choose Floor Material / Color** – User selects material and color for the kitchen floor.  
6. **Generate Base Layout Options** – The system suggests up to 3 layout options for the kitchen base.  
7. **Select Base Layout** – User selects one of the suggested base layouts.  
8. **Choose Base Material / Color** – User selects material and color for the base.  
9. **Suggest Countertop** – The system recommends a suitable countertop based on the chosen base.  
10. **Customize Countertop (Optional)** – User customizes dimensions and selects material and color.  
11. **Suggest Sink & Cooktop Layout** – Placement suggestions based on design rules (centered, under window, side-by-side).  
12. **User Chooses Sink/Cooktop Option** – User selects the preferred layout.  
13. **Suggest Island (Optional)** – System proposes an island if space allows.  
14. **Choose Island Material / Color** – User selects design properties.  
15. **Create Cabinets** – Cabinets are automatically generated along the visible bases.  
16. **Place Appliances** – System suggests and places appliances like the microwave and oven.  
17. **Save Final Design** – All `.SLDPRT` files and preview images are saved to the output folder.

---

## 📁 Key Files

- `LayoutLauncher.cs`: Splits SolidWorks and dashboard view  
- `JsonReader.cs`: Loads user input and applies it in SolidWorks  
- `EditSketchDim_IModel.cs`: Edits a specific SolidWorks sketch dimension by name and value  
- `Show_Bodies_In_Sld_IModel.cs`: Makes specific SolidWorks bodies visible by name  
- `Hide_Bodies_In_Sld_IModel.cs`: Hides specific SolidWorks bodies by name  
- `SaveImgs.cs`: Saves multiple standard view images (top, front, right, isometric) of the model to a folder


---
##  License
This project is licensed under the MIT License. See the LICENSE file for more information.

